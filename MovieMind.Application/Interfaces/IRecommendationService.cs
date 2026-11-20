using MovieMind.Application.Dtos.Recommendations;

namespace MovieMind.Application.Interfaces;

public interface IRecommendationService
{
    Task<RecommendationResultDto?> GetForUserAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default);
}