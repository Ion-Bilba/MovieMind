using MovieMind.Application.Algorithms;
using Xunit;

namespace MovieMind.Application.Tests.Algorithms;

public sealed class TopKSelectorTests
{
    private static readonly Guid Action = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Drama = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Select_RanksMoviesByCosineSimilarity()
    {
        var user = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = 1, [Drama] = -0.5 });
        var candidates = new[]
        {
            Candidate("Drama", new Dictionary<Guid, double> { [Drama] = 1 }),
            Candidate("Action", new Dictionary<Guid, double> { [Action] = 1 }),
            Candidate("Mixed", new Dictionary<Guid, double> { [Action] = 1, [Drama] = -0.5 })
        };

        var result = TopKSelector.Select(user, candidates, 2);

        Assert.Equal(["Mixed", "Action"], result.Select(item => item.Title));
        Assert.Equal(1, result[0].Similarity, 10);
    }

    [Fact]
    public void Select_ExcludesMoviesAndUsesStableTitleTieBreak()
    {
        var user = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = 1 });
        var excludedId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var candidates = new[]
        {
            new RecommendationCandidate(excludedId, "A excluded", new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = 1 })),
            Candidate("Zulu", new Dictionary<Guid, double> { [Action] = 1 }),
            Candidate("Alpha", new Dictionary<Guid, double> { [Action] = 1 })
        };

        var result = TopKSelector.Select(user, candidates, 5, [excludedId]);

        Assert.Equal(["Alpha", "Zulu"], result.Select(item => item.Title));
        Assert.DoesNotContain(result, item => item.Id == excludedId);
    }

    [Fact]
    public void Select_ReturnsEmptyForZeroCount()
    {
        var result = TopKSelector.Select(
            new SparsePreferenceVector(new Dictionary<Guid, double>()),
            [],
            0);

        Assert.Empty(result);
    }

    [Fact]
    public void Select_RejectsNegativeCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TopKSelector.Select(
            new SparsePreferenceVector(new Dictionary<Guid, double>()), [], -1));
    }

    [Fact]
    public void Select_RejectsDuplicateCandidateIds()
    {
        var id = Guid.NewGuid();
        var vector = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = 1 });
        RecommendationCandidate[] candidates =
        [
            new(id, "First", vector),
            new(id, "Second", vector)
        ];

        Assert.Throws<ArgumentException>(() => TopKSelector.Select(vector, candidates, 2));
    }

    private static RecommendationCandidate Candidate(string title, IReadOnlyDictionary<Guid, double> vector) =>
        new(Guid.NewGuid(), title, new SparsePreferenceVector(vector));
}
