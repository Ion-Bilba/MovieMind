namespace MovieMind.Application.Dtos.Recommendations;

public sealed class RecommendationResultDto
{
    public Guid UserId { get; init; }
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public string Strategy { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<MovieRecommendationDto> Items { get; init; } = [];
}

public sealed class MovieRecommendationDto
{
    public Guid MovieId { get; init; }
    public string Title { get; init; } = string.Empty;
    public int ReleaseYear { get; init; }
    public string PosterUrl { get; init; } = string.Empty;
    public IReadOnlyList<string> Genres { get; init; } = [];
    public double Score { get; init; }
    public double ContentSimilarity { get; init; }
    public double? CollaborativeScore { get; init; }
    public string Reason { get; init; } = string.Empty;
}