using PathPlanning.Experiments.Models;
using PathPlanning.Experiments.Options;

namespace PathPlanning.Experiments;

public class TabuExperimentMetricsTool
{
    private readonly List<ExperimentResult> _problemExperimentResults;
    private readonly List<SolverExperimentResult> _solverExperimentResults = new ();

    private readonly List<TabuOptions> _tabuOptions = new()
    {
        TabuOptions.A,
        TabuOptions.B,
        TabuOptions.C,
        TabuOptions.D,
        TabuOptions.E
    };

    public TabuExperimentMetricsTool(List<ExperimentResult> problemExperimentResults)
    {
        _problemExperimentResults = problemExperimentResults;
    }

    public void Calculate()
    {
        foreach (var maxIterationsCoefficient in _tabuOptions)
        {
            foreach (var tabuListSizeCoefficient in _tabuOptions)
            {
                var solverExperimentResult = new SolverExperimentResult(SolverOptions.TabuRpaan)
                    {
                        MaxIterationsCoefficient = (int)maxIterationsCoefficient / 4d,
                        TabuListSizeCoefficient = (int)tabuListSizeCoefficient / 4d
                    };

                var timeOfExecution = new List<double>();

                foreach (var problemExperimentResult in _problemExperimentResults)
                {
                    timeOfExecution.AddRange(problemExperimentResult.Solutions
                        .Where(x => 
                            Math.Abs(x.MaxIterationsCoefficient - solverExperimentResult.MaxIterationsCoefficient) < 0.00001 &&
                            Math.Abs(x.TabuListSizeCoefficient - solverExperimentResult.TabuListSizeCoefficient) < 0.00001)
                        .Select(x => x.TimeOfExecutionInTicks)
                        .ToArray());

                    var maxTotalWeight = problemExperimentResult.Solutions.Max(x => x.GetTotalWeight());

                    var solverOptionsSolutions = problemExperimentResult.Solutions
                        .Where(x => Math.Abs(x.GetTotalWeight() - maxTotalWeight) < 0.000001)
                        .GroupBy(x => new { x.MaxIterationsCoefficient, x.TabuListSizeCoefficient })
                        .OrderByDescending(x => x.Count());

                    var bestSolutionsCount = solverOptionsSolutions.First().Count();

                    var bestSolverOptionsSolutions = solverOptionsSolutions
                        .Where(x => x.Count() == bestSolutionsCount)
                        .Select(x => x.Key);

                    if (bestSolverOptionsSolutions.Contains(new { solverExperimentResult.MaxIterationsCoefficient, solverExperimentResult.TabuListSizeCoefficient }))
                        solverExperimentResult.BestResultTimes++;

                    var minTotalWeight = problemExperimentResult.Solutions.Min(x => x.GetTotalWeight());

                    if (Math.Abs(minTotalWeight - maxTotalWeight) > 0.00001)
                    {
                        solverOptionsSolutions = problemExperimentResult.Solutions
                            .Where(x => Math.Abs(x.GetTotalWeight() - minTotalWeight) < 0.000001)
                            .GroupBy(x => new { x.MaxIterationsCoefficient, x.TabuListSizeCoefficient })
                            .OrderBy(x => x.Count());

                        var worstSolutionsCount = solverOptionsSolutions.First().Count();

                        var worstSolverOptionsSolutions = solverOptionsSolutions
                            .Where(x => x.Count() == worstSolutionsCount)
                            .Select(x => x.Key);

                        if (worstSolverOptionsSolutions.Contains(new { solverExperimentResult.MaxIterationsCoefficient, solverExperimentResult.TabuListSizeCoefficient }))
                            solverExperimentResult.WorstResultTimes++;
                    }

                    var greedySolution = problemExperimentResult.Solutions
                        .Where(x => x is { MaxIterationsCoefficient: 1, TabuListSizeCoefficient: 1 })
                        .Average(x => x.GetTotalWeight());

                    var bestDeviationFromGreedyInPercents = problemExperimentResult.Solutions
                        .Where(x =>
                            Math.Abs(x.MaxIterationsCoefficient - solverExperimentResult.MaxIterationsCoefficient) < 0.00001 &&
                            Math.Abs(x.TabuListSizeCoefficient - solverExperimentResult.TabuListSizeCoefficient) < 0.00001)
                        .Max(x => x.GetTotalWeight() / greedySolution - 1);

                    if (solverExperimentResult.BestDeviationFromGreedyInPercents < bestDeviationFromGreedyInPercents)
                        solverExperimentResult.BestDeviationFromGreedyInPercents = bestDeviationFromGreedyInPercents;

                    var worstDeviationFromGreedyInPercents = problemExperimentResult.Solutions
                        .Where(x =>
                            Math.Abs(x.MaxIterationsCoefficient - solverExperimentResult.MaxIterationsCoefficient) < 0.00001 &&
                            Math.Abs(x.TabuListSizeCoefficient - solverExperimentResult.TabuListSizeCoefficient) < 0.00001)
                        .Min(x => x.GetTotalWeight() / greedySolution - 1);

                    if (solverExperimentResult.WorstDeviationFromGreedyInPercents > worstDeviationFromGreedyInPercents)
                        solverExperimentResult.WorstDeviationFromGreedyInPercents = worstDeviationFromGreedyInPercents;

                    solverExperimentResult.AverageDeviationFromGreedyInPercents += problemExperimentResult.Solutions
                        .Where(x =>
                            Math.Abs(x.MaxIterationsCoefficient - solverExperimentResult.MaxIterationsCoefficient) < 0.00001 &&
                            Math.Abs(x.TabuListSizeCoefficient - solverExperimentResult.TabuListSizeCoefficient) < 0.00001)
                        .Average(x => x.GetTotalWeight() / greedySolution - 1);
                }

                solverExperimentResult.BestDeviationFromGreedyInPercents = Math.Round(solverExperimentResult.BestDeviationFromGreedyInPercents * 100, 2);

                solverExperimentResult.AverageDeviationFromGreedyInPercents = Math.Round(solverExperimentResult.AverageDeviationFromGreedyInPercents / _problemExperimentResults.Count * 100, 2);

                solverExperimentResult.WorstDeviationFromGreedyInPercents = Math.Round(solverExperimentResult.WorstDeviationFromGreedyInPercents * 100, 2);

                solverExperimentResult.BestResultTimesInPercents = Math.Round(solverExperimentResult.BestResultTimes / (double)_problemExperimentResults.Count * 100, 2);

                solverExperimentResult.WorstResultTimesInPercents = Math.Round(solverExperimentResult.WorstResultTimes / (double)_problemExperimentResults.Count * 100, 2);

                solverExperimentResult.MaxExecutionTimeInMs = Math.Round(timeOfExecution.Max() / 200000, 2);

                solverExperimentResult.AverageExecutionTimeInMs = Math.Round(timeOfExecution.Average() / 200000, 2);

                solverExperimentResult.MinExecutionTimeInMs = Math.Round(timeOfExecution.Min() / 200000, 2);

                _solverExperimentResults.Add(solverExperimentResult);
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
