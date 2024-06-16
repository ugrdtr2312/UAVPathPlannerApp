using PathPlanning.Entities;
using PathPlanning.Solvers.Collections;
using PathPlanning.Solvers.Interfaces;
using PathPlanning.Solvers.Options;

namespace PathPlanning.Solvers;

public class TabuSolver : ISolver
{
    private static readonly Random Random = new();

    private readonly Problem _problem;
    private readonly LocalOptimizationOptions _localOptimizationOption;

    private int _currentStartBaseIndex = 1;
    private readonly int _maxIterationsWithoutImprovement;
    private readonly int _tabuListSize;

    private SubPath _subPathToOptimize;
    private LimitedSizeQueue<string> _tabuList;

    public TabuSolver(
            Problem problem, 
            LocalOptimizationOptions localOptimizationOption,
            double maxIterationsCoefficient = 1d, 
            double tabuListCoefficient = 1d)
    {
        _problem = problem;
        _localOptimizationOption = localOptimizationOption;
        _maxIterationsWithoutImprovement = (int)(problem.IntelligenceObjectsCount * problem.BasesCount * 10d * maxIterationsCoefficient);
        _tabuListSize = (int)(_maxIterationsWithoutImprovement / 5d * tabuListCoefficient);
    }

    public Solution Solve()
    {
        var solution = new Solution(_problem.ServiceTimeInHours);

        for (var i = 1; i <= _problem.SubPathCount; i++)
        {
            _tabuList = new LimitedSizeQueue<string>(_tabuListSize);

            _currentStartBaseIndex = i;

            _subPathToOptimize = GenerateInitialSubPath();
            
            _tabuList.Enqueue(string.Join("/", _subPathToOptimize.IntelligenceObjects.Select(x => x.Id)));

            var iterationsWithoutImprovement = 0;

            while (iterationsWithoutImprovement != _maxIterationsWithoutImprovement)
            {
                var isOptimized = _localOptimizationOption switch
                {
                    LocalOptimizationOptions.RebuildProbable => RebuildProbableOptimization(),
                    LocalOptimizationOptions.RebuildProbableNeighborhood => RebuildProbableNeighborhoodOptimization(),
                    LocalOptimizationOptions.RebuildProbableAndAddNearest => RebuildProbableAndAddNearestOptimization(),
                    LocalOptimizationOptions.RebuildProbableNeighborhoodAndAddNearest => RebuildProbableNeighborhoodAndAddNearestOptimization(),
                    _ => throw new ArgumentException("Invalid LocalOptimizationOptions type")
                };

                if (!isOptimized)
                    iterationsWithoutImprovement++;
                else
                    iterationsWithoutImprovement = 0;
            }

            solution.SubPaths.Add(_subPathToOptimize);

            foreach (var intelligenceObject in _subPathToOptimize.IntelligenceObjects)
            {
                _problem.IntelligenceObjects[intelligenceObject.Id - 1].Visit(intelligenceObject.TimeForMovement);
            }
        } 
        
        return solution;
    }

    private bool RebuildProbableOptimization()
    {
        var isOptimized = false;

        var currentBest = _subPathToOptimize;
        var tempBest = _subPathToOptimize;

        while (tempBest != null)
        {
            tempBest = TryToRebuildProbableOptimize(currentBest);

            if (tempBest != null)
            {
                currentBest = tempBest;
                isOptimized = true;
            }
        }

        if (isOptimized)
            _subPathToOptimize = currentBest;

        return isOptimized;
    }

