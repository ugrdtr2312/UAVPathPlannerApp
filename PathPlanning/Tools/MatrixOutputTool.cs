using PathPlanning.Entities;

namespace PathPlanning.Tools;

public static class MatrixOutputTool
{
    public static void ToCsv(MovementCharacteristics[,] matrix, bool averageWeight = true)
    {
        const string filePath = @"C:\Users\Volodymyr_Shenheliia\Desktop\Diploma\UAVPathPlanner\UAVPathPlannerSandbox\Data\matrix.csv";

        using var writer = new StreamWriter(filePath);

        var rows = matrix.GetLength(0);
        var cols = matrix.GetLength(1);

        for (var j = 0; j < rows; j++)
        {
            for (var i = 0; i < cols; i++)
            {
                if (i > 0)
                    writer.Write(",");

                writer.Write(averageWeight
                    ? Math.Round(matrix[i, j].AverageWeight, 2)
                    : Math.Round(matrix[i, j].Time, 2));
            }

            writer.WriteLine();
        }
    }
}
