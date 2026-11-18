using MovieMind.Application.Dtos.Ratings;

namespace MovieMind.Application.Interfaces;

public interface IRatingService
{
    Task<IReadOnlyList<RatingDto>?> GetMovieRatingsAsync(
        Guid movieId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RatingDto>> GetUserRatingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<RatingDto?> CreateAsync(
        Guid movieId,
        Guid userId,
        double score,
        CancellationToken cancellationToken = default);

    Task<RatingDto?> UpdateMineAsync(
        Guid movieId,
        Guid userId,
        double score,
        CancellationToken cancellationToken = default);
}
