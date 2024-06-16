namespace PathPlanning.Entities;

public class SubPath : ICloneable
{
    public Base StartBase { get; }

    public Base EndBase { get; }
    
    public List<IntelligenceObject> IntelligenceObjects { get; private set; } = new();

    public double TimeToEndBase { get; private set; }

    public SubPath(Base startBase, Base endBase)
    {
        StartBase = startBase;
        EndBase = endBase;
    }

    public void SetTimeToEndBase(double timeToEndBase)
    {
        TimeToEndBase = timeToEndBase;
    }

    public double GetTotalWeight()
    {
        return IntelligenceObjects.Sum(x => x.Weight);
    }

    public double GetTimeInAir()
    {
        return IntelligenceObjects.Sum(x => x.TimeForMovement) + TimeToEndBase;
    }

    public string GetInfo()
    {
        var path = $"База {StartBase.Id} -> " +
                   string.Join(" ", IntelligenceObjects.Select(x => $"Ціль {x.Id} ->")) +
                   $" База {EndBase.Id}";

        var timeInAir = TimeSpan.FromHours(GetTimeInAir());
        var timeInfo = (int)timeInAir.TotalHours > 0
            ? $"({(int)timeInAir.TotalHours} г. {timeInAir.Minutes:D2} хв. {timeInAir.Seconds:D2} с.)"
            : $"({timeInAir.Minutes:D2} хв. {timeInAir.Seconds:D2} с.)";

        return $"{path} {timeInfo}";
    }

    public void OutputToConsole()
    {
        Console.WriteLine($"A{StartBase.Id} " + string.Join(" ", IntelligenceObjects.Select(x => x.Id)) + $" A{EndBase.Id}");

        Console.WriteLine($"SubPath weight: {GetTotalWeight()}");

        var timeInAir = TimeSpan.FromHours(GetTimeInAir());
        Console.WriteLine($"SubPath time: {(int)timeInAir.TotalHours}h:{timeInAir.Minutes:D2}m:{timeInAir.Seconds:D2}s");

        Console.WriteLine($"SubPath weight/time: {Math.Round(GetTotalWeight() / GetTimeInAir(), 2)}{Environment.NewLine}");
    }

    public object Clone()
    {
        var startBase = (Base)StartBase.Clone();
        var endBase = (Base)EndBase.Clone();

        var intelligenceObjects = IntelligenceObjects
            .Select(io => (IntelligenceObject)io.Clone())
            .ToList();

        var subPath = new SubPath(startBase, endBase)
        {
            IntelligenceObjects = intelligenceObjects
        };

        if (TimeToEndBase > 0)
        {
            subPath.SetTimeToEndBase(TimeToEndBase);
        }

        return subPath;
    }
}