    private SubPath? TryToRebuildProbableOptimize(SubPath subPath)
    {
        var currentIntelligenceObjectIndexes = subPath.IntelligenceObjects.Select(x => x.Id).ToArray();

        var optimized = new List<SubPath>();

        for (var j = 0; j < _problem.IntelligenceObjectsOnSubPath; j++)
        {
            var pointToOptimize = Random.Next(0, subPath.IntelligenceObjects.Count);

            var toOptimize = currentIntelligenceObjectIndexes.Take(pointToOptimize + 1).ToList();

            var newSubPathPoints = FindProbableSubPath(toOptimize, CalculateAvailableTimeInAir(toOptimize));

            var tabuRecord = string.Join("/", newSubPathPoints);

            if (_tabuList.Contains(tabuRecord))
                continue;

            _tabuList.Enqueue(tabuRecord);

            optimized.Add(CreateSubPath(newSubPathPoints));
        }

        var bestSubPath = optimized.MaxBy(x => x.GetTotalWeight());

        return bestSubPath?.GetTotalWeight() > subPath.GetTotalWeight() ? bestSubPath : null;
    }

    private bool RebuildProbableNeighborhoodOptimization()
    {
        var isOptimized = false;

        var currentBest = _subPathToOptimize;
        var tempBest = _subPathToOptimize;

        while (tempBest != null)
        {
            tempBest = TryToRebuildProbableNeighborhoodOptimize(currentBest);

            if (tempBest != null)
            {
                currentBest = tempBest;
                isOptimized = true;
            }
        }

        if (isOptimized)
            _subPathToOptimize = currentBest;

        return isOptimized;
    }

    private SubPath? TryToRebuildProbableNeighborhoodOptimize(SubPath subPath)
    {
        var currentIntelligenceObjectIndexes = subPath.IntelligenceObjects.Select(x => x.Id).ToArray();

        var optimized = new List<SubPath>();

        for (var j = 0; j < _problem.IntelligenceObjectsOnSubPath; j++)
        {
            var pointToOptimize = Random.Next(0, subPath.IntelligenceObjects.Count);

            var toOptimize = currentIntelligenceObjectIndexes.Take(pointToOptimize + 1).ToList();

            var newSubPathPoints = FindProbableNeighborhoodSubPath(toOptimize, CalculateAvailableTimeInAir(toOptimize));

            var tabuRecord = string.Join("/", newSubPathPoints);

            if (_tabuList.Contains(tabuRecord))
                continue;

            _tabuList.Enqueue(tabuRecord);

            optimized.Add(CreateSubPath(newSubPathPoints));
        }

        var bestSubPath = optimized.MaxBy(x => x.GetTotalWeight());

        return bestSubPath?.GetTotalWeight() > subPath.GetTotalWeight() ? bestSubPath : null;
    }

    private bool RebuildProbableAndAddNearestOptimization()
    {
        var isOptimized = false;

        var currentBest = _subPathToOptimize;
        var tempBest = _subPathToOptimize;

        while (tempBest != null)
        {
            tempBest = TryToRebuildProbableAndAddNearestOptimize(currentBest);

            if (tempBest != null)
            {
                currentBest = tempBest;
                isOptimized = true;
            }
        }

        if (isOptimized)
            _subPathToOptimize = currentBest;

        return isOptimized;
    }

    private SubPath? TryToRebuildProbableAndAddNearestOptimize(SubPath subPath)
    {
        var currentIntelligenceObjectIndexes = subPath.IntelligenceObjects.Select(x => x.Id).ToArray();

        var optimized = new List<SubPath>();

        for (var j = 0; j < _problem.IntelligenceObjectsOnSubPath; j++)
        {
            var pointToOptimize = Random.Next(0, subPath.IntelligenceObjects.Count);

            var toOptimize = currentIntelligenceObjectIndexes.Take(pointToOptimize + 1).ToList();

            var newSubPathPoints = FindProbableSubPath(toOptimize, CalculateAvailableTimeInAir(toOptimize));

            var tabuRecord = string.Join("/", newSubPathPoints);

            if (_tabuList.Contains(tabuRecord))
                continue;

            _tabuList.Enqueue(tabuRecord);

            optimized.Add(CreateSubPath(newSubPathPoints));
        }

        var bestSubPath = optimized.MaxBy(x => x.GetTotalWeight() / x.GetTimeInAir());

        var currentBest = bestSubPath;
        var tempBest = bestSubPath;

        while (tempBest != null)
        {
            tempBest = TryToAddNearestOptimize(currentBest!);

            if (tempBest != null)
                currentBest = tempBest;
        }

        return currentBest?.GetTotalWeight() > subPath.GetTotalWeight() ? currentBest : null;
    }

