namespace MovieMind.Application.Algorithms;

public static class TopKSelector
{
    public static IReadOnlyList<ScoredRecommendation> Select(
        SparsePreferenceVector userVector,
        IEnumerable<RecommendationCandidate> candidates,
        int count,
        IEnumerable<Guid>? excludedMovieIds = null)
    {
        ArgumentNullException.ThrowIfNull(userVector);
        ArgumentNullException.ThrowIfNull(candidates);
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Result count cannot be negative.");
        }

        CosineSimilarity.ValidateValues(userVector.Values, nameof(userVector));
        if (count == 0)
        {
            return [];
        }

        var candidateList = candidates.ToList();
        if (candidateList.Any(candidate => candidate is null || candidate.Vector is null || candidate.Title is null))
        {
            throw new ArgumentException("Candidates, titles and vectors cannot be null.", nameof(candidates));
        }
        if (candidateList.Select(candidate => candidate.Id).Distinct().Count() != candidateList.Count)
        {
            throw new ArgumentException("Candidate IDs must be unique.", nameof(candidates));
        }

        foreach (var candidate in candidateList)
        {
            CosineSimilarity.ValidateValues(candidate.Vector.Values, nameof(candidates));
        }

        var excluded = excludedMovieIds?.ToHashSet() ?? [];
        var available = candidateList.Where(candidate => !excluded.Contains(candidate.Id));

        return available
            .Select(candidate => new ScoredRecommendation(
                candidate.Id,
                candidate.Title,
                CosineSimilarity.Calculate(userVector, candidate.Vector)))
            .OrderByDescending(candidate => candidate.Similarity)
            .ThenBy(candidate => candidate.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Id)
            .Take(count)
            .ToArray();
    }
}
