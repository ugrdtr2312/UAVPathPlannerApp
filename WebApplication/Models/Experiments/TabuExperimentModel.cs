using PathPlanning.Experiments.Options;

namespace WebApplication.Models.Experiments
{
    public class TabuExperimentModel
    {
        public SolverOptions AlgorithmOption { get; set; }

        public double MinIterationCountCoefficient { get; set; }

        public double MaxIterationCountCoefficient { get; set; }

        public double IterationCountCoefficientStep { get; set; }

        public double MinTabuListSizeCoefficient { get; set; }

        public double MaxTabuListSizeCountCoefficient { get; set; }

        public double TabuListSizeCountCoefficientStep { get; set; }
    }
}
