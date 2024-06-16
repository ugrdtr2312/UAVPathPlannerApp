namespace PathPlanning.Solvers.Options;

public enum LocalOptimizationOptions
{
    AddNearest = 1,
    RebuildProbable,
    RebuildProbableNeighborhood,
    RebuildProbableAndAddNearest,
    RebuildProbableNeighborhoodAndAddNearest
}