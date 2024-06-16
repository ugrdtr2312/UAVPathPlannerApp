using PathPlanning.Experiments.Options;

namespace PathPlanning.Experiments.Models;

public class SolverExperimentResult
{
    public Options.SolverOptions SolverOption { get; }

    public double MaxIterationsCoefficient { get; set; }

    public double TabuListSizeCoefficient { get; set; }

    public double ScoutBeesCoefficient { get; set; }

    public double ForagerBeesCoefficient { get; set; }

    public double TimeOfExecutionInTicks { get; set; }

    public double BestDeviationFromGreedyInPercents { get; set; } = double.MinValue;

    public double AverageDeviationFromGreedyInPercents { get; set; }

    public double WorstDeviationFromGreedyInPercents { get; set; } = double.MaxValue;

    public int BestResultTimes { get; set; }

    public double BestResultTimesInPercents { get; set; }

    public int WorstResultTimes { get; set; }

    public double WorstResultTimesInPercents { get; set; }

    public double MaxExecutionTimeInMs { get; set; }

    public double AverageExecutionTimeInMs { get; set; }

    public double MinExecutionTimeInMs { get; set; }

    public SolverExperimentResult(SolverOptions solverOption)
    {
        SolverOption = solverOption;
    }
}
