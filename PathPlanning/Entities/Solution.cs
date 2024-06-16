using PathPlanning.Experiments.Options;
using System.Text;

namespace PathPlanning.Entities;

public class Solution
{
    private readonly double _serviceTime;

    public List<SubPath> SubPaths { get; } = new();

    public SolverOptions SolverOption { get; private set; }
    
    public double MaxIterationsCoefficient { get; private set; }
    
    public double TabuListSizeCoefficient { get; private set; }
    
    public double ScoutBeesCoefficient { get; private set; }
    
    public double ForagerBeesCoefficient { get; private set; }

    public double TimeOfExecutionInTicks { get; private set; }

    public Solution(double serviceTime)
    {
        _serviceTime = serviceTime;
    }

    public void SetExecutionCharacteristics(SolverOptions solverOption, double timeOfExecutionInTicks)
    {
        SolverOption = solverOption;
        TimeOfExecutionInTicks = timeOfExecutionInTicks;
    }
    
    public void SetAbcExecutionCharacteristics(
            double maxIterationsCoefficient, 
            double scoutBeesCoefficient, 
            double foragerBeesCoefficient,  
            double timeOfExecutionInTicks)
    {
        SolverOption = SolverOptions.AbcPnRpnaan;
        MaxIterationsCoefficient = maxIterationsCoefficient;
        ScoutBeesCoefficient = scoutBeesCoefficient;
        ForagerBeesCoefficient = foragerBeesCoefficient;
        TimeOfExecutionInTicks = timeOfExecutionInTicks;
    }
    
    public void SetTabuExecutionCharacteristics(
            double maxIterationsCoefficient, 
            double tabuListSizeCoefficient, 
            double timeOfExecutionInTicks)
    {
        SolverOption = SolverOptions.TabuRpaan;
        MaxIterationsCoefficient = maxIterationsCoefficient;
        TabuListSizeCoefficient = tabuListSizeCoefficient;
        TimeOfExecutionInTicks = timeOfExecutionInTicks;
    }

    public double GetTotalWeight()
    {
        return SubPaths.Sum(x => x.GetTotalWeight());
    }

    public string GetTotalTime()
    {
        var totalTime = TimeSpan.FromHours(SubPaths.Sum(x => x.GetTimeInAir()) + _serviceTime * (SubPaths.Count - 1));

        return (int)totalTime.TotalHours > 0 
            ? $"{(int)totalTime.TotalHours} г. {totalTime.Minutes:D2} хв. {totalTime.Seconds:D2} с."
            : $"{totalTime.Minutes:D2} хв. {totalTime.Seconds:D2} с.";
    }

    public string GetTotalDistance(double speed)
    {
        var timeInAir = SubPaths.Sum(x => x.GetTimeInAir()) * speed * 1000;

        return $"{(int)(timeInAir / 1000)} км. {(int)(timeInAir % 1000)} м.";
    }

    public string[] GetSubPathsInfo()
    {
        return SubPaths.Select(x => x.GetInfo()).ToArray();
    }

    public string SubPathsToString()
    {
        var subPathCount = SubPaths.Count;
        var subPathIndex = 0;

        var sb = new StringBuilder();

        foreach (var subPath in SubPaths)
        {
            sb.AppendLine($"{subPath.StartBase.X}; {subPath.StartBase.Y}");

            foreach (var intelligenceObject in subPath.IntelligenceObjects)
            {
                sb.AppendLine($"{intelligenceObject.X}; {intelligenceObject.Y}");
            }

            if (++subPathIndex < subPathCount)
            {
                sb.AppendLine($"{subPath.EndBase.X}; {subPath.EndBase.Y}");
                sb.AppendLine();
            }
            else
            {
                sb.Append($"{subPath.EndBase.X}; {subPath.EndBase.Y}");
            }
        }

        return sb.ToString();
    }
}