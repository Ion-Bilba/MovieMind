using MovieMind.Application.Algorithms;
using Xunit;

namespace MovieMind.Application.Tests.Algorithms;

public sealed class KMeansClustererTests
{
    private static readonly Guid Action = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Drama = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Cluster_GroupsUsersWithSimilarPreferenceVectors()
    {
        var samples = new[]
        {
            Sample(1, 1, 0),
            Sample(2, 0.8, 0.1),
            Sample(3, -1, 0),
            Sample(4, -0.8, -0.1)
        };

        var clusters = KMeansClusterer.Cluster(samples, 2);

        Assert.Equal(2, clusters.Count);
        Assert.Equal(4, clusters.Sum(cluster => cluster.MemberIds.Count));
        Assert.Contains(clusters, cluster => cluster.MemberIds.Order().SequenceEqual([Id(1), Id(2)]));
        Assert.Contains(clusters, cluster => cluster.MemberIds.Order().SequenceEqual([Id(3), Id(4)]));
    }

    [Fact]
    public void Cluster_IsDeterministicForSameInput()
    {
        var samples = new[] { Sample(1, 1, 0), Sample(2, 0.8, 0.1), Sample(3, -1, 0), Sample(4, -0.8, -0.1) };

        var first = KMeansClusterer.Cluster(samples, 2);
        var second = KMeansClusterer.Cluster(samples.Reverse(), 2);

        Assert.Equal(first.Select(cluster => cluster.MemberIds), second.Select(cluster => cluster.MemberIds));
        Assert.Equal(first.SelectMany(cluster => cluster.Centroid.Values), second.SelectMany(cluster => cluster.Centroid.Values));
    }

    [Fact]
    public void Cluster_CentroidIsMeanOfAssignedPoints()
    {
        var clusters = KMeansClusterer.Cluster([Sample(1, 1, 0), Sample(2, 0.5, 0)], 1);

        Assert.Equal(0.75, clusters[0].Centroid.Values[Action], 10);
        Assert.Equal(0, clusters[0].Centroid.Values[Drama], 10);
    }

    [Fact]
    public void Cluster_RejectsEmptyInput()
    {
        Assert.Throws<ArgumentException>(() => KMeansClusterer.Cluster([], 1));
    }

    [Fact]
    public void Cluster_RejectsClusterCountGreaterThanSampleCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KMeansClusterer.Cluster([Sample(1, 1, 0)], 2));
    }

    [Fact]
    public void Cluster_RejectsVectorsWithoutDimensions()
    {
        var sample = new PreferenceVectorSample(Id(1), new SparsePreferenceVector(new Dictionary<Guid, double>()));

        Assert.Throws<ArgumentException>(() => KMeansClusterer.Cluster([sample], 1));
    }

    [Fact]
    public void Cluster_RejectsDuplicateSampleIds()
    {
        var sample = Sample(1, 1, 0);

        Assert.Throws<ArgumentException>(() => KMeansClusterer.Cluster([sample, sample], 1));
    }

    [Fact]
    public void Cluster_RejectsNullSamples()
    {
        PreferenceVectorSample?[] samples = [null];

        Assert.Throws<ArgumentException>(() => KMeansClusterer.Cluster(samples!, 1));
    }

    [Fact]
    public void Cluster_RejectsNonPositiveIterationLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KMeansClusterer.Cluster([Sample(1, 1, 0)], 1, 0));
    }

    private static PreferenceVectorSample Sample(int id, double action, double drama) =>
        new(Id(id), new SparsePreferenceVector(new Dictionary<Guid, double> { [Action] = action, [Drama] = drama }));

    private static Guid Id(int number) => Guid.Parse($"00000000-0000-0000-0000-{number:D12}");
}
