using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.VisualBasic;
using PathPlanning.Entities;
using PathPlanning.Solvers;
using PathPlanning.Solvers.Interfaces;
using PathPlanning.Solvers.Options;
using System.Reflection;
using System.Text;
using System.Text.Json;
using WebApplication.Models;
using WebApplication.Models.Solver;

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

        public IActionResult Solve(ProblemData data)
        {
            Problem problem = null;

            if (data.File != null && data.File.Length != 0)
            {
                problem = FileInputTool.ReadProblem(data.File);
            }
            else
            {
               // problem = new Problem();
            }

            problem!.Init();

            ISolver solver = data.AlgorithmType switch
            {
                "Greedy" => new GreedySolver(problem),
                "Abc" => new AbcSolver(
                    problem, 
                    InitialSolutionOptions.ProbableNeighborhood,
                    LocalOptimizationOptions.RebuildProbableNeighborhoodAndAddNearest),
                "Tabu" => new TabuSolver(
                    problem, 
                    LocalOptimizationOptions.RebuildProbableAndAddNearest),
                _ => throw new Exception("Некоретно обрано алгоритм")
            };

            var solution = solver.Solve();

            _solutionService.Solution = new SolutionModel
            {
                Problem = problem,
                Solution = solution
            };

            // Prepare base and object data
            var connections = new List<Connection>();

            var baseMarkers = problem.Bases.Select(x => new MapMarker { Longitude = x.X, Latitude = x.Y, Label = x.Id.ToString() }).ToList();

            var objectMarkers = problem.IntelligenceObjects.Select(x => new MapMarker { Longitude = x.X, Latitude = x.Y, Label = x.Id.ToString() }).ToList();

            // Prepare the connections data


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

            var counter = 0;

            foreach (var subPath in solution.SubPaths)
            {
                if (subPath.IntelligenceObjects.Any())
                {
                    connections.Add(new Connection
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
                            connections.Add(new Connection
                            {
                                StartType = "Object",
                                StartIndex = subPath.IntelligenceObjects[i].Id - 1,
                                EndType = "Object",
                                EndIndex = subPath.IntelligenceObjects[i + 1].Id - 1,
                                Color = colors[counter]
                            });
                        }
                    }

                    connections.Add(new Connection
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
                    connections.Add(new Connection
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
    }
}
