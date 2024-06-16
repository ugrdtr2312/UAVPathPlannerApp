using PathPlanning.Entities;
using PathPlanning.Solvers.Interfaces;

namespace PathPlanning.Solvers;

public class GreedySolver : ISolver
{
    private readonly Problem _problem;

    public GreedySolver(Problem problem)
    {
        _problem = problem;
    }

    public Solution Solve()
    {
        var solution = new Solution(_problem.ServiceTimeInHours);

        for (var i = 0; i < _problem.SubPathCount; i++)
        {
            solution.SubPaths.Add(FindSubPath(_problem.Bases[i], _problem.Bases[i + 1]));
        }

        return solution;
    }

    private SubPath FindSubPath(Base startBase, Base endBase)
    {
        var subPath = new SubPath(startBase, endBase);

        var availableTimeInAir = _problem.MaxTimeInAirInHours;
        var startPointIndex = startBase.Id + _problem.IntelligenceObjectsCount;
        var endPointIndex = endBase.Id + _problem.IntelligenceObjectsCount;

        do
        {
            var (startPoint, timeToTravel) = FindBestPossibleIntelligenceObject(startPointIndex);

            if (availableTimeInAir - timeToTravel < _problem.MovementCharacteristicsMatrix[endPointIndex - 1, startPoint!.Id - 1].Time)
            {
                if (subPath.IntelligenceObjects.Count == 0)
                {
                    subPath.SetTimeToEndBase(_problem.MovementCharacteristicsMatrix[endPointIndex - 1, startPointIndex - 1].Time);
                    break;
                }
                
                (startPoint, timeToTravel) = TryFindBestPossibleIntelligenceObject(startPointIndex, endPointIndex, startPoint.Id, availableTimeInAir);

                if (startPoint == null)
                {
                    subPath.SetTimeToEndBase(_problem.MovementCharacteristicsMatrix[endPointIndex - 1, subPath.IntelligenceObjects[^1].Id - 1].Time);
                    break;
                }
            }

            availableTimeInAir -= timeToTravel;

            startPoint.Visit(timeToTravel);

            subPath.IntelligenceObjects.Add(startPoint);

            startPointIndex = startPoint.Id;

        } while (true);

        return subPath;
    }

    private (IntelligenceObject? IntelligenceObject, double TimeToTravel) TryFindBestPossibleIntelligenceObject(
        int startPointIndex, int endPointIndex, int skipIntelligenceObjectId, double availableTimeInAir)
    {
        var intelligenceObjectIdsToSkip = new List<int> { skipIntelligenceObjectId };

        do
        {
            var (startPoint, timeToTravel) = FindBestPossibleIntelligenceObject(startPointIndex, intelligenceObjectIdsToSkip);

            if (startPoint == null) 
                return (null, 0);

            if (availableTimeInAir - timeToTravel > _problem.MovementCharacteristicsMatrix[endPointIndex - 1, startPoint.Id - 1].Time)
                return (startPoint, timeToTravel);

            intelligenceObjectIdsToSkip.Add(startPoint.Id);
        } while (true);
    }

    private (IntelligenceObject? intelligenceObjectToVisit, double minTimeToTravel) FindBestPossibleIntelligenceObject(
        int startPointIndex, List<int>? intelligenceObjectIndexToSkip = null)
    {
        intelligenceObjectIndexToSkip ??= new List<int>();

        IntelligenceObject? intelligenceObjectToVisit = null;
        var maxAverageWeight = double.MinValue;
        var timeToTravel = double.MaxValue;

        for (var i = 0; i < _problem.IntelligenceObjects.Count; i++)
        {
            if (!_problem.IntelligenceObjects[i].IsVisited
                && _problem.MovementCharacteristicsMatrix[i, startPointIndex - 1].AverageWeight > maxAverageWeight
                && !intelligenceObjectIndexToSkip.Contains(i + 1))
            {
                intelligenceObjectToVisit = _problem.IntelligenceObjects[i];
                maxAverageWeight = _problem.MovementCharacteristicsMatrix[i, startPointIndex - 1].AverageWeight;
                timeToTravel = _problem.MovementCharacteristicsMatrix[i, startPointIndex - 1].Time;
            }
        }

        return intelligenceObjectToVisit != null
                ? (intelligenceObjectToVisit, timeToTravel)
                : (null, 0);
    }
}
