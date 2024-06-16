using PathPlanning.Entities;

namespace PathPlanning.Helpers;

public static class MovementCharacteristicsMatrixHelper
{
    public static MovementCharacteristics[,] Calculate(Problem problem)
    {
        var bases = problem.Bases.Select(x => x.GetPoint()).ToArray();
        var intelligenceObjects = problem.IntelligenceObjects.Select(x => x.GetPoint()).ToArray();
        var points = intelligenceObjects.Concat(bases).ToArray();

        var distanceMatrix = problem.IsGeographic
            ? DistanceMatrixHelper.CalculateForGeographic(points)
            : DistanceMatrixHelper.CalculateFor2D(points);

        var movementCharacteristicsMatrix = new MovementCharacteristics[problem.PointsCount, problem.PointsCount];

        for (var j = 0; j < problem.PointsCount; j++)
        {
            for (var i = 0; i < problem.PointsCount; i++)
            {
                var averageWeight = double.MinValue;

                if (i == j)
                {
                    movementCharacteristicsMatrix[i, j] = new MovementCharacteristics(averageWeight, double.MaxValue);
                    continue;
                }

                var time = distanceMatrix[i, j] / problem.SpeedInKmPerHour;

                if (i > intelligenceObjects.Length - 1)
                {
                    averageWeight = 0;
                }
                else
                {
                    averageWeight = problem.IntelligenceObjects[i].Weight / time;
                }

                movementCharacteristicsMatrix[i, j] = new MovementCharacteristics(averageWeight, time);
            }
        }

        return movementCharacteristicsMatrix;
    }
}