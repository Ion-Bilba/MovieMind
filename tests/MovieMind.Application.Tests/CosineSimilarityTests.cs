using MovieMind.Application.Algorithms;
using Xunit;

namespace MovieMind.Application.Tests.Algorithms;

public sealed class CosineSimilarityTests
{
    private static readonly Guid Action = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Drama = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Calculate_ReturnsOneForIdenticalVectors()
    {
        var vector = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = 0.8, [Drama] = -0.4 });

        Assert.Equal(1, CosineSimilarity.Calculate(vector, vector), 10);
    }

    [Fact]
    public void Calculate_ReturnsZeroForOrthogonalVectors()
    {
        var left = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = 1 });
        var right = new SparsePreferenceVector(new Dictionary<Guid, double> { [Drama] = 1 });

        Assert.Equal(0, CosineSimilarity.Calculate(left, right));
    }

    [Fact]
    public void Calculate_ReturnsNegativeOneForOppositeVectors()
    {
        var left = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = 1, [Drama] = -1 });
        var right = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = -1, [Drama] = 1 });

        Assert.Equal(-1, CosineSimilarity.Calculate(left, right), 10);
    }

    [Fact]
    public void Calculate_AlignsSparseVectorsByGenreId()
    {
        var left = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = 1 });
        var right = new SparsePreferenceVector(new Dictionary<Guid, double> { [Drama] = 1, [Action] = 1 });

        Assert.Equal(1 / Math.Sqrt(2), CosineSimilarity.Calculate(left, right), 10);
    }

    [Fact]
    public void Calculate_ReturnsZeroWhenEitherVectorHasZeroMagnitude()
    {
        var zero = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = 0, [Drama] = 0 });
        var nonZero = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = 1 });

        Assert.Equal(0, CosineSimilarity.Calculate(zero, nonZero));
        Assert.Equal(0, CosineSimilarity.Calculate(new SparsePreferenceVector(new Dictionary<Guid, double>()), nonZero));
    }

    [Fact]
    public void Calculate_RejectsNonFiniteValues()
    {
        var invalid = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = double.NaN });
        var valid = new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = 1 });

        Assert.Throws<ArgumentException>(() => CosineSimilarity.Calculate(invalid, valid));
    }

    [Fact]
    public void FromGenres_CreatesOneHotValuesAndIgnoresDuplicates()
    {
        var vector = SparsePreferenceVector.FromGenres([Action, Drama, Action]);

        Assert.Equal(2, vector.Values.Count);
        Assert.Equal(1, vector.Values[Action]);
        Assert.Equal(1, vector.Values[Drama]);
    }
}