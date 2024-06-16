namespace WebApplication.Models.Experiments
{
    public class BaseExperimentModel
    {
        public int MinIntelligenceObjectsCount { get; set; }

        public int MaxIntelligenceObjectsCount { get; set; }

        public int IntelligenceObjectsCountStep { get; set; }

        public int ProblemsCount { get; set; }

        public int ProblemRunsCount { get; set; }
    }
}
