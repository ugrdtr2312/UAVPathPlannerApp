using Microsoft.AspNetCore.Mvc;
using PathPlanning.Entities;
using PathPlanning.Solvers;
using PathPlanning.Solvers.Interfaces;
using PathPlanning.Solvers.Options;
using System.Text;
using System.Text.Json;
using WebApplication.Models.Solver.Solve;
using WebApplication.Models.Solver.Visualize;
using WebApplication.Services;
using WebApplication.Tools;

namespace WebApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly SolutionService _solutionService;

        public HomeController(SolutionService solutionService)
        {
            _solutionService = solutionService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Solve(ProblemModel data)
        {
            var problem = data.File != null && data.File.Length != 0
                ? FileInputTool.ReadProblem(data.File)
                : MapToProblem(data);

            problem.Init();

            ISolver solver = data.AlgorithmType switch
            {
                "Greedy" => new GreedySolver(problem),
                "Abc" => new AbcSolver(
                    problem, 
                    InitialSolutionOptions.ProbableNeighborhood,
                    LocalOptimizationOptions.RebuildProbableNeighborhoodAndAddNearest),
                "Tabu" => new TabuSolver(
                    problem, 
                    LocalOptimizationOptions.RebuildProbableNeighborhoodAndAddNearest, 4, 2),
                _ => throw new Exception("Некоретно обрано алгоритм")
            };

            var solution = solver.Solve();

            _solutionService.Solution = new SolutionModel
            {
                Problem = problem,
                Solution = solution
            };

            var connections = GetConnections(solution);
            var baseMarkers = problem.Bases.Select(x => new MarkerModel { Longitude = x.X, Latitude = x.Y, Label = x.Id.ToString() }).ToList();
            var objectMarkers = problem.IntelligenceObjects.Select(x => new MarkerModel { Longitude = x.X, Latitude = x.Y, Label = x.Id.ToString() }).ToList();

            ViewData["BaseMarkers"] = JsonSerializer.Serialize(baseMarkers);
            ViewData["ObjectMarkers"] = JsonSerializer.Serialize(objectMarkers);
            ViewData["Connections"] = JsonSerializer.Serialize(connections);

            return View(_solutionService.Solution);
        }

        [HttpGet]
        public FileContentResult SaveToFile()
        {
            var subPathsAsString = _solutionService.Solution.Solution.SubPathsToString();

            return File(Encoding.UTF8.GetBytes(subPathsAsString), "text/plain", "result.txt");
        }

        private Problem MapToProblem(ProblemModel data)
        {
            var bases = data.Bases.Select(x => new Base(x.Id, x.X, x.Y)).ToList();
            var intelligenceObjects = data.Points.Select(x => new IntelligenceObject(x.Id, x.X, x.Y, x.Weight)).ToList();

            return new Problem(
                bases,
                intelligenceObjects,
                data.MaxFlightTime / 60,
                data.Speed,
                data.ChargeTime / 60,
                true);
        }

        private List<ConnectionModel> GetConnections(Solution solution)
        {
            var colors = new[]
            {
                "#FF0000", // Red
                "#800080", // Purple
                "#00FF00", // Green
                "#0000FF", // Blue
                "#FFFF00", // Yellow
                "#FF00FF", // Magenta
                "#00FFFF", // Cyan
                "#FFA500", // Orange
                "#FFC0CB", // Pink
                "#00FF7F"  // Spring Green
            };
            var connections = new List<ConnectionModel>();

            var counter = 0;

            foreach (var subPath in solution.SubPaths)
            {
                if (subPath.IntelligenceObjects.Any())
                {
                    connections.Add(new ConnectionModel
                    {
                        StartType = "Base",
                        StartIndex = subPath.StartBase.Id - 1,
                        EndType = "Object",
                        EndIndex = subPath.IntelligenceObjects[0].Id - 1,
                        Color = colors[counter]
                    });

                    if (subPath.IntelligenceObjects.Count > 1)
                    {
                        for (var i = 0; i < subPath.IntelligenceObjects.Count - 1; i++)
                        {
                            connections.Add(new ConnectionModel
                            {
                                StartType = "Object",
                                StartIndex = subPath.IntelligenceObjects[i].Id - 1,
                                EndType = "Object",
                                EndIndex = subPath.IntelligenceObjects[i + 1].Id - 1,
                                Color = colors[counter]
                            });
                        }
                    }

                    connections.Add(new ConnectionModel
                    {
                        StartType = "Object",
                        StartIndex = subPath.IntelligenceObjects[^1].Id - 1,
                        EndType = "Base",
                        EndIndex = subPath.EndBase.Id - 1,
                        Color = colors[counter]
                    });
                }
                else
                {
                    connections.Add(new ConnectionModel
                    {
                        StartType = "Base",
                        StartIndex = subPath.StartBase.Id - 1,
                        EndType = "Base",
                        EndIndex = subPath.EndBase.Id - 1,
                        Color = colors[counter]
                    });
                }

                counter++;
            }

            return connections;
        }
    }
}
