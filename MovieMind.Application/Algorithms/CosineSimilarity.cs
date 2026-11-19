namespace MovieMind.Application.Algorithms;

public static class CosineSimilarity
{
    public static double Calculate(
        SparsePreferenceVector left,
        SparsePreferenceVector right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ValidateValues(left.Values, nameof(left));
        ValidateValues(right.Values, nameof(right));

        var leftNormSquared = left.Values.Values.Sum(value => value * value);
        var rightNormSquared = right.Values.Values.Sum(value => value * value);
        if (leftNormSquared == 0 || rightNormSquared == 0)
        {
            return 0;
        }

        var dotProduct = left.Values.Sum(pair =>
            right.Values.TryGetValue(pair.Key, out var rightValue)
                ? pair.Value * rightValue
                : 0);

        var similarity = dotProduct / Math.Sqrt(leftNormSquared * rightNormSquared);
        return Math.Clamp(similarity, -1, 1);
    }

    internal static void ValidateValues(
        IReadOnlyDictionary<Guid, double> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (values.Values.Any(value => !double.IsFinite(value)))
        {
            throw new ArgumentException("Vector values must be finite numbers.", parameterName);
        }
    }
}