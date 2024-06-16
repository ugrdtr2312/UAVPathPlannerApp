using PathPlanning.Experiments.Models;

namespace PathPlanning.Experiments;

public class BaseExperimentMetricsTool
{
    private readonly List<ExperimentResult> _problemExperimentResults;
    private List<SolverExperimentResult> _solverExperimentResults = new();

    private readonly List<Options.SolverOptions> _solverOptions = new()
    {
        Options.SolverOptions.Greedy,
        Options.SolverOptions.AbcRAn,
        Options.SolverOptions.AbcPAn,
        Options.SolverOptions.AbcPnAn,
        Options.SolverOptions.AbcRRp,
        Options.SolverOptions.AbcPRp,
        Options.SolverOptions.AbcPnRp,
        Options.SolverOptions.AbcRRpn,
        Options.SolverOptions.AbcPRpn,
        Options.SolverOptions.AbcPnRpn,
        Options.SolverOptions.AbcRRpaan,
        Options.SolverOptions.AbcPRpaan,
        Options.SolverOptions.AbcPnRpaan,
        Options.SolverOptions.AbcRRpnaan,
        Options.SolverOptions.AbcPRpnaan,
        Options.SolverOptions.AbcPnRpnaan,
        Options.SolverOptions.TabuRp,
        Options.SolverOptions.TabuRpn,
        Options.SolverOptions.TabuRpaan,
        Options.SolverOptions.TabuRpnaan
    };

    public BaseExperimentMetricsTool(List<ExperimentResult> problemExperimentResults)
    {
        _problemExperimentResults = problemExperimentResults;
    }

