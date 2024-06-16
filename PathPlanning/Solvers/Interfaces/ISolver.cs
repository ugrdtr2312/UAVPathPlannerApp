using PathPlanning.Entities;

namespace PathPlanning.Solvers.Interfaces;

public interface ISolver
{
    Solution Solve();
}