namespace MovieMind.Application.Algorithms;

public static class KMeansClusterer
{
    public static IReadOnlyList<PreferenceVectorCluster> Cluster(
        IEnumerable<PreferenceVectorSample> samples,
        int clusterCount,
        int maxIterations = 100)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (clusterCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(clusterCount), "At least one cluster is required.");
        }
        if (maxIterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxIterations), "At least one iteration is required.");
        }

        var points = samples.ToList();
        if (points.Count == 0)
        {
            throw new ArgumentException("At least one sample is required.", nameof(samples));
        }
        if (points.Any(sample => sample is null || sample.Vector is null))
        {
            throw new ArgumentException("Samples and their vectors cannot be null.", nameof(samples));
        }

        points = points.OrderBy(sample => sample.Id).ToList();
        if (clusterCount > points.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(clusterCount), "Cluster count cannot exceed sample count.");
        }
        if (points.Select(sample => sample.Id).Distinct().Count() != points.Count)
        {
            throw new ArgumentException("Sample IDs must be unique.", nameof(samples));
        }

        foreach (var point in points)
        {
            CosineSimilarity.ValidateValues(point.Vector.Values, nameof(samples));
        }

        var dimensions = points
            .SelectMany(point => point.Vector.Values.Keys)
            .Distinct()
            .Order()
            .ToArray();
        if (dimensions.Length == 0)
        {
            throw new ArgumentException("Samples must contain at least one vector dimension.", nameof(samples));
        }

        var values = points
            .Select(point => dimensions.Select(dimension =>
                point.Vector.Values.TryGetValue(dimension, out var value) ? value : 0d).ToArray())
            .ToArray();
        var centroids = InitializeCentroids(points, values, clusterCount);
        var assignments = Enumerable.Repeat(-1, points.Count).ToArray();

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var nextAssignments = Assign(values, centroids);
            var assignmentsUnchanged = assignments.SequenceEqual(nextAssignments);
            assignments = nextAssignments;
            centroids = RecalculateCentroids(values, assignments, centroids, clusterCount);

            if (assignmentsUnchanged)
            {
                break;
            }
        }

        assignments = Assign(values, centroids);
        centroids = RecalculateCentroids(values, assignments, centroids, clusterCount);

        return Enumerable.Range(0, clusterCount)
            .Select(index => new PreferenceVectorCluster(
                index,
                new SparsePreferenceVector(dimensions
                    .Select((dimension, position) => new KeyValuePair<Guid, double>(dimension, centroids[index][position]))
                    .ToDictionary(pair => pair.Key, pair => pair.Value)),
                points.Where((_, pointIndex) => assignments[pointIndex] == index)
                    .Select(point => point.Id)
                    .ToArray()))
            .ToArray();
    }

    private static double[][] InitializeCentroids(
        IReadOnlyList<PreferenceVectorSample> points,
        IReadOnlyList<double[]> values,
        int clusterCount)
    {
        var selected = new List<int> { 0 };
        while (selected.Count < clusterCount)
        {
            var bestIndex = -1;
            var bestDistance = double.NegativeInfinity;

            for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                if (selected.Contains(pointIndex))
                {
                    continue;
                }

                var nearestDistance = selected.Min(centroidIndex =>
                    SquaredDistance(values[pointIndex], values[centroidIndex]));
                if (nearestDistance > bestDistance)
                {
                    bestDistance = nearestDistance;
                    bestIndex = pointIndex;
                }
            }

            selected.Add(bestIndex);
        }

        return selected.Select(index => values[index].ToArray()).ToArray();
    }

    private static int[] Assign(IReadOnlyList<double[]> points, IReadOnlyList<double[]> centroids)
    {
        var assignments = new int[points.Count];
        for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
        {
            var nearestCluster = 0;
            var nearestDistance = SquaredDistance(points[pointIndex], centroids[0]);

            for (var clusterIndex = 1; clusterIndex < centroids.Count; clusterIndex++)
            {
                var distance = SquaredDistance(points[pointIndex], centroids[clusterIndex]);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestCluster = clusterIndex;
                }
            }

            assignments[pointIndex] = nearestCluster;
        }

        return assignments;
    }

    private static double[][] RecalculateCentroids(
        IReadOnlyList<double[]> points,
        IReadOnlyList<int> assignments,
        IReadOnlyList<double[]> previousCentroids,
        int clusterCount)
    {
        var centroids = new double[clusterCount][];
        for (var clusterIndex = 0; clusterIndex < clusterCount; clusterIndex++)
        {
            var members = Enumerable.Range(0, points.Count)
                .Where(pointIndex => assignments[pointIndex] == clusterIndex)
                .ToArray();
            if (members.Length == 0)
            {
                centroids[clusterIndex] = previousCentroids[clusterIndex].ToArray();
                continue;
            }

            centroids[clusterIndex] = Enumerable.Range(0, points[0].Length)
                .Select(dimension => members.Average(pointIndex => points[pointIndex][dimension]))
                .ToArray();
        }

        return centroids;
    }

    private static double SquaredDistance(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        var total = 0d;
        for (var dimension = 0; dimension < left.Count; dimension++)
        {
            var difference = left[dimension] - right[dimension];
            total += difference * difference;
        }

        return total;
    }
}