    public void Calculate()
    {
        _solverExperimentResults = _solverOptions.Select(solverOption => new SolverExperimentResult(solverOption)).ToList();

        foreach (var solverOption in _solverOptions)
        {
            var solverExperimentResult = _solverExperimentResults.Single(x => x.SolverOption == solverOption);

            var timeOfExecution = new List<double>();

            foreach (var problemExperimentResult in _problemExperimentResults)
            {
                timeOfExecution.AddRange(problemExperimentResult.Solutions
                    .Where(x => x.SolverOption == solverOption)
                    .Select(x => x.TimeOfExecutionInTicks)
                    .ToArray());

                var maxTotalWeight = problemExperimentResult.Solutions.Max(x => x.GetTotalWeight());

                var solverOptionsSolutions = problemExperimentResult.Solutions
                    .Where(x => Math.Abs(x.GetTotalWeight() - maxTotalWeight) < 0.000001)
                    .GroupBy(x => x.SolverOption)
                    .OrderByDescending(x => x.Count());

                var bestSolutionsCount = solverOptionsSolutions.First().Count();

                var bestSolverOptionsSolutions = solverOptionsSolutions
                    .Where(x => x.Count() == bestSolutionsCount)
                    .Select(x => x.Key);

                if (bestSolverOptionsSolutions.Contains(solverOption))
                    solverExperimentResult.BestResultTimes++;

                var minTotalWeight = problemExperimentResult.Solutions.Min(x => x.GetTotalWeight());

                solverOptionsSolutions = problemExperimentResult.Solutions
                    .Where(x => Math.Abs(x.GetTotalWeight() - minTotalWeight) < 0.000001)
                    .GroupBy(x => x.SolverOption)
                    .OrderBy(x => x.Count());

                var worstSolutionsCount = solverOptionsSolutions.First().Count();

                var worstSolverOptionsSolutions = solverOptionsSolutions
                    .Where(x => x.Count() == worstSolutionsCount)
                    .Select(x => x.Key);

                if (worstSolverOptionsSolutions.Contains(solverOption))
                    solverExperimentResult.WorstResultTimes++;

                var greedySolution = problemExperimentResult.Solutions
                    .First(x => x.SolverOption == Options.SolverOptions.Greedy);
                
                var bestDeviationFromGreedyInPercents = problemExperimentResult.Solutions
                    .Where(x => x.SolverOption == solverOption)
                    .Max(x => x.GetTotalWeight() / greedySolution.GetTotalWeight() - 1);

                if (solverExperimentResult.BestDeviationFromGreedyInPercents < bestDeviationFromGreedyInPercents)
                    solverExperimentResult.BestDeviationFromGreedyInPercents = bestDeviationFromGreedyInPercents;

                var worstDeviationFromGreedyInPercents = problemExperimentResult.Solutions
                    .Where(x => x.SolverOption == solverOption)
                    .Min(x => x.GetTotalWeight() / greedySolution.GetTotalWeight() - 1);

                if (solverExperimentResult.WorstDeviationFromGreedyInPercents > worstDeviationFromGreedyInPercents)
                    solverExperimentResult.WorstDeviationFromGreedyInPercents = worstDeviationFromGreedyInPercents;

                solverExperimentResult.AverageDeviationFromGreedyInPercents += problemExperimentResult.Solutions
                    .Where(x => x.SolverOption == solverOption)
                    .Average(x => x.GetTotalWeight() / greedySolution.GetTotalWeight() - 1);
            }

            solverExperimentResult.BestDeviationFromGreedyInPercents = Math.Round(solverExperimentResult.BestDeviationFromGreedyInPercents * 100, 2);

            solverExperimentResult.AverageDeviationFromGreedyInPercents = Math.Round(solverExperimentResult.AverageDeviationFromGreedyInPercents / _problemExperimentResults.Count * 100, 2);

            solverExperimentResult.WorstDeviationFromGreedyInPercents = Math.Round(solverExperimentResult.WorstDeviationFromGreedyInPercents * 100, 2);

            solverExperimentResult.BestResultTimesInPercents = Math.Round(solverExperimentResult.BestResultTimes / (double)_problemExperimentResults.Count * 100, 2);

            solverExperimentResult.WorstResultTimesInPercents = Math.Round(solverExperimentResult.WorstResultTimes / (double)_problemExperimentResults.Count * 100, 2);


            if (solverOption is Options.SolverOptions.TabuRp or Options.SolverOptions.TabuRpn or Options.SolverOptions.TabuRpaan or Options.SolverOptions.TabuRpnaan)
            {
                solverExperimentResult.MaxExecutionTimeInMs = Math.Round(timeOfExecution.Max() / 500000, 2);

                solverExperimentResult.AverageExecutionTimeInMs = Math.Round(timeOfExecution.Average() / 500000, 2);

                solverExperimentResult.MinExecutionTimeInMs = Math.Round(timeOfExecution.Min() / 500000, 2);
            }
            else if (solverOption != Options.SolverOptions.Greedy)
            {
                solverExperimentResult.MaxExecutionTimeInMs = Math.Round(timeOfExecution.Max() / 100000, 2);

                solverExperimentResult.AverageExecutionTimeInMs = Math.Round(timeOfExecution.Average() / 100000, 2);

                solverExperimentResult.MinExecutionTimeInMs = Math.Round(timeOfExecution.Min() / 100000, 2);
            }
            else
            {
                solverExperimentResult.MaxExecutionTimeInMs = Math.Round(timeOfExecution.Max() / 10000, 2);

                solverExperimentResult.AverageExecutionTimeInMs = Math.Round(timeOfExecution.Average() / 10000, 2);

                solverExperimentResult.MinExecutionTimeInMs = Math.Round(timeOfExecution.Min() / 10000, 2);
            }
        }
    }

    public byte[] OutputToMemoryStream()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);

        writer.Write("SolverOption,BestDeviationFromGreedyInPercents,AverageDeviationFromGreedyInPercents,WorstDeviationFromGreedyInPercents,BestResultTimes,BestResultTimesInPercents,");
        writer.Write("WorstResultTimes,WorstResultTimesInPercents,MaxExecutionTimeInMs,AverageExecutionTimeInMs,MinExecutionTimeInMs");
        writer.WriteLine();

        foreach (var result in _solverExperimentResults)
        {
            writer.Write($"{result.SolverOption},{result.BestDeviationFromGreedyInPercents},{result.AverageDeviationFromGreedyInPercents},{result.WorstDeviationFromGreedyInPercents},{result.BestResultTimes},{result.BestResultTimesInPercents},");
            writer.Write($"{result.WorstResultTimes},{result.WorstResultTimesInPercents},{result.MaxExecutionTimeInMs},{result.AverageExecutionTimeInMs},{result.MinExecutionTimeInMs}");
            writer.WriteLine();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }
}
