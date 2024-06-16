using PathPlanning.Entities;
using PathPlanning.Exceptions;

namespace WebApplication.Tools;

public static class FileInputTool
{
    public static Problem ReadProblem(IFormFile file)
    {
        var bases = new List<Base>();
        var intelligenceObjects = new List<IntelligenceObject>();

        var isAllBasesRead = false;
        var index = 1;

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                switch (line)
                {
                    case "---":
                        isAllBasesRead = true;
                        index = 1;
                        continue;
                    case "***":
                        line = reader.ReadLine();

                        var values = line?.Split(';');

                        if (values?.Length == 3 &&
                            double.TryParse(values[0], out var maxTimeInAirInHours) &&
                            double.TryParse(values[1], out var speedInKmPerHour) &&
                            double.TryParse(values[2], out var serviceTimeInMinutes))
                        {
                            return new Problem(
                                bases,
                                intelligenceObjects,
                                maxTimeInAirInHours,
                                speedInKmPerHour,
                                serviceTimeInMinutes / 60,
                                true);
                        }

                        throw new FileInputException("Invalid data format for UAV characteristics.");
                }

                var coordinates = line?.Split(';');

                if (coordinates?.Length == 2 && !isAllBasesRead &&
                    double.TryParse(coordinates[0], out var x) &&
                    double.TryParse(coordinates[1], out var y))
                {
                    bases.Add(new Base(index, x, y));
                }

                if (coordinates?.Length == 3 && isAllBasesRead &&
                    double.TryParse(coordinates[0], out var xp) &&
                    double.TryParse(coordinates[1], out var yp) &&
                    double.TryParse(coordinates[2], out var wp))
                {
                    intelligenceObjects.Add(new IntelligenceObject(index, xp, yp, wp));
                }

                index++;
            }
        }
        catch (Exception)
        {
            throw new FileInputException("Invalid data format for points coordinate.");
        }

        return null!;
    }
}
