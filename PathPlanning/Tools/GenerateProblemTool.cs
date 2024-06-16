using PathPlanning.Entities;

namespace PathPlanning.Tools;

public class GenerateProblemTool
{
    private const double MaxWidth = 1200;
    private const double HeightCenter = 250;

    private readonly int _basesCount;
    private readonly int _intelligenceObjectsCount;
    private readonly double _maxTimeInAirCoefficient;
    private readonly bool _isEquivalent;

    private readonly double _sectionWidth;
    private readonly double _baseRadius;
    private readonly double _minDistance;
    private readonly double _intelligenceObjectsRadius;

    private static readonly Random Random = new();

    public GenerateProblemTool(int basesCount, int intelligenceObjectsCount = 0, double maxTimeInAirCoefficient = 2, bool isForExperiments = false, bool isEquivalent = false)
    {
        _basesCount = basesCount;
        _intelligenceObjectsCount = intelligenceObjectsCount;
        _maxTimeInAirCoefficient = maxTimeInAirCoefficient;
        _isEquivalent = isEquivalent;
        _sectionWidth = MaxWidth / _basesCount;
        _baseRadius = MaxWidth / _basesCount / 8;
        _minDistance = isForExperiments
            ? _baseRadius * 0.2
            : _baseRadius;
        _intelligenceObjectsRadius = MaxWidth / _basesCount * 0.4; // 0.025 - 10 px 
    }

    public Problem Generate()
    {
        var neighborIntelligenceObjectsCountPerBase = GetNeighborIntelligenceObjectsCountPerBase();
        
        var intelligenceObjects = new List<IntelligenceObject>();
        var bases = new List<Base>();

        var intelligenceObjectsIndex = 0;

        for (var i = 0; i < _basesCount; i++)
        {
            var newBase = GenerateBase(i + 1);
            
            bases.Add(newBase);

            intelligenceObjects.AddRange(GenerateIntelligenceObjects(
                newBase,
                neighborIntelligenceObjectsCountPerBase[i],
                intelligenceObjectsIndex));

            intelligenceObjectsIndex += neighborIntelligenceObjectsCountPerBase[i];
        }

        var maxDistanceWithoutRecharge = _maxTimeInAirCoefficient * GetMinDistanceBetweenBases(bases.ToArray());

        return new Problem(
            bases,
            intelligenceObjects,
            maxDistanceWithoutRecharge / 100,
            100,
            5d / 60d,
            false);
    }

    private int[] GetNeighborIntelligenceObjectsCountPerBase()
    {
        var result = new int[_basesCount];

        var intelligenceObjectsCount = _intelligenceObjectsCount == 0
            ? (_basesCount - 1) * 10
            : _intelligenceObjectsCount;

        var intelligenceObjectsCountPerBase = (int)(intelligenceObjectsCount * 0.8 / _basesCount);
        var additionalIntelligenceObjectsCount = intelligenceObjectsCount - intelligenceObjectsCountPerBase * _basesCount;

        for (var i = 0; i < _basesCount; i++)
        {
            result[i] = intelligenceObjectsCountPerBase;
        }

        for (var i = 0; i < additionalIntelligenceObjectsCount; i++)
        {
            var index = Random.Next(1, _basesCount - 2);
            result[index]++;
        }

        return result.ToArray();
    }

    private Base GenerateBase(int baseIndex)
    {
        var sectionWidthCenter = (baseIndex - 0.5) * _sectionWidth;

        int x;
        int y;
        double distance;

        do
        {
            x = Random.Next((int)(sectionWidthCenter - _baseRadius), (int)(sectionWidthCenter + _baseRadius));
            y = Random.Next((int)(HeightCenter - _baseRadius), (int)(HeightCenter + _baseRadius));

            distance = Math.Sqrt(Math.Pow(x - sectionWidthCenter, 2) + Math.Pow(y - HeightCenter, 2));

        } while (distance > _baseRadius);

        return new Base(baseIndex, x, y);
    }

    private IntelligenceObject[] GenerateIntelligenceObjects(Base currentBase, int intelligenceObjectsCount, int startIntelligenceObjectIndex)
    {
        var intelligenceObjects = new List<IntelligenceObject>();

        var sectionWidthCenter = (currentBase.Id - 1) * _sectionWidth + _sectionWidth * 0.5;

        for (var i = 1; i <= intelligenceObjectsCount; i++)
        {
            int x;
            int y;
            var isValid = false;

            do
            {
                x = Random.Next((int)(sectionWidthCenter - _intelligenceObjectsRadius), (int)(sectionWidthCenter + _intelligenceObjectsRadius));
                y = Random.Next((int)(HeightCenter - _intelligenceObjectsRadius), (int)(HeightCenter + _intelligenceObjectsRadius));

                var distance = Math.Sqrt(Math.Pow(x - sectionWidthCenter, 2) + Math.Pow(y - HeightCenter, 2));
                var distanceToBase = Math.Sqrt(Math.Pow(x - currentBase.X, 2) + Math.Pow(y - currentBase.Y, 2));

                if (distance > _intelligenceObjectsRadius || distance < _minDistance || distanceToBase < _minDistance)
                {
                    continue;
                }

                isValid = IsValidIntelligenceObject(x, y, intelligenceObjects);
            } while (!isValid);

            intelligenceObjects.Add(new IntelligenceObject(startIntelligenceObjectIndex + i, x, y, _isEquivalent ? 1 : Random.Next(1, 11)));
        }

        intelligenceObjects = intelligenceObjects
            .OrderBy(x => x.X)
            .ThenBy(x => x.Y)
            .ToList();

        var sortedIntelligenceObjects = new List<IntelligenceObject>();

        for (var i = 0; i < intelligenceObjectsCount; i++)
        {
            sortedIntelligenceObjects.Add(new IntelligenceObject(
                startIntelligenceObjectIndex + i + 1, 
                intelligenceObjects[i].X, 
                intelligenceObjects[i].Y, 
                intelligenceObjects[i].Weight));
        }

        return sortedIntelligenceObjects.ToArray();
    }

    private bool IsValidIntelligenceObject(int x, int y, List<IntelligenceObject> intelligenceObjects)
    {
        foreach (var intelligenceObject in intelligenceObjects)
        {
            var distance = Math.Sqrt(Math.Pow(x - intelligenceObject.X, 2) + Math.Pow(y - intelligenceObject.Y, 2));

            if (distance < _minDistance)
                return false;
        }

        return true;
    }

    private double GetMinDistanceBetweenBases(Base[] bases)
    {
        var minDistance = double.MaxValue;

        for (var i = 0; i < _basesCount - 1; i++)
        {
            var distance = Math.Sqrt(Math.Pow(bases[i + 1].X - bases[i].X, 2) + Math.Pow(bases[i + 1].Y - bases[i].Y, 2));

            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        return minDistance;
    }
}
