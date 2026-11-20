namespace MovieMind.Application.Algorithms;

public readonly record struct RecommendationScore(double Value, string Strategy);

public static class HybridRecommendationScorer
{
    public static RecommendationScore Combine(
        double contentSimilarity,
        double? collaborativeRating,
        bool hasPreferenceOrRatingHistory,
        double catalogRating)
    {
        if (!double.IsFinite(contentSimilarity) || !double.IsFinite(catalogRating) ||
            collaborativeRating is { } rating && !double.IsFinite(rating))
        {
            throw new ArgumentException("Recommendation inputs must be finite numbers.");
        }

        if (!hasPreferenceOrRatingHistory)
        {
            return new RecommendationScore(Math.Clamp(catalogRating / 10, 0, 1), "Popularity");
        }

        var contentScore = (Math.Clamp(contentSimilarity, -1, 1) + 1) / 2;
        if (collaborativeRating is not { } collaborative)
        {
            return new RecommendationScore(contentScore, "ContentBased");
        }

        var collaborativeScore = Math.Clamp(collaborative / 10, 0, 1);
        return new RecommendationScore(
            0.65 * contentScore + 0.35 * collaborativeScore,
            "Hybrid");
    }
}