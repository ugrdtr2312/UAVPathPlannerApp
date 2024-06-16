using PathPlanning.Helpers;

namespace PathPlanning.Entities;

public class Problem : ICloneable
{
    public List<Base> Bases { get; }

    public List<IntelligenceObject> IntelligenceObjects { get; }

    public double MaxTimeInAirInHours { get; }

    public double SpeedInKmPerHour { get; }

    public double ServiceTimeInHours { get; }

    public bool IsGeographic { get; }

    public int BasesCount { get; private set; }

    public int SubPathCount { get; private set; }

    public int IntelligenceObjectsCount { get; private set; }

    public int IntelligenceObjectsOnSubPath { get; private set; }

    public int PointsCount { get; private set; }

    public MovementCharacteristics[,] MovementCharacteristicsMatrix { get; private set; }

    public Problem(
        List<Base> bases,
        List<IntelligenceObject> intelligenceObjects,
        double maxTimeInAirInHours,
        double speedInKmPerHour,
        double serviceTimeInHours,
        bool isGeographic)
    {
        Bases = bases;
        IntelligenceObjects = intelligenceObjects;
        MaxTimeInAirInHours = maxTimeInAirInHours;
        SpeedInKmPerHour = speedInKmPerHour;
        ServiceTimeInHours = serviceTimeInHours;
        IsGeographic = isGeographic;
    }

    public void Init()
    {
        BasesCount = Bases.Count;
        SubPathCount = BasesCount - 1;
        IntelligenceObjectsCount = IntelligenceObjects.Count;
        IntelligenceObjectsOnSubPath = IntelligenceObjectsCount / SubPathCount;
        PointsCount = IntelligenceObjectsCount + BasesCount;
        MovementCharacteristicsMatrix = MovementCharacteristicsMatrixHelper.Calculate(this);
    }

    public object Clone()
    {
        var bases = Bases
            .Select(b => (Base)b.Clone())
            .ToList();
        
        var intelligenceObjects = IntelligenceObjects
            .Select(io => (IntelligenceObject)io.Clone())
            .ToList();

        var problem = new Problem(
            bases,
            intelligenceObjects,
            MaxTimeInAirInHours,
            SpeedInKmPerHour,
            ServiceTimeInHours,
            IsGeographic);

        problem.Init();

        return problem;
    }
}

public class MovementCharacteristics
{
    public double AverageWeight { get; }

    public double Time { get; }

    public MovementCharacteristics(double averageWeight, double time)
    {
        AverageWeight = averageWeight;
        Time = time;
    }
}