    private bool RebuildProbableNeighborhoodAndAddNearestOptimization()
    {
        var isOptimized = false;

        var currentBest = _subPathToOptimize;
        var tempBest = _subPathToOptimize;

        while (tempBest != null)
        {
            tempBest = TryToRebuildProbableNeighborhoodAndAddNearestOptimize(currentBest);

            if (tempBest != null)
            {
                currentBest = tempBest;
                isOptimized = true;
            }
        }

        if (isOptimized)
            _subPathToOptimize = currentBest;

        return isOptimized;
    }

    private SubPath? TryToRebuildProbableNeighborhoodAndAddNearestOptimize(SubPath subPath)
    {
        var currentIntelligenceObjectIndexes = subPath.IntelligenceObjects.Select(x => x.Id).ToArray();

        var optimized = new List<SubPath>();

        for (var j = 0; j < _problem.IntelligenceObjectsOnSubPath; j++)
        {
            var pointToOptimize = Random.Next(0, subPath.IntelligenceObjects.Count);

            var toOptimize = currentIntelligenceObjectIndexes.Take(pointToOptimize + 1).ToList();

            var newSubPathPoints = FindProbableNeighborhoodSubPath(toOptimize, CalculateAvailableTimeInAir(toOptimize));

            var tabuRecord = string.Join("/", newSubPathPoints);

            if (_tabuList.Contains(tabuRecord))
                continue;

            _tabuList.Enqueue(tabuRecord);

            optimized.Add(CreateSubPath(newSubPathPoints));
        }

        var bestSubPath = optimized.MaxBy(x => x.GetTotalWeight() / x.GetTimeInAir());

        var currentBest = bestSubPath;
        var tempBest = bestSubPath;

        while (tempBest != null)
        {
            tempBest = TryToAddNearestOptimize(currentBest!);

            if (tempBest != null)
                currentBest = tempBest;
        }

        return currentBest?.GetTotalWeight() > subPath.GetTotalWeight() ? currentBest : null;
    }

