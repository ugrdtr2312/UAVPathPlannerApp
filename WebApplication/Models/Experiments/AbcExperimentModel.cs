using PathPlanning.Experiments.Options;

namespace WebApplication.Models.Experiments
{
    public class AbcExperimentModel
    {
        public SolverOptions AlgorithmOption { get; set; }

        public double MinIterationCountCoefficient { get; set; }

        public double MaxIterationCountCoefficient { get; set; }

        public double IterationCountCoefficientStep { get; set; }

        public double MinScoutsCountCoefficient { get; set; }

        public double MaxScoutsCountCoefficient { get; set; }

        public double ScoutsCountCoefficientStep { get; set; }

        public double MinForagersCountCoefficient { get; set; }

        public double MaxForagersCountCoefficient { get; set; }

        public double ForagersCountCoefficientStep { get; set; }
    }
}
