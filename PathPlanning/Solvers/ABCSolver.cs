using PathPlanning.Entities;
using PathPlanning.Solvers.Interfaces;
using PathPlanning.Solvers.Options;

namespace PathPlanning.Solvers;

public class AbcSolver : ISolver
{
    private readonly Problem _problem;
    private readonly InitialSolutionOptions _initialSolutionOption;
    private readonly LocalOptimizationOptions _localOptimizationOption;

    private readonly int _scoutBeesCount;
    private readonly int _bestScoutBeesCount;
    private readonly int _foragerBeesCount;
    private readonly int _maxIterationsWithoutImprovement;
    private readonly int _maxTotalIterations;

    private int _currentStartBaseIndex = 1;
    private List<int> _allReachableIntelligenceObjects = new();
    private List<SubPath> _bestSubPaths = new();

    private List<string> _previouslyGeneratedSubPathIndexes = new();

    private static readonly Random Random = new();

    public AbcSolver(
        Problem problem, 
        InitialSolutionOptions initialSolutionOption, 
        LocalOptimizationOptions localOptimizationOption,
        double maxIterationsCoefficient = 1d,
        double scoutBeesCoefficient = 1d,
        double foragerBeesCoefficient = 1d)
    {
        _problem = problem;
        _initialSolutionOption = initialSolutionOption;
        _localOptimizationOption = localOptimizationOption;
        _scoutBeesCount = (int)(problem.IntelligenceObjectsCount * 1.5 * scoutBeesCoefficient);
        _bestScoutBeesCount = (int)(problem.IntelligenceObjectsCount / 2d);
        _foragerBeesCount = (int)(problem.IntelligenceObjectsCount / 4d * foragerBeesCoefficient);
        _maxIterationsWithoutImprovement = (int)(problem.BasesCount * 5 * maxIterationsCoefficient);
        _maxTotalIterations = (int)(_maxIterationsWithoutImprovement * problem.IntelligenceObjectsOnSubPath * maxIterationsCoefficient);
    }

    public Solution Solve()
    {
        var solution = new Solution(_problem.ServiceTimeInHours);
        
        var totalIterations = 0;

        for (var i = 1; i <= _problem.SubPathCount; i++)
        {
            _currentStartBaseIndex = i;

            if (_initialSolutionOption == InitialSolutionOptions.Random)
            {
                _allReachableIntelligenceObjects = GetAllReachableIntelligenceObjectIndexes(_currentStartBaseIndex);
            }

            var iterationsWithoutImprovement = 0;

            while (iterationsWithoutImprovement != _maxIterationsWithoutImprovement && totalIterations != _maxTotalIterations)
            {
                var subPaths = new List<SubPath>();

                switch (_initialSolutionOption)
                {
                    case InitialSolutionOptions.Random:
                        subPaths.AddRange(GenerateRandomScoutBees());
                        break;
                    case InitialSolutionOptions.Probable:
                        subPaths.AddRange(GenerateProbableScoutBees());
                        break;
                    case InitialSolutionOptions.ProbableNeighborhood:
                        subPaths.AddRange(GenerateProbableNeighborhoodScoutBees());
                        break;
                    default:
                        throw new ArgumentException("Invalid InitialSolutionOptions type");
                }

                _bestSubPaths = subPaths.Take(_bestScoutBeesCount).ToList();

                bool isOptimized;

                switch (_localOptimizationOption)
                {
                    case LocalOptimizationOptions.AddNearest:
                        isOptimized = AddNearestOptimization();
                        break;
                    case LocalOptimizationOptions.RebuildProbable:
                        isOptimized = RebuildProbableOptimization();
                        break;
                    case LocalOptimizationOptions.RebuildProbableNeighborhood:
                        isOptimized = RebuildProbableNeighborhoodOptimization();
                        break;
                    case LocalOptimizationOptions.RebuildProbableAndAddNearest:
                        isOptimized = RebuildProbableAndAddNearestOptimization();
                        break;
                    case LocalOptimizationOptions.RebuildProbableNeighborhoodAndAddNearest:
                        isOptimized = RebuildProbableNeighborhoodAndAddNearestOptimization();
                        break;
                    default:
                        throw new ArgumentException("Invalid LocalOptimizationOptions type");
                }

                if (!isOptimized)
                    iterationsWithoutImprovement++;
                else
                    iterationsWithoutImprovement = 0;

                totalIterations++;
            }

            var subPath = _bestSubPaths.MaxBy(x => x.GetTotalWeight());

            if (subPath != null)
            {
                solution.SubPaths.Add(subPath);

                foreach (var intelligenceObject in subPath.IntelligenceObjects)
                {
                    _problem.IntelligenceObjects[intelligenceObject.Id - 1].Visit(intelligenceObject.TimeForMovement);
                }
            }

            _bestSubPaths = new List<SubPath>();
        }

        return solution;
    }

    private bool AddNearestOptimization()
    {
        var isOptimized = false;
        var bestSubPaths = new List<SubPath>();

        foreach (var subPath in _bestSubPaths)
        {
            var currentBest = subPath;
            var tempBest = subPath;

            while (tempBest != null)
            {
                tempBest = TryToAddNearestOptimize(currentBest);

                if (tempBest != null)
                {
                    currentBest = tempBest;
                    isOptimized = true;
                }
            }

            bestSubPaths.Add(currentBest);
        }

        _bestSubPaths = bestSubPaths;

        return isOptimized;
    }

    private SubPath? TryToAddNearestOptimize(SubPath subPath)
    {
        var currentIntelligenceObjectIndexes = subPath.IntelligenceObjects.Select(x => x.Id).ToArray();

        var optimized = new List<SubPath>();

        for (var j = 0; j < _foragerBeesCount; j++)
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

    private bool RebuildProbableOptimization()
    {
        var isOptimized = false;
        var bestSubPaths = new List<SubPath>();

        foreach (var subPath in _bestSubPaths)
        {
            var currentBest = subPath;
            var tempBest = subPath;

            while (tempBest != null)
            {
                tempBest = TryToRebuildProbableOptimize(currentBest);

                if (tempBest != null)
                {
                    currentBest = tempBest;
                    isOptimized = true;
                }
            }

            bestSubPaths.Add(currentBest);
        }

        _bestSubPaths = bestSubPaths;

        return isOptimized;
    }

    private SubPath? TryToRebuildProbableOptimize(SubPath subPath)
    {
        var currentIntelligenceObjectIndexes = subPath.IntelligenceObjects.Select(x => x.Id).ToArray();

        var optimized = new List<SubPath>();

        for (var j = 0; j < _foragerBeesCount; j++)
        {
            var pointToOptimize = Random.Next(0, subPath.IntelligenceObjects.Count);

            var toOptimize = currentIntelligenceObjectIndexes.Take(pointToOptimize + 1).ToList();

            var newSubPathPoints = FindProbableScoutBee(toOptimize, CalculateAvailableTimeInAir(toOptimize));

            optimized.Add(CreateSubPath(newSubPathPoints));
        }

        var bestSubPath = optimized.MaxBy(x => x.GetTotalWeight());

        return bestSubPath?.GetTotalWeight() > subPath.GetTotalWeight() ? bestSubPath : null;
    }

    private bool RebuildProbableNeighborhoodOptimization()
    {
        var isOptimized = false;
        var bestSubPaths = new List<SubPath>();

        foreach (var subPath in _bestSubPaths)
        {
            var currentBest = subPath;
            var tempBest = subPath;

            while (tempBest != null)
            {
                tempBest = TryToRebuildProbableNeighborhoodOptimize(currentBest);

                if (tempBest != null)
                {
                    currentBest = tempBest;
                    isOptimized = true;
                }
            }

            bestSubPaths.Add(currentBest);
        }

        _bestSubPaths = bestSubPaths;

        return isOptimized;
    }

    private SubPath? TryToRebuildProbableNeighborhoodOptimize(SubPath subPath)
    {
        var currentIntelligenceObjectIndexes = subPath.IntelligenceObjects.Select(x => x.Id).ToArray();

        var optimized = new List<SubPath>();

        for (var j = 0; j < _foragerBeesCount; j++)
        {
            var pointToOptimize = Random.Next(0, subPath.IntelligenceObjects.Count);

            var toOptimize = currentIntelligenceObjectIndexes.Take(pointToOptimize + 1).ToList();

            var newSubPathPoints = FindProbableNeighborhoodScoutBee(toOptimize, CalculateAvailableTimeInAir(toOptimize));

            optimized.Add(CreateSubPath(newSubPathPoints));
        }

        var bestSubPath = optimized.MaxBy(x => x.GetTotalWeight());

        return bestSubPath?.GetTotalWeight() > subPath.GetTotalWeight() ? bestSubPath : null;
    }

    private bool RebuildProbableAndAddNearestOptimization()
    {
        var isOptimized = false;
        var bestSubPaths = new List<SubPath>();

        foreach (var subPath in _bestSubPaths)
        {
            var currentBest = subPath;
            var tempBest = subPath;

            while (tempBest != null)
            {
                tempBest = TryToRebuildProbableAndAddNearestOptimize(currentBest);

                if (tempBest != null)
                {
                    currentBest = tempBest;
                    isOptimized = true;
                }
            }

            bestSubPaths.Add(currentBest);
        }

        _bestSubPaths = bestSubPaths;

        return isOptimized;
    }

    private SubPath? TryToRebuildProbableAndAddNearestOptimize(SubPath subPath)
    {
        var currentIntelligenceObjectIndexes = subPath.IntelligenceObjects.Select(x => x.Id).ToArray();

        var optimized = new List<SubPath>();

        for (var j = 0; j < _foragerBeesCount; j++)
        {
            var pointToOptimize = Random.Next(0, subPath.IntelligenceObjects.Count);

            var toOptimize = currentIntelligenceObjectIndexes.Take(pointToOptimize + 1).ToList();

            var newSubPathPoints = FindProbableScoutBee(toOptimize, CalculateAvailableTimeInAir(toOptimize));

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
        var bestSubPaths = new List<SubPath>();

        foreach (var subPath in _bestSubPaths)
        {
            var currentBest = subPath;
            var tempBest = subPath;

            while (tempBest != null)
            {
                tempBest = TryToRebuildProbableNeighborhoodAndAddNearestOptimize(currentBest);

                if (tempBest != null)
                {
                    currentBest = tempBest;
                    isOptimized = true;
                }
            }

            bestSubPaths.Add(currentBest);
        }

        _bestSubPaths = bestSubPaths;

        return isOptimized;
    }

    private SubPath? TryToRebuildProbableNeighborhoodAndAddNearestOptimize(SubPath subPath)
    {
        var currentIntelligenceObjectIndexes = subPath.IntelligenceObjects.Select(x => x.Id).ToArray();

        var optimized = new List<SubPath>();

        for (var j = 0; j < _foragerBeesCount; j++)
        {
            var pointToOptimize = Random.Next(0, subPath.IntelligenceObjects.Count);

            var toOptimize = currentIntelligenceObjectIndexes.Take(pointToOptimize + 1).ToList();

            var newSubPathPoints = FindProbableNeighborhoodScoutBee(toOptimize, CalculateAvailableTimeInAir(toOptimize));

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

    private IEnumerable<SubPath> GenerateRandomScoutBees()
    {
        var subPathsToCheck = new List<SubPath>();

        var newGeneratedSubPathIndexes = new List<string>();

        var maxCountOfPoints = _allReachableIntelligenceObjects.Count < _problem.IntelligenceObjectsOnSubPath
            ? _allReachableIntelligenceObjects.Count
            : _problem.IntelligenceObjectsOnSubPath;

        var iterationsCount = 0;

        while (subPathsToCheck.Count != _scoutBeesCount - _bestSubPaths.Count && iterationsCount < 1000)
        {
            iterationsCount++;

            var countOfPoints = Random.Next(1, maxCountOfPoints);
            var currentReachableIntelligenceObjects = _allReachableIntelligenceObjects.Select(x => x).ToList();
            var intelligenceObjectIndexesToVisit = new List<int>();

            for (var i = 0; i < countOfPoints; i++)
            {
                var intelligenceObjectsToVisitIndex = Random.Next(0, currentReachableIntelligenceObjects.Count);
                intelligenceObjectIndexesToVisit.Add(currentReachableIntelligenceObjects[intelligenceObjectsToVisitIndex]);
                currentReachableIntelligenceObjects.Remove(currentReachableIntelligenceObjects[intelligenceObjectsToVisitIndex]);
            }

            var intelligenceObjectCode = string.Join("/", intelligenceObjectIndexesToVisit);

            if (_previouslyGeneratedSubPathIndexes.Contains(intelligenceObjectCode) || newGeneratedSubPathIndexes.Contains(intelligenceObjectCode))
            {
                continue;
            }

            newGeneratedSubPathIndexes.Add(intelligenceObjectCode);

            if (IsValidSubPath(intelligenceObjectIndexesToVisit))
            {
                subPathsToCheck.Add(CreateSubPath(intelligenceObjectIndexesToVisit));
            }
        }

        subPathsToCheck.AddRange(_bestSubPaths);

        _previouslyGeneratedSubPathIndexes = new List<string>();

        foreach (var subPath in subPathsToCheck)
        {
            var intelligenceObjectIndexesToVisit = subPath.IntelligenceObjects.Select(intelligenceObject => intelligenceObject.Id).ToList();

            _previouslyGeneratedSubPathIndexes.Add(string.Join("/", intelligenceObjectIndexesToVisit));
        }

        return subPathsToCheck.OrderByDescending(x => x.GetTotalWeight() / x.GetTimeInAir());
    }

    private IEnumerable<SubPath> GenerateProbableScoutBees()
    {
        var subPathsToCheck = new List<SubPath>();

        var newGeneratedSubPathIndexes = new List<string>();

        var iterationsCount = 0;

        while (subPathsToCheck.Count != _scoutBeesCount - _bestSubPaths.Count && iterationsCount < 1000)
        {
            iterationsCount++;
            
            var intelligenceObjectIndexesToVisit = FindProbableScoutBee();

            var intelligenceObjectCode = string.Join("/", intelligenceObjectIndexesToVisit);

            if (_previouslyGeneratedSubPathIndexes.Contains(intelligenceObjectCode) || newGeneratedSubPathIndexes.Contains(intelligenceObjectCode))
            {
                continue;
            }

            newGeneratedSubPathIndexes.Add(intelligenceObjectCode);

            subPathsToCheck.Add(CreateSubPath(intelligenceObjectIndexesToVisit));
        }

        subPathsToCheck.AddRange(_bestSubPaths);

        _previouslyGeneratedSubPathIndexes = new List<string>();

        foreach (var subPath in subPathsToCheck)
        {
            var intelligenceObjectIndexesToVisit = subPath.IntelligenceObjects.Select(intelligenceObject => intelligenceObject.Id).ToList();

            _previouslyGeneratedSubPathIndexes.Add(string.Join("/", intelligenceObjectIndexesToVisit));
        }

        return subPathsToCheck.OrderByDescending(x => x.GetTotalWeight() / x.GetTimeInAir());
    }

    private IEnumerable<SubPath> GenerateProbableNeighborhoodScoutBees()
    {
        var subPathsToCheck = new List<SubPath>();

        var newGeneratedSubPathIndexes = new List<string>();

        var iterationsCount = 0;

        while (subPathsToCheck.Count != _scoutBeesCount - _bestSubPaths.Count && iterationsCount < 1000)
        {
            iterationsCount++;

            var intelligenceObjectIndexesToVisit = FindProbableNeighborhoodScoutBee();

            var intelligenceObjectCode = string.Join("/", intelligenceObjectIndexesToVisit);

            if (_previouslyGeneratedSubPathIndexes.Contains(intelligenceObjectCode) || newGeneratedSubPathIndexes.Contains(intelligenceObjectCode))
            {
                continue;
            }

            newGeneratedSubPathIndexes.Add(intelligenceObjectCode);

            subPathsToCheck.Add(CreateSubPath(intelligenceObjectIndexesToVisit));
        }

        subPathsToCheck.AddRange(_bestSubPaths);

        _previouslyGeneratedSubPathIndexes = new List<string>();

        foreach (var subPath in subPathsToCheck)
        {
            var intelligenceObjectIndexesToVisit = subPath.IntelligenceObjects.Select(intelligenceObject => intelligenceObject.Id).ToList();

            _previouslyGeneratedSubPathIndexes.Add(string.Join("/", intelligenceObjectIndexesToVisit));
        }

        return subPathsToCheck.OrderByDescending(x => x.GetTotalWeight() / x.GetTimeInAir());
    }

    private List<int> GetAllReachableIntelligenceObjectIndexes(int startBaseIndex)
    {
        var reachableIntelligenceObjects = new List<int>();

        for (var i = 0; i < _problem.IntelligenceObjectsCount; i++)
        {
            var flightTime =
                _problem.MovementCharacteristicsMatrix[i, _problem.IntelligenceObjectsCount + startBaseIndex - 1].Time +
                _problem.MovementCharacteristicsMatrix[_problem.IntelligenceObjectsCount + startBaseIndex, i].Time;

            if (!_problem.IntelligenceObjects[i].IsVisited && flightTime < _problem.MaxTimeInAirInHours)
            {
                reachableIntelligenceObjects.Add(_problem.IntelligenceObjects[i].Id);
            }
        }

        return reachableIntelligenceObjects;
    }

    private List<int> FindProbableScoutBee(List<int> intelligenceObjectsToVisit = null, double? availableTimeInAir = null)
    {
        availableTimeInAir ??= _problem.MaxTimeInAirInHours;

        intelligenceObjectsToVisit ??= new List<int>();

        var currentPointIndex = intelligenceObjectsToVisit.Any() 
            ? intelligenceObjectsToVisit.Last()
            : _currentStartBaseIndex + _problem.IntelligenceObjectsCount;

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

            if (!availableForMovementIndexes.Any())
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

    private List<int> FindProbableNeighborhoodScoutBee(List<int> intelligenceObjectsToVisit = null, double? availableTimeInAir = null)
    {
        availableTimeInAir ??= _problem.MaxTimeInAirInHours;

        intelligenceObjectsToVisit ??= new List<int>();

        var currentPointIndex = intelligenceObjectsToVisit.Any()
            ? intelligenceObjectsToVisit.Last()
            : _currentStartBaseIndex + _problem.IntelligenceObjectsCount;

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

            if (!availableForMovementIndexes.Any())
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

    private double CalculateAvailableTimeInAir(IReadOnlyList<int> intelligenceObjectIndexesToVisit)
    {
        var timeInAir = _problem.MovementCharacteristicsMatrix[intelligenceObjectIndexesToVisit[0] - 1, _problem.IntelligenceObjectsCount + _currentStartBaseIndex - 1].Time;

        for (var j = 0; j < intelligenceObjectIndexesToVisit.Count - 1; j++)
        {
            timeInAir += _problem.MovementCharacteristicsMatrix[intelligenceObjectIndexesToVisit[j + 1] - 1, intelligenceObjectIndexesToVisit[j] - 1].Time;
        } 
        
        return _problem.MaxTimeInAirInHours - timeInAir;
    }

    private bool IsValidSubPath(List<int> intelligenceObjectIndexesToVisit)
    {
        var timeInAir = _problem.MovementCharacteristicsMatrix[intelligenceObjectIndexesToVisit[0] - 1, _problem.IntelligenceObjectsCount + _currentStartBaseIndex - 1].Time;

        for (var j = 0; j < intelligenceObjectIndexesToVisit.Count - 1; j++)
        {
            timeInAir += _problem.MovementCharacteristicsMatrix[intelligenceObjectIndexesToVisit[j] - 1, intelligenceObjectIndexesToVisit[j + 1] - 1].Time;
        }

        timeInAir += _problem.MovementCharacteristicsMatrix[_problem.IntelligenceObjectsCount + _currentStartBaseIndex, intelligenceObjectIndexesToVisit[^1] - 1].Time;

        return timeInAir <= _problem.MaxTimeInAirInHours;
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
}
