using PathPlanning.Entities;

namespace PathPlanning.Helpers;

public static class DistanceMatrixHelper
{
    public static double[,] CalculateFor2D(IReadOnlyList<Point> points)
    {
        return Calculate(points, true);
    }

    public static double[,] CalculateForGeographic(IReadOnlyList<Point> points)
    {
        return Calculate(points, false);
    }

    private static double[,] Calculate(IReadOnlyList<Point> points, bool is2D)
    {
        var n = points.Count;
        var distanceMatrix = new double[n, n];

        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                var distance = is2D
                    ? DistanceFor2D(points[i], points[j])
                    : DistanceForGeographic(points[i], points[j]);

                distanceMatrix[i, j] = Math.Round(distance, 5);
            }
        }

        return distanceMatrix;
    }

    private static double DistanceFor2D(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
    }

    private static double DistanceForGeographic(Point p1, Point p2)
    {
        const double earthRadiusInKilometers = 6371;

        var dLat = ToRadian(p2.X - p1.X);
        var dLon = ToRadian(p2.Y - p1.Y);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadian(p1.X)) * Math.Cos(ToRadian(p2.X)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));

        return earthRadiusInKilometers * c;
    }

    private static double ToRadian(double val)
    {
        return Math.PI / 180 * val;
    }
}
