using Microsoft.AspNetCore.Mvc;
using PathPlanning.Experiments;
using WebApplication.Models.Experiments;

namespace WebApplication.Controllers
{
    public class ExperimentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Experiment1(BaseExperimentModel experiment)
        {
            var result = new BaseExperimentBuilder(
                3,
                3,
                experiment.ProblemsCount,
                experiment.ProblemRunsCount,
                experiment.MinIntelligenceObjectsCount,
                experiment.MaxIntelligenceObjectsCount,
                experiment.IntelligenceObjectsCountStep,
                true).Run();
            
            var metricsBuilder = new BaseExperimentMetricsTool(result);
            metricsBuilder.Calculate();

            return File(metricsBuilder.OutputToMemoryStream(), "text/csv", "experiment1.csv");
        }

        [HttpPost]
        public IActionResult Experiment2(BaseExperimentModel experiment)
        {
            var result = new BaseExperimentBuilder(
                3,
                3,
                experiment.ProblemsCount,
                experiment.ProblemRunsCount,
                experiment.MinIntelligenceObjectsCount,
                experiment.MaxIntelligenceObjectsCount,
                experiment.IntelligenceObjectsCountStep).Run();

            var metricsBuilder = new BaseExperimentMetricsTool(result);
            metricsBuilder.Calculate();

            return File(metricsBuilder.OutputToMemoryStream(), "text/csv", "experiment2.csv");
        }

        [HttpPost]
        public IActionResult Experiment3(ExperimentModel experiment)
        {
            var result = new BaseExperimentBuilder(
                experiment.MinBasesCount,
                experiment.MaxBasesCount,
                experiment.ProblemsCount,
                experiment.ProblemRunsCount,
                experiment.MinIntelligenceObjectsCount,
                experiment.MaxIntelligenceObjectsCount,
                experiment.IntelligenceObjectsCountStep).Run();

            var metricsBuilder = new BaseExperimentMetricsTool(result);
            metricsBuilder.Calculate();

            return File(metricsBuilder.OutputToMemoryStream(), "text/csv", "experiment3.csv");
        }

        [HttpPost]
        public IActionResult Experiment4(AbcExperimentModel experiment)
        {
            var result = new AbcExperimentBuilder().Run();

            var metricsBuilder = new AbcExperimentMetricsTool(result);
            metricsBuilder.Calculate();

            return File(metricsBuilder.OutputToMemoryStream(), "text/csv", "experiment4.csv");
        }

        [HttpPost]
        public IActionResult Experiment5(TabuExperimentModel experiment)
        {
            var result = new TabuExperimentBuilder().Run();

            var metricsBuilder = new TabuExperimentMetricsTool(result);
            metricsBuilder.Calculate();

            return File(metricsBuilder.OutputToMemoryStream(), "text/csv", "experiment5.csv");
        }
    }
}
