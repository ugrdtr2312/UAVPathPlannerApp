using System.Diagnostics;
using PathPlanning.Entities;
using PathPlanning.Experiments.Models;
using PathPlanning.Solvers;
using PathPlanning.Solvers.Interfaces;
using PathPlanning.Solvers.Options;
using PathPlanning.Tools;

namespace PathPlanning.Experiments;

public class BaseExperimentBuilder
{
    private readonly int _minBasesCount;
    private readonly int _maxBasesCount;
    private readonly int _problemsCount;
    private readonly int _problemRunsCount;
    private readonly int _minIntelligenceObjectsCount;
    private readonly int _maxIntelligenceObjectsCount;
    private readonly int _intelligenceObjectsCountStep;
    private readonly bool _isEquivalent;

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

    public BaseExperimentBuilder(
        int minBasesCount,
        int maxBasesCount,
        int problemsCount,
        int problemRunsCount,
        int minIntelligenceObjectsCount,
        int maxIntelligenceObjectsCount,
        int intelligenceObjectsCountStep,
        bool isEquivalent = false)
    {
        _minBasesCount = minBasesCount;
        _maxBasesCount = maxBasesCount;
        _problemsCount = problemsCount;
        _problemRunsCount = problemRunsCount;
        _minIntelligenceObjectsCount = minIntelligenceObjectsCount;
        _maxIntelligenceObjectsCount = maxIntelligenceObjectsCount;
        _intelligenceObjectsCountStep = intelligenceObjectsCountStep;
        _isEquivalent = isEquivalent;
    }

    public List<ExperimentResult> Run()
    {
        var totalProblemsCount = _problemsCount * (_maxBasesCount - _minBasesCount + 1);

        var problems = new Problem[totalProblemsCount];

        for (var j = 0; j <= _maxBasesCount - _minBasesCount; j++)
        {
            for (var i = 0; i < _problemsCount; i++)
            {
                var problem = new GenerateProblemTool(
                    _minBasesCount + j,
                    (_minBasesCount + j) * 10,
                    2,
                    true,
                    _isEquivalent).Generate();

                problems[i + _problemsCount * j] = problem;
            }
        }

        var results = new List<ExperimentResult>();

        for (var p = 0; p < totalProblemsCount; p++)
        {
            var problemExperimentResult = new ExperimentResult();

            foreach (var solverOption in _solverOptions)
            {
                for (var i = 0; i < _problemRunsCount; i++)
                {
                    var problem = (Problem)problems[p].Clone();

                    ISolver solver;

                    switch (solverOption)
                    {
                        case Options.SolverOptions.Greedy:
                            solver = new GreedySolver(problem);
                            break;
                        case Options.SolverOptions.AbcRAn:
                            solver = new AbcSolver(problem, InitialSolutionOptions.Random, LocalOptimizationOptions.AddNearest);
                            break;
                        case Options.SolverOptions.AbcPAn:
                            solver = new AbcSolver(problem, InitialSolutionOptions.Probable, LocalOptimizationOptions.AddNearest);
                            break;
                        case Options.SolverOptions.AbcPnAn:
                            solver = new AbcSolver(problem, InitialSolutionOptions.ProbableNeighborhood, LocalOptimizationOptions.AddNearest);
                            break;
                        case Options.SolverOptions.AbcRRp:
                            solver = new AbcSolver(problem, InitialSolutionOptions.Random, LocalOptimizationOptions.RebuildProbable);
                            break;
                        case Options.SolverOptions.AbcPRp:
                            solver = new AbcSolver(problem, InitialSolutionOptions.Probable, LocalOptimizationOptions.RebuildProbable);
                            break;
                        case Options.SolverOptions.AbcPnRp:
                            solver = new AbcSolver(problem, InitialSolutionOptions.ProbableNeighborhood, LocalOptimizationOptions.RebuildProbable);
                            break;
                        case Options.SolverOptions.AbcRRpn:
                            solver = new AbcSolver(problem, InitialSolutionOptions.Random, LocalOptimizationOptions.RebuildProbableNeighborhood);
                            break;
                        case Options.SolverOptions.AbcPRpn:
                            solver = new AbcSolver(problem, InitialSolutionOptions.Probable, LocalOptimizationOptions.RebuildProbableNeighborhood);
                            break;
                        case Options.SolverOptions.AbcPnRpn:
                            solver = new AbcSolver(problem, InitialSolutionOptions.ProbableNeighborhood, LocalOptimizationOptions.RebuildProbableNeighborhood);
                            break;
                        case Options.SolverOptions.AbcRRpaan:
                            solver = new AbcSolver(problem, InitialSolutionOptions.Random, LocalOptimizationOptions.RebuildProbableAndAddNearest);
                            break;
                        case Options.SolverOptions.AbcPRpaan:
                            solver = new AbcSolver(problem, InitialSolutionOptions.Probable, LocalOptimizationOptions.RebuildProbableAndAddNearest);
                            break;
                        case Options.SolverOptions.AbcPnRpaan:
                            solver = new AbcSolver(problem, InitialSolutionOptions.ProbableNeighborhood, LocalOptimizationOptions.RebuildProbableAndAddNearest);
                            break;
                        case Options.SolverOptions.AbcRRpnaan:
                            solver = new AbcSolver(problem, InitialSolutionOptions.Random, LocalOptimizationOptions.RebuildProbableNeighborhoodAndAddNearest);
                            break;
                        case Options.SolverOptions.AbcPRpnaan:
                            solver = new AbcSolver(problem, InitialSolutionOptions.Probable, LocalOptimizationOptions.RebuildProbableNeighborhoodAndAddNearest);
                            break;
                        case Options.SolverOptions.AbcPnRpnaan:
                            solver = new AbcSolver(problem, InitialSolutionOptions.ProbableNeighborhood, LocalOptimizationOptions.RebuildProbableNeighborhoodAndAddNearest);
                            break;
                        case Options.SolverOptions.TabuRp:
                            solver = new TabuSolver(problem, LocalOptimizationOptions.RebuildProbable);
                            break;
                        case Options.SolverOptions.TabuRpn:
                            solver = new TabuSolver(problem, LocalOptimizationOptions.RebuildProbableNeighborhood);
                            break;
                        case Options.SolverOptions.TabuRpaan:
                            solver = new TabuSolver(problem, LocalOptimizationOptions.RebuildProbableAndAddNearest);
                            break;
                        case Options.SolverOptions.TabuRpnaan:
                            solver = new TabuSolver(problem, LocalOptimizationOptions.RebuildProbableNeighborhoodAndAddNearest);
                            break;
                        default:
                            throw new ArgumentException("Invalid SolverOptions type");
                    }

                    var stopwatch = new Stopwatch();

                    stopwatch.Start();

                    var solution = solver.Solve();

                    stopwatch.Stop();

                    solution.SetExecutionCharacteristics(solverOption, stopwatch.ElapsedTicks);

                    problemExperimentResult.Solutions.Add(solution);
                }
            }

            results.Add(problemExperimentResult);

            Console.WriteLine($"Problem {p+1} solved");
        }

        return results;
    }
}