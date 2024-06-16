using System.Diagnostics;
using PathPlanning.Entities;
using PathPlanning.Experiments.Models;
using PathPlanning.Experiments.Options;
using PathPlanning.Solvers;
using PathPlanning.Solvers.Options;
using PathPlanning.Tools;

namespace PathPlanning.Experiments;

public class TabuExperimentBuilder
{
    private const int CountOfProblems = 2;
    private const int CountOfIterationsPerProblem = 2;

    private const int BasesCount = 3;
    private const int IntelligenceObjectsCount = 30;

    private readonly List<TabuOptions> _tabuOptions = new()
    {
        TabuOptions.A,
        TabuOptions.B,
        TabuOptions.C,
        TabuOptions.D,
        TabuOptions.E
    };

    public List<ExperimentResult> Run()
    {
        var problems = new Problem[CountOfProblems];

        for (var i = 0; i < CountOfProblems; i++)
        {
            var generator = new GenerateProblemTool(BasesCount, IntelligenceObjectsCount, 2, true);
            problems[i] = generator.Generate();
        }

        var results = new List<ExperimentResult>();

        for (var p = 0; p < CountOfProblems; p++)
        {
            var problemExperimentResult = new ExperimentResult();

            foreach (var maxIterationsCoefficient in _tabuOptions)
            {
                foreach (var tabuListSizeCoefficient in _tabuOptions)
                {
                    for (var i = 0; i < CountOfIterationsPerProblem; i++)
                    {
                        var problem = (Problem)problems[p].Clone();

                        problem.Init();

                        var solver = new TabuSolver(
                            problem, 
                            LocalOptimizationOptions.RebuildProbableAndAddNearest,
                            (int)maxIterationsCoefficient / 4d,
                            (int)tabuListSizeCoefficient / 4d);

                        var stopwatch = new Stopwatch();

                        stopwatch.Start();

                        var solution = solver.Solve();

                        stopwatch.Stop();

                        solution.SetTabuExecutionCharacteristics(
                            (int)maxIterationsCoefficient / 4d,
                            (int)tabuListSizeCoefficient / 4d,
                            stopwatch.ElapsedTicks);

                        problemExperimentResult.Solutions.Add(solution);
                    }
                }
            }

            results.Add(problemExperimentResult);

            Console.WriteLine($"Problem {p + 1} solved");
        }

        return results;
    }
}