using Microsoft.EntityFrameworkCore;
using MovieMind.Application.Dtos.Ratings;
using MovieMind.Application.Interfaces;
using MovieMind.Domain.Entities;

namespace MovieMind.Application.Services;

public class RatingService : IRatingService
{
    private readonly IApplicationDbContext _context;

    public RatingService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RatingDto>?> GetMovieRatingsAsync(
        Guid movieId,
        CancellationToken cancellationToken = default)
    {
        var movieExists = await _context.Movies
            .AnyAsync(movie => movie.Id == movieId, cancellationToken);

        if (!movieExists)
        {
            return null;
        }

        return await _context.Ratings
            .AsNoTracking()
            .Where(rating => rating.MovieId == movieId)
            .OrderByDescending(rating => rating.UpdatedAt ?? rating.CreatedAt)
            .ThenByDescending(rating => rating.Id)
            .Select(rating => new RatingDto { Id = rating.Id, MovieId = rating.MovieId, MovieTitle = rating.Movie.Title, UserId = rating.UserId, UserName = rating.User.UserName, Score = rating.Score, CreatedAt = rating.CreatedAt, UpdatedAt = rating.UpdatedAt })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RatingDto>> GetUserRatingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Ratings
            .AsNoTracking()
            .Where(rating => rating.UserId == userId)
            .OrderByDescending(rating => rating.UpdatedAt ?? rating.CreatedAt)
            .ThenByDescending(rating => rating.Id)
            .Select(rating => new RatingDto { Id = rating.Id, MovieId = rating.MovieId, MovieTitle = rating.Movie.Title, UserId = rating.UserId, UserName = rating.User.UserName, Score = rating.Score, CreatedAt = rating.CreatedAt, UpdatedAt = rating.UpdatedAt })
            .ToListAsync(cancellationToken);
    }

    public async Task<RatingDto?> CreateAsync(
        Guid movieId,
        Guid userId,
        double score,
        CancellationToken cancellationToken = default)
    {
        var userName = await _context.Users
            .Where(user => user.Id == userId)
            .Select(user => user.UserName)
            .FirstOrDefaultAsync(cancellationToken);

        if (userName is null)
        {
            return null;
        }

        var movie = await _context.Movies
            .FirstOrDefaultAsync(item => item.Id == movieId, cancellationToken);

        if (movie is null)
        {
            return null;
        }

        var alreadyRated = await _context.Ratings
            .AnyAsync(rating => rating.MovieId == movieId && rating.UserId == userId,
                cancellationToken);

        if (alreadyRated)
        {
            throw new InvalidOperationException("Ai evaluat deja acest film. Modifică evaluarea existentă.");
        }

        var scores = await _context.Ratings
            .Where(rating => rating.MovieId == movieId)
            .Select(rating => rating.Score)
            .ToListAsync(cancellationToken);
        scores.Add(score);

        var rating = new Rating
        {
            Id = Guid.NewGuid(),
            MovieId = movieId,
            UserId = userId,
            Score = score,
            CreatedAt = DateTime.UtcNow
        };

        movie.RatingCount = scores.Count;
        movie.AverageRating = scores.Average();

        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(rating, userName, movie.Title);
    }

    public async Task<RatingDto?> UpdateMineAsync(
        Guid movieId,
        Guid userId,
        double score,
        CancellationToken cancellationToken = default)
    {
        var rating = await _context.Ratings
            .Include(item => item.User)
            .FirstOrDefaultAsync(
                item => item.MovieId == movieId && item.UserId == userId,
                cancellationToken);

        if (rating is null)
        {
            return null;
        }

        var movie = await _context.Movies
            .FirstOrDefaultAsync(item => item.Id == movieId, cancellationToken);

        if (movie is null)
        {
            return null;
        }

        var scores = await _context.Ratings
            .Where(item => item.MovieId == movieId)
            .Select(item => item.Score)
            .ToListAsync(cancellationToken);

        var scoreTotal = scores.Sum() - rating.Score + score;
        rating.Score = score;
        rating.UpdatedAt = DateTime.UtcNow;
        movie.RatingCount = scores.Count;
        movie.AverageRating = scores.Count == 0 ? 0 : scoreTotal / (double)scores.Count;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(rating, rating.User.UserName, movie.Title);
    }

private static RatingDto MapToDto(Rating rating, string userName, string movieTitle)
    {
        return new RatingDto
        {
            Id = rating.Id,
            MovieId = rating.MovieId,
            MovieTitle = movieTitle,
            UserId = rating.UserId,
            UserName = userName,
            Score = rating.Score,
            CreatedAt = rating.CreatedAt,
            UpdatedAt = rating.UpdatedAt
        };
    }
}