    private List<int> FindProbableSubPath(List<int> intelligenceObjectsToVisit, double availableTimeInAir)
    {
        var currentPointIndex = intelligenceObjectsToVisit.Count != 0
            ? intelligenceObjectsToVisit[^1]
            : _currentStartBaseIndex + _problem.IntelligenceObjectsCount - 1;

        do
        {
            var availableForMovementIndexes = new List<int>();

            var totalWeight = 0d;

            for (var i = 0; i < _problem.IntelligenceObjects.Count; i++)
            {
                if (!_problem.IntelligenceObjects[i].IsVisited
                    && !intelligenceObjectsToVisit.Contains(i + 1)
                    && _problem.MovementCharacteristicsMatrix[i, currentPointIndex - 1].Time
                        + _problem.MovementCharacteristicsMatrix[_currentStartBaseIndex + _problem.IntelligenceObjectsCount, i].Time <= availableTimeInAir)
                {
                    availableForMovementIndexes.Add(_problem.IntelligenceObjects[i].Id);
                    totalWeight += _problem.MovementCharacteristicsMatrix[i, currentPointIndex - 1].AverageWeight;
                }
            }

            if (availableForMovementIndexes.Count == 0)
                break;

            var rand = Random.NextDouble();

            var sum = 0d;

            foreach (var intelligenceObjectIndex in availableForMovementIndexes)
            {
                sum += _problem.MovementCharacteristicsMatrix[intelligenceObjectIndex - 1, currentPointIndex - 1].AverageWeight / totalWeight;

                if (sum >= rand)
                {
                    availableTimeInAir -= _problem.MovementCharacteristicsMatrix[intelligenceObjectIndex - 1, currentPointIndex - 1].Time;
                    currentPointIndex = intelligenceObjectIndex;
                    intelligenceObjectsToVisit.Add(currentPointIndex);
                    break;
                }
            }
        } while (true);

        return intelligenceObjectsToVisit;
    }
    private List<int> FindProbableNeighborhoodSubPath(List<int> intelligenceObjectsToVisit, double availableTimeInAir)
    {
        var currentPointIndex = intelligenceObjectsToVisit.Count != 0
            ? intelligenceObjectsToVisit[^1]
            : _currentStartBaseIndex + _problem.IntelligenceObjectsCount - 1;

        do
        {
            var availableForMovementIndexes = new List<int>();

            var totalWeight = 0d;

            for (var i = 0; i < _problem.IntelligenceObjects.Count; i++)
            {
                if (!_problem.IntelligenceObjects[i].IsVisited
                    && !intelligenceObjectsToVisit.Contains(i + 1)
                    && _problem.MovementCharacteristicsMatrix[i, currentPointIndex - 1].Time <= 0.5 * _problem.MaxTimeInAirInHours
                    && _problem.MovementCharacteristicsMatrix[i, currentPointIndex - 1].Time
                        + _problem.MovementCharacteristicsMatrix[_currentStartBaseIndex + _problem.IntelligenceObjectsCount, i].Time <= availableTimeInAir)
                {
                    availableForMovementIndexes.Add(_problem.IntelligenceObjects[i].Id);
                    totalWeight += _problem.MovementCharacteristicsMatrix[i, currentPointIndex - 1].AverageWeight;
                }
            }

            if (availableForMovementIndexes.Count == 0)
                break;

            var rand = Random.NextDouble();

            var sum = 0d;

            foreach (var intelligenceObjectIndex in availableForMovementIndexes)
            {
                sum += _problem.MovementCharacteristicsMatrix[intelligenceObjectIndex - 1, currentPointIndex - 1].AverageWeight / totalWeight;

                if (sum >= rand)
                {
                    availableTimeInAir -= _problem.MovementCharacteristicsMatrix[intelligenceObjectIndex - 1, currentPointIndex - 1].Time;
                    currentPointIndex = intelligenceObjectIndex;
                    intelligenceObjectsToVisit.Add(currentPointIndex);
                    break;
                }
            }
        } while (true);

        return intelligenceObjectsToVisit;
    }

    private SubPath? TryToAddNearestOptimize(SubPath subPath)
    {
        var currentIntelligenceObjectIndexes = subPath.IntelligenceObjects.Select(x => x.Id).ToArray();

        var optimized = new List<SubPath>();

        for (var j = 0; j < _problem.IntelligenceObjectsOnSubPath; j++)
        {
            var clonedSubPath = (SubPath)subPath.Clone();

            var pointToOptimize = Random.Next(0, clonedSubPath.IntelligenceObjects.Count);

            var pointToOptimizeIndex = clonedSubPath.IntelligenceObjects[pointToOptimize].Id - 1;

            var minTimeInAir = double.MaxValue;
            var pointToAddIndex = int.MinValue;

            for (var i = 0; i < _problem.IntelligenceObjectsCount; i++)
            {
                if (_problem.IntelligenceObjects[i].IsVisited
                    || currentIntelligenceObjectIndexes.Contains(i + 1)
                    || i == pointToOptimizeIndex) continue;

                var timeInAir = _problem.MovementCharacteristicsMatrix[i, pointToOptimizeIndex].Time;

                if (timeInAir < minTimeInAir)
                {
                    minTimeInAir = timeInAir;
                    pointToAddIndex = i;
                }
            }

            var newSubPath = ValidatePointToAdd(clonedSubPath, pointToOptimize, pointToAddIndex);

            if (newSubPath != null)
            {
                optimized.Add(newSubPath);
            }
        }

        var bestSubPath = optimized.MaxBy(x => x.GetTotalWeight());

        return bestSubPath;
    }

    private SubPath? ValidatePointToAdd(SubPath subPath, int intelligenceObjectToOptimize, int intelligenceObjectToAddIndex)
    {
        var timeInAir = subPath.GetTimeInAir();

        var intelligenceObject = subPath.IntelligenceObjects[intelligenceObjectToOptimize];

        var intelligenceObjectToAdd = (IntelligenceObject)_problem.IntelligenceObjects[intelligenceObjectToAddIndex].Clone();

        // One element
        if (subPath.IntelligenceObjects.Count == 1)
        {
            // Add from start
            var timeInAirAddingToStart = timeInAir - intelligenceObject.TimeForMovement;

            var timeInAirToFirstBase = _problem.MovementCharacteristicsMatrix[intelligenceObjectToAddIndex, subPath.StartBase.Id + _problem.IntelligenceObjectsCount - 1].Time;

            var timeInAirToIntelligenceObjectR = _problem.MovementCharacteristicsMatrix[intelligenceObject.Id - 1, intelligenceObjectToAddIndex].Time;

            timeInAirAddingToStart += timeInAirToFirstBase + timeInAirToIntelligenceObjectR;

            // Add to end
            var timeInAirAddingToEnd = timeInAir - subPath.TimeToEndBase;

            var timeInAirToIntelligenceObjectL = _problem.MovementCharacteristicsMatrix[intelligenceObjectToAddIndex, intelligenceObject.Id - 1].Time;

            var timeInAirToEndBase = _problem.MovementCharacteristicsMatrix[subPath.StartBase.Id + _problem.IntelligenceObjectsCount, intelligenceObjectToAddIndex].Time;

            timeInAirAddingToEnd += timeInAirToIntelligenceObjectL + timeInAirToEndBase;

            if (timeInAirAddingToStart > _problem.MaxTimeInAirInHours &&
                timeInAirAddingToEnd > _problem.MaxTimeInAirInHours)
                return null;

            if (timeInAirAddingToStart <= timeInAirAddingToEnd)
            {
                intelligenceObjectToAdd.Visit(timeInAirToFirstBase);
                intelligenceObject.Visit(timeInAirToIntelligenceObjectR);

                subPath.IntelligenceObjects.Insert(0, intelligenceObjectToAdd);

                return subPath;
            }

            intelligenceObjectToAdd.Visit(timeInAirToIntelligenceObjectL);
            subPath.SetTimeToEndBase(timeInAirToEndBase);

            subPath.IntelligenceObjects.Insert(1, intelligenceObjectToAdd);

            return subPath;
        }

        // First intelligence object
        if (intelligenceObjectToOptimize == 0 && subPath.IntelligenceObjects.Count > 1)
        {
            // Add from start
            var timeInAirAddingToStart = timeInAir - intelligenceObject.TimeForMovement;

            var timeInAirToFirstBase = _problem.MovementCharacteristicsMatrix[intelligenceObjectToAddIndex, subPath.StartBase.Id + _problem.IntelligenceObjectsCount - 1].Time;

            var timeInAirToIntelligenceObjectR = _problem.MovementCharacteristicsMatrix[intelligenceObject.Id - 1, intelligenceObjectToAddIndex].Time;

            timeInAirAddingToStart += timeInAirToFirstBase + timeInAirToIntelligenceObjectR;

            // Add right
            var timeInAirAddingRight = timeInAir - subPath.IntelligenceObjects[1].TimeForMovement;

            var indexOfRight = subPath.IntelligenceObjects[1].Id - 1;

            var timeInAirToIntelligenceObjectL = _problem.MovementCharacteristicsMatrix[intelligenceObjectToAddIndex, intelligenceObject.Id - 1].Time;

            var timeInAirToIntelligenceObjectR2 = _problem.MovementCharacteristicsMatrix[indexOfRight, intelligenceObjectToAddIndex].Time;

            timeInAirAddingRight = timeInAirAddingRight + timeInAirToIntelligenceObjectL + timeInAirToIntelligenceObjectR2;

            if (timeInAirAddingToStart > _problem.MaxTimeInAirInHours &&
                timeInAirAddingRight > _problem.MaxTimeInAirInHours)
                return null;

            if (timeInAirAddingToStart <= timeInAirAddingRight)
            {
                intelligenceObjectToAdd.Visit(timeInAirToFirstBase);
                intelligenceObject.Visit(timeInAirToIntelligenceObjectR);

                subPath.IntelligenceObjects.Insert(0, intelligenceObjectToAdd);

                return subPath;
            }

            intelligenceObjectToAdd.Visit(timeInAirToIntelligenceObjectL);

            subPath.IntelligenceObjects[1].Visit(timeInAirToIntelligenceObjectR2);

            subPath.IntelligenceObjects.Insert(1, intelligenceObjectToAdd);

            return subPath;
        }

        // Last intelligence object
        if (intelligenceObjectToOptimize == subPath.IntelligenceObjects.Count - 1 && subPath.IntelligenceObjects.Count > 1)
        {
            // Add left
            var timeInAirAddingLeft = timeInAir - intelligenceObject.TimeForMovement;

            var indexOfLeft = subPath.IntelligenceObjects[intelligenceObjectToOptimize - 1].Id - 1;

            var timeInAirToIntelligenceObjectL = _problem.MovementCharacteristicsMatrix[intelligenceObjectToAddIndex, indexOfLeft].Time;

            var timeInAirToIntelligenceObjectR = _problem.MovementCharacteristicsMatrix[intelligenceObject.Id - 1, intelligenceObjectToAddIndex].Time;

            timeInAirAddingLeft = timeInAirAddingLeft + timeInAirToIntelligenceObjectL + timeInAirToIntelligenceObjectR;

            // Add to end
            var timeInAirAddingToEnd = timeInAir - subPath.TimeToEndBase;

            var timeInAirToIntelligenceObjectL2 = _problem.MovementCharacteristicsMatrix[intelligenceObjectToAddIndex, intelligenceObject.Id - 1].Time;

            var timeInAirToEndBase = _problem.MovementCharacteristicsMatrix[subPath.StartBase.Id + _problem.IntelligenceObjectsCount, intelligenceObjectToAddIndex].Time;

            timeInAirAddingToEnd += timeInAirToIntelligenceObjectL2 + timeInAirToEndBase;

            if (timeInAirAddingLeft > _problem.MaxTimeInAirInHours &&
                timeInAirAddingToEnd > _problem.MaxTimeInAirInHours)
                return null;

            if (timeInAirAddingLeft <= timeInAirAddingToEnd)
            {
                intelligenceObjectToAdd.Visit(timeInAirToIntelligenceObjectL);

                subPath.IntelligenceObjects.Insert(intelligenceObjectToOptimize, intelligenceObjectToAdd);

                subPath.IntelligenceObjects[intelligenceObjectToOptimize + 1].Visit(timeInAirToIntelligenceObjectR);

                return subPath;
            }

            intelligenceObjectToAdd.Visit(timeInAirToIntelligenceObjectL2);
            subPath.SetTimeToEndBase(timeInAirToEndBase);

            subPath.IntelligenceObjects.Add(intelligenceObjectToAdd);

            return subPath;
        }

        // In the middle
        if (intelligenceObjectToOptimize > 0 && intelligenceObjectToOptimize < subPath.IntelligenceObjects.Count - 1)
        {
            // Add left
            var timeInAirAddingLeft = timeInAir - intelligenceObject.TimeForMovement;

            var indexOfLeft = subPath.IntelligenceObjects[intelligenceObjectToOptimize - 1].Id - 1;

            var timeInAirToIntelligenceObjectL = _problem.MovementCharacteristicsMatrix[intelligenceObjectToAddIndex, indexOfLeft].Time;

            var timeInAirToIntelligenceObjectR = _problem.MovementCharacteristicsMatrix[intelligenceObject.Id - 1, intelligenceObjectToAddIndex].Time;

            timeInAirAddingLeft = timeInAirAddingLeft + timeInAirToIntelligenceObjectL + timeInAirToIntelligenceObjectR;

            // Add right
            var timeInAirAddingRight = timeInAir - subPath.IntelligenceObjects[intelligenceObjectToOptimize + 1].TimeForMovement;

            var indexOfRight = subPath.IntelligenceObjects[intelligenceObjectToOptimize + 1].Id - 1;

            var timeInAirToIntelligenceObjectL2 = _problem.MovementCharacteristicsMatrix[intelligenceObjectToAddIndex, intelligenceObject.Id - 1].Time;

            var timeInAirToIntelligenceObjectR2 = _problem.MovementCharacteristicsMatrix[indexOfRight, intelligenceObjectToAddIndex].Time;

            timeInAirAddingRight = timeInAirAddingRight + timeInAirToIntelligenceObjectL2 + timeInAirToIntelligenceObjectR2;

            if (timeInAirAddingLeft > _problem.MaxTimeInAirInHours &&
                timeInAirAddingRight > _problem.MaxTimeInAirInHours)
                return null;

            if (timeInAirAddingLeft <= timeInAirAddingRight)
            {
                intelligenceObjectToAdd.Visit(timeInAirToIntelligenceObjectL);

                subPath.IntelligenceObjects.Insert(intelligenceObjectToOptimize, intelligenceObjectToAdd);

                subPath.IntelligenceObjects[intelligenceObjectToOptimize + 1].Visit(timeInAirToIntelligenceObjectR);

                return subPath;
            }

            intelligenceObjectToAdd.Visit(timeInAirToIntelligenceObjectL2);

            subPath.IntelligenceObjects.Insert(intelligenceObjectToOptimize + 1, intelligenceObjectToAdd);

            subPath.IntelligenceObjects[intelligenceObjectToOptimize + 2].Visit(timeInAirToIntelligenceObjectR2);

            return subPath;
        }

        return null;
    }

    private double CalculateAvailableTimeInAir(IReadOnlyList<int> intelligenceObjectIndexesToVisit)
    {
        if (intelligenceObjectIndexesToVisit.Count == 0)
            return _problem.MaxTimeInAirInHours;

        var timeInAir = _problem.MovementCharacteristicsMatrix[intelligenceObjectIndexesToVisit[0] - 1, _problem.IntelligenceObjectsCount + _currentStartBaseIndex - 1].Time;

        for (var j = 0; j < intelligenceObjectIndexesToVisit.Count - 1; j++)
        {
            timeInAir += _problem.MovementCharacteristicsMatrix[intelligenceObjectIndexesToVisit[j + 1] - 1, intelligenceObjectIndexesToVisit[j] - 1].Time;
        }

        return _problem.MaxTimeInAirInHours - timeInAir;
    }

    private SubPath CreateSubPath(List<int> intelligenceObjectIndexesToVisit)
    {
        var subPath = new SubPath(_problem.Bases[_currentStartBaseIndex - 1], _problem.Bases[_currentStartBaseIndex]);

        var intelligenceObjects = new List<IntelligenceObject>();

        var firstIntelligenceObject = (IntelligenceObject)_problem.IntelligenceObjects[intelligenceObjectIndexesToVisit[0] - 1].Clone();

        var startBaseIndex = _problem.IntelligenceObjectsCount + _currentStartBaseIndex;

        firstIntelligenceObject.Visit(_problem.MovementCharacteristicsMatrix[intelligenceObjectIndexesToVisit[0] - 1, startBaseIndex - 1].Time);

        intelligenceObjects.Add(firstIntelligenceObject);

        for (var j = 0; j < intelligenceObjectIndexesToVisit.Count - 1; j++)
        {
            var intelligenceObject = (IntelligenceObject)_problem.IntelligenceObjects[intelligenceObjectIndexesToVisit[j + 1] - 1].Clone();
            intelligenceObject.Visit(_problem.MovementCharacteristicsMatrix[intelligenceObjectIndexesToVisit[j] - 1, intelligenceObjectIndexesToVisit[j + 1] - 1].Time);
            intelligenceObjects.Add(intelligenceObject);
        }

        subPath.IntelligenceObjects.AddRange(intelligenceObjects);

        subPath.SetTimeToEndBase(_problem.MovementCharacteristicsMatrix[startBaseIndex, intelligenceObjectIndexesToVisit[^1] - 1].Time);

        return subPath;
    }

    private SubPath GenerateInitialSubPath()
    {
        var subPath = new SubPath(_problem.Bases[_currentStartBaseIndex - 1], _problem.Bases[_currentStartBaseIndex]);

        var availableTimeInAir = _problem.MaxTimeInAirInHours;
        var startPointIndex = _problem.Bases[_currentStartBaseIndex - 1].Id + _problem.IntelligenceObjectsCount;
        var endPointIndex = _problem.Bases[_currentStartBaseIndex].Id + _problem.IntelligenceObjectsCount;

        var visitedIntelligenceObjectIndexes = new List<int>();

        do
        {
            var (startPoint, timeToTravel) = FindBestPossibleIntelligenceObject(startPointIndex, visitedIntelligenceObjectIndexes);

            if (availableTimeInAir - timeToTravel < _problem.MovementCharacteristicsMatrix[endPointIndex - 1, startPoint!.Id - 1].Time)
            {
                if (subPath.IntelligenceObjects.Count == 0)
                {
                    subPath.SetTimeToEndBase(_problem.MovementCharacteristicsMatrix[endPointIndex - 1, startPointIndex - 1].Time);
                    break;
                }

                visitedIntelligenceObjectIndexes.Add(startPoint.Id);

                (startPoint, timeToTravel) = TryFindBestPossibleIntelligenceObject(startPointIndex, endPointIndex, visitedIntelligenceObjectIndexes, availableTimeInAir);

                if (startPoint == null)
                {
                    subPath.SetTimeToEndBase(_problem.MovementCharacteristicsMatrix[endPointIndex - 1, subPath.IntelligenceObjects[^1].Id - 1].Time);
                    break;
                }
            }

            visitedIntelligenceObjectIndexes.Add(startPoint.Id);

            availableTimeInAir -= timeToTravel;

            var clonedIntelligenceObject = (IntelligenceObject)startPoint.Clone();

            clonedIntelligenceObject.Visit(timeToTravel);

            subPath.IntelligenceObjects.Add(clonedIntelligenceObject);

            startPointIndex = startPoint.Id;

        } while (true);

        return subPath;
    }

    private (IntelligenceObject? IntelligenceObject, double TimeToTravel) TryFindBestPossibleIntelligenceObject(
        int startPointIndex, int endPointIndex, ICollection<int> intelligenceObjectIndexToSkip, double availableTimeInAir)
    {
        do
        {
            var (startPoint, timeToTravel) = FindBestPossibleIntelligenceObject(startPointIndex, intelligenceObjectIndexToSkip);

            if (startPoint == null)
                return (null, 0);

            if (availableTimeInAir - timeToTravel > _problem.MovementCharacteristicsMatrix[endPointIndex - 1, startPoint.Id - 1].Time)
                return (startPoint, timeToTravel);

            intelligenceObjectIndexToSkip.Add(startPoint.Id);
        } while (true);
    }

    private (IntelligenceObject? intelligenceObjectToVisit, double minTimeToTravel) FindBestPossibleIntelligenceObject(
        int startPointIndex, ICollection<int> intelligenceObjectIndexToSkip)
    {
        IntelligenceObject? intelligenceObjectToVisit = null;
        var maxAverageWeight = double.MinValue;
        var timeToTravel = double.MaxValue;

        for (var i = 0; i < _problem.IntelligenceObjects.Count; i++)
        {
            if (!_problem.IntelligenceObjects[i].IsVisited
                && !intelligenceObjectIndexToSkip.Contains(i + 1)
                && _problem.MovementCharacteristicsMatrix[i, startPointIndex - 1].AverageWeight > maxAverageWeight)
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
