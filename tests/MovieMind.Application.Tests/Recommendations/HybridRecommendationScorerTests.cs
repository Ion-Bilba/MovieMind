using MovieMind.Application.Algorithms;
using Xunit;

namespace MovieMind.Application.Tests.Recommendations;

public sealed class HybridRecommendationScorerTests
{
    [Fact]
    public void Combine_UsesPopularityForANewUser()
    {
        var result = HybridRecommendationScorer.Combine(0, null, false, 8);

        Assert.Equal("Popularity", result.Strategy);
        Assert.Equal(0.8, result.Value, 10);
    }

    [Fact]
    public void Combine_UsesContentScoreWhenThereAreNoCollaborativeSignals()
    {
        var result = HybridRecommendationScorer.Combine(0.6, null, true, 8);

        Assert.Equal("ContentBased", result.Strategy);
        Assert.Equal(0.8, result.Value, 10);
    }

    [Fact]
    public void Combine_WeightsContentAndCollaborativeSignals()
    {
        var result = HybridRecommendationScorer.Combine(1, 10, true, 0);

        Assert.Equal("Hybrid", result.Strategy);
        Assert.Equal(1, result.Value, 10);
    }

    [Fact]
    public void Combine_ClampsScoresToSupportedRanges()
    {
        var result = HybridRecommendationScorer.Combine(-2, 20, true, 0);

        Assert.Equal(0.35, result.Value, 10);
    }

    [Fact]
    public void Combine_RejectsNonFiniteInputs()
    {
        Assert.Throws<ArgumentException>(() => HybridRecommendationScorer.Combine(double.NaN, null, true, 5));
        Assert.Throws<ArgumentException>(() => HybridRecommendationScorer.Combine(0, double.PositiveInfinity, true, 5));
    }
}