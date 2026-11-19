namespace MovieMind.Application.Algorithms;

public sealed record SparsePreferenceVector(IReadOnlyDictionary<Guid, double> Values)
{
    public static SparsePreferenceVector FromGenres(IEnumerable<Guid> genreIds)
    {
        ArgumentNullException.ThrowIfNull(genreIds);

        var values = genreIds
            .Distinct()
            .ToDictionary(genreId => genreId, _ => 1d);

        return new SparsePreferenceVector(values);
    }
}

public sealed record PreferenceVectorSample(Guid Id, SparsePreferenceVector Vector);

public sealed record PreferenceVectorCluster(
    int Index,
    SparsePreferenceVector Centroid,
    IReadOnlyList<Guid> MemberIds);

public sealed record RecommendationCandidate(
    Guid Id,
    string Title,
    SparsePreferenceVector Vector);

public sealed record ScoredRecommendation(Guid Id, string Title, double Similarity);