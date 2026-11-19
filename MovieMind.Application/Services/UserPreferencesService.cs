using Microsoft.EntityFrameworkCore;
using MovieMind.Application.Dtos.Preferences;
using MovieMind.Application.Interfaces;
using MovieMind.Domain.Entities;

namespace MovieMind.Application.Services;

public sealed class UserPreferencesService(IApplicationDbContext context) : IUserPreferencesService
{
    public async Task<UserPreferencesDto?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!await context.Users.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken))
        {
            return null;
        }

        return await BuildPreferencesAsync(userId, cancellationToken);
    }

    public async Task<UserPreferencesDto?> UpdateAsync(
        Guid userId,
        UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await context.Users.AnyAsync(user => user.Id == userId, cancellationToken))
        {
            return null;
        }

        var inputs = request.GenrePreferences;
        if (inputs.Any(input => input.GenreId == Guid.Empty ||
                                !double.IsFinite(input.Score) ||
                                input.Score is < 0 or > 10))
        {
            throw new ArgumentException("Fiecare preferință trebuie să aibă un gen valid și un scor între 0 și 10.");
        }

        if (inputs.Select(input => input.GenreId).Distinct().Count() != inputs.Count)
        {
            throw new ArgumentException("Lista conține genuri duplicate.");
        }

        var requestedGenreIds = inputs.Select(input => input.GenreId).ToArray();
        var existingGenreCount = await context.Genres
            .CountAsync(genre => requestedGenreIds.Contains(genre.Id), cancellationToken);
        if (existingGenreCount != requestedGenreIds.Length)
        {
            return null;
        }

        var profile = await context.UserPreferenceProfiles
            .Include(item => item.GenrePreferences)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new UserPreferenceProfile { UserId = userId };
            context.UserPreferenceProfiles.Add(profile);
        }

        var submittedScores = inputs.ToDictionary(input => input.GenreId, input => input.Score);
        var currentPreferences = profile.GenrePreferences.ToDictionary(item => item.GenreId);

        foreach (var current in profile.GenrePreferences.ToList())
        {
            if (!submittedScores.TryGetValue(current.GenreId, out var score))
            {
                context.UserGenrePreferences.Remove(current);
                continue;
            }

            current.Score = score;
        }

        foreach (var input in inputs)
        {
            if (!currentPreferences.ContainsKey(input.GenreId))
            {
                profile.GenrePreferences.Add(new UserGenrePreference
                {
                    UserId = userId,
                    GenreId = input.GenreId,
                    Score = input.Score
                });
            }
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return await BuildPreferencesAsync(userId, cancellationToken);
    }

    private static double ToAffinity(double score) => Math.Clamp((score - 5) / 5, -1, 1);

    private async Task<UserPreferencesDto> BuildPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var updatedAt = await context.UserPreferenceProfiles
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => (DateTime?)item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var explicitPreferenceRows = await context.UserGenrePreferences
            .AsNoTracking()
            .Where(preference => preference.UserId == userId)
            .Select(preference => new
            {
                preference.GenreId,
                GenreName = preference.Genre.Name,
                preference.Score
            })
            .ToListAsync(cancellationToken);

        var genres = await context.Genres
            .AsNoTracking()
            .OrderBy(genre => genre.Name)
            .Select(genre => new { genre.Id, genre.Name })
            .ToListAsync(cancellationToken);

        var userRatings = await context.Ratings
            .AsNoTracking()
            .Where(rating => rating.UserId == userId)
            .Select(rating => new { rating.MovieId, rating.Score })
            .ToListAsync(cancellationToken);

        var ratedMovieIds = userRatings.Select(rating => rating.MovieId).Distinct().ToArray();
        var ratedMovies = await context.Movies
            .AsNoTracking()
            .Where(movie => ratedMovieIds.Contains(movie.Id))
            .Include(movie => movie.Genres)
            .ToListAsync(cancellationToken);

        var scoresByMovie = userRatings.ToDictionary(rating => rating.MovieId, rating => rating.Score);
        var inferredScores = ratedMovies
            .SelectMany(movie => movie.Genres.Select(genre => new
            {
                GenreId = genre.Id,
                Score = scoresByMovie[movie.Id]
            }))
            .GroupBy(item => item.GenreId)
            .ToDictionary(group => group.Key, group => group.Average(item => item.Score));
        var explicitPreferences = explicitPreferenceRows
            .ToDictionary(item => item.GenreId, item => item);

        var vector = genres.Select(genre =>
        {
            if (explicitPreferences.TryGetValue(genre.Id, out var explicitPreference))
            {
                return new PreferenceVectorEntryDto
                {
                    GenreId = genre.Id,
                    GenreName = genre.Name,
                    Value = ToAffinity(explicitPreference.Score),
                    Signal = "ExplicitPreference"
                };
            }

            if (inferredScores.TryGetValue(genre.Id, out var inferredScore))
            {
                return new PreferenceVectorEntryDto
                {
                    GenreId = genre.Id,
                    GenreName = genre.Name,
                    Value = ToAffinity(inferredScore),
                    Signal = "RatingHistory"
                };
            }

            return new PreferenceVectorEntryDto
            {
                GenreId = genre.Id,
                GenreName = genre.Name,
                Value = 0,
                Signal = "Neutral"
            };
        }).ToList();

        var preferences = explicitPreferenceRows
            .OrderBy(item => item.GenreName)
            .Select(item => new GenrePreferenceDto
            {
                GenreId = item.GenreId,
                GenreName = item.GenreName,
                Score = item.Score
            })
            .ToList();

        return new UserPreferencesDto
        {
            UserId = userId,
            UpdatedAt = updatedAt,
            GenrePreferences = preferences,
            Vector = vector
        };
    }
}