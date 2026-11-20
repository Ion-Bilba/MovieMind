using Microsoft.EntityFrameworkCore;
using MovieMind.Application.Algorithms;
using MovieMind.Application.Dtos.Recommendations;
using MovieMind.Application.Interfaces;
using MovieMind.Domain.Entities;

namespace MovieMind.Application.Services;

public sealed class RecommendationService(
    IApplicationDbContext context,
    IUserPreferencesService preferencesService) : IRecommendationService
{
    private const int MaximumRecommendations = 50;
    private const int MaximumKMeansIterations = 100;

    public async Task<RecommendationResultDto?> GetForUserAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (count is < 1 or > MaximumRecommendations)
        {
            throw new ArgumentOutOfRangeException(nameof(count), $"Count must be between 1 and {MaximumRecommendations}.");
        }

        var profile = await preferencesService.GetAsync(userId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var genres = await context.Genres
            .AsNoTracking()
            .OrderBy(genre => genre.Id)
            .Select(genre => new { genre.Id, genre.Name })
            .ToListAsync(cancellationToken);
        var genreIds = genres.Select(genre => genre.Id).ToArray();
        var movies = await context.Movies
            .AsNoTracking()
            .Include(movie => movie.Genres)
            .ToListAsync(cancellationToken);
        var ratings = await context.Ratings
            .AsNoTracking()
            .Select(rating => new RatingSignal(rating.UserId, rating.MovieId, rating.Score))
            .ToListAsync(cancellationToken);
        var explicitPreferences = await context.UserGenrePreferences
            .AsNoTracking()
            .Select(preference => new ExplicitPreferenceSignal(preference.UserId, preference.GenreId, preference.Score))
            .ToListAsync(cancellationToken);

        var movieGenreIds = movies.ToDictionary(
            movie => movie.Id,
            movie => movie.Genres.Select(genre => genre.Id).ToArray());
        var ratingsByUser = ratings
            .GroupBy(rating => rating.UserId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var explicitByUser = explicitPreferences
            .GroupBy(preference => preference.UserId)
            .ToDictionary(group => group.Key, group => group.ToDictionary(item => item.GenreId, item => item.Score));
        var ratingSignalsByUserGenre = BuildRatingSignalsByUserGenre(ratings, movieGenreIds);
        var activeUserIds = ratings.Select(rating => rating.UserId)
            .Concat(explicitPreferences.Select(preference => preference.UserId))
            .Append(userId)
            .Distinct()
            .ToArray();
        var hasProfile = ratingsByUser.ContainsKey(userId) || explicitByUser.ContainsKey(userId);
        var userVector = new SparsePreferenceVector(profile.Vector.ToDictionary(item => item.GenreId, item => item.Value));
        var neighborWeights = FindNeighborWeights(
            userId,
            hasProfile,
            genreIds,
            activeUserIds,
            ratingsByUser,
            explicitByUser,
            ratingSignalsByUserGenre,
            userVector);
        var excludedMovieIds = ratingsByUser.TryGetValue(userId, out var ownRatings)
            ? ownRatings.Select(rating => rating.MovieId).ToHashSet()
            : [];
        var candidates = movies
            .Where(movie => !excludedMovieIds.Contains(movie.Id))
            .Select(movie => new RecommendationCandidate(
                movie.Id,
                movie.Title,
                SparsePreferenceVector.FromGenres(movie.Genres.Select(genre => genre.Id))))
            .ToList();
        var contentRanked = TopKSelector.Select(userVector, candidates, candidates.Count, excludedMovieIds)
            .ToDictionary(item => item.Id);

        var recommendations = movies
            .Where(movie => contentRanked.ContainsKey(movie.Id))
            .Select(movie => CreateRecommendation(movie, contentRanked[movie.Id], neighborWeights, ratingsByUser, hasProfile))
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.ContentSimilarity)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.MovieId)
            .Take(count)
            .ToArray();

        var strategy = !hasProfile
            ? "Popularity"
            : recommendations.Any(item => item.CollaborativeScore is not null) ? "Hybrid" : "ContentBased";
        var message = strategy switch
        {
            "Popularity" => "Your profile is new, so these popular films are a starting point. Rate a few films or set genre preferences to personalize recommendations.",
            "ContentBased" => "Recommendations are based on your genre preferences and rating history.",
            _ => "Recommendations combine genre similarity with ratings from users who share your taste cluster."
        };

        return new RecommendationResultDto
        {
            UserId = userId,
            Strategy = strategy,
            Message = message,
            Items = recommendations
        };
    }

    private static Dictionary<Guid, double> FindNeighborWeights(
        Guid userId,
        bool hasProfile,
        IReadOnlyCollection<Guid> genreIds,
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyDictionary<Guid, List<RatingSignal>> ratingsByUser,
        IReadOnlyDictionary<Guid, Dictionary<Guid, double>> explicitByUser,
        IReadOnlyDictionary<(Guid UserId, Guid GenreId), double> ratingSignalsByUserGenre,
        SparsePreferenceVector userVector)
    {
        if (!hasProfile || genreIds.Count == 0 || activeUserIds.Count < 2)
        {
            return [];
        }

        var samples = activeUserIds
            .Select(id => new PreferenceVectorSample(
                id,
                id == userId
                    ? userVector
                    : BuildUserVector(id, genreIds, explicitByUser, ratingSignalsByUserGenre)))
            .ToList();
        var clusterCount = Math.Min(
            samples.Count,
            Math.Max(2, (int)Math.Round(Math.Sqrt(samples.Count / 2d))));
        var clusters = KMeansClusterer.Cluster(samples, clusterCount, MaximumKMeansIterations);
        var currentCluster = clusters.FirstOrDefault(cluster => cluster.MemberIds.Contains(userId));
        if (currentCluster is null)
        {
            return [];
        }

        return samples
            .Where(sample => sample.Id != userId && currentCluster.MemberIds.Contains(sample.Id))
            .Select(sample => new
            {
                sample.Id,
                Similarity = Math.Max(0, CosineSimilarity.Calculate(userVector, sample.Vector))
            })
            .Where(item => item.Similarity > 0)
            .ToDictionary(item => item.Id, item => item.Similarity);
    }

    private static Dictionary<(Guid UserId, Guid GenreId), double> BuildRatingSignalsByUserGenre(
        IEnumerable<RatingSignal> ratings,
        IReadOnlyDictionary<Guid, Guid[]> movieGenreIds)
    {
        var signals = new Dictionary<(Guid UserId, Guid GenreId), List<double>>();
        foreach (var rating in ratings)
        {
            if (!movieGenreIds.TryGetValue(rating.MovieId, out var genres))
            {
                continue;
            }

            foreach (var genreId in genres)
            {
                var key = (rating.UserId, genreId);
                if (!signals.TryGetValue(key, out var scores))
                {
                    scores = [];
                    signals.Add(key, scores);
                }
                scores.Add(ToAffinity(rating.Score));
            }
        }

        return signals.ToDictionary(pair => pair.Key, pair => pair.Value.Average());
    }

    private static SparsePreferenceVector BuildUserVector(
        Guid userId,
        IEnumerable<Guid> genreIds,
        IReadOnlyDictionary<Guid, Dictionary<Guid, double>> explicitByUser,
        IReadOnlyDictionary<(Guid UserId, Guid GenreId), double> ratingSignalsByUserGenre)
    {
        explicitByUser.TryGetValue(userId, out var explicitPreferences);
        var values = genreIds.ToDictionary(
            genreId => genreId,
            genreId => explicitPreferences is not null && explicitPreferences.TryGetValue(genreId, out var explicitScore)
                ? ToAffinity(explicitScore)
                : ratingSignalsByUserGenre.TryGetValue((userId, genreId), out var inferredScore)
                    ? inferredScore
                    : 0d);
        return new SparsePreferenceVector(values);
    }

    private static MovieRecommendationDto CreateRecommendation(
        Movie movie,
        ScoredRecommendation content,
        IReadOnlyDictionary<Guid, double> neighborWeights,
        IReadOnlyDictionary<Guid, List<RatingSignal>> ratingsByUser,
        bool hasProfile)
    {
        double? collaborativeScore = null;
        var weightedRating = 0d;
        var totalWeight = 0d;
        foreach (var (neighborId, weight) in neighborWeights)
        {
            if (!ratingsByUser.TryGetValue(neighborId, out var neighborRatings))
            {
                continue;
            }

            var rating = neighborRatings.FirstOrDefault(item => item.MovieId == movie.Id);
            if (rating is null)
            {
                continue;
            }

            weightedRating += rating.Score * weight;
            totalWeight += weight;
        }

        if (totalWeight > 0)
        {
            collaborativeScore = weightedRating / totalWeight;
        }

        var score = HybridRecommendationScorer.Combine(
            content.Similarity,
            collaborativeScore,
            hasProfile,
            movie.CatalogRating);
        var reason = score.Strategy switch
        {
            "Popularity" => "Popular film to help you get started.",
            "Hybrid" => "Matches your genre taste and is rated by users with similar preferences.",
            _ => movie.Genres.Count == 0
                ? "Recommended from your broader taste profile."
                : $"Matches your interest in {string.Join(", ", movie.Genres.Select(genre => genre.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase))}."
        };

        return new MovieRecommendationDto
        {
            MovieId = movie.Id,
            Title = movie.Title,
            ReleaseYear = movie.ReleaseYear,
            PosterUrl = movie.PosterUrl,
            Genres = movie.Genres.Select(genre => genre.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(),
            Score = score.Value,
            ContentSimilarity = content.Similarity,
            CollaborativeScore = collaborativeScore,
            Reason = reason
        };
    }

    private static double ToAffinity(double score) => Math.Clamp((score - 5) / 5, -1, 1);

    private sealed record RatingSignal(Guid UserId, Guid MovieId, double Score);
    private sealed record ExplicitPreferenceSignal(Guid UserId, Guid GenreId, double Score);
}
