using System.Globalization;
using System.Text;
using System.Xml;
using PathPlanning.Entities;
using PathPlanning.Exceptions;

namespace PathPlanning.Tools;

public class VisualizeProblemTool
{
    private const string MaxWidth = "1200";
    private const string MaxHeight = "500";

    private readonly Problem _problem;
    private readonly Solution? _solution;
    private readonly bool _addCoordinates;

    public VisualizeProblemTool(Problem problem, Solution? solution = null, bool addCoordinates = false)
    {
        _problem = problem;
        _solution = solution;
        _addCoordinates = addCoordinates;
    }

    private readonly XmlDocument _svgDoc = new();

    public void Visualize()
    {
        if (_problem.BasesCount > 6)
            throw new VisualizeProblemException("Problems visualization for more than 6 bases is not supported.");

        var svgRoot = _svgDoc.CreateElement("svg");
        svgRoot.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
        svgRoot.SetAttribute("width", MaxWidth );
        svgRoot.SetAttribute("height", MaxHeight );
        _svgDoc.AppendChild(svgRoot);

        var svgRect = _svgDoc.CreateElement("rect");
        svgRect.SetAttribute("x", "0");
        svgRect.SetAttribute("y", "0");
        svgRect.SetAttribute("width", MaxWidth);
        svgRect.SetAttribute("height", MaxHeight);
        svgRect.SetAttribute("fill", "#F6F5F2");
        svgRect.SetAttribute("stroke", "black");
        svgRect.SetAttribute("stroke-width", "2");
        svgRoot.AppendChild(svgRect);

        if (_addCoordinates)
        {
            VisualizeCoordinates(svgRoot);
        }

        if (_solution != null)
        {
            VisualizeSolution(svgRoot);
        }

        VisualizeBases(svgRoot);
        VisualizeIntelligenceObjects(svgRoot);

        var fileName = $"output-{DateTime.Now:yyyy-MM-ddTHH-mm-ss-fff}.svg";

        using var writer = new XmlTextWriter(fileName, null);
        writer.Formatting = Formatting.Indented;
        _svgDoc.Save(writer);
    }

    private void VisualizeCoordinates(XmlElement svgRoot)
    {
        var segmentsCountX = _problem.BasesCount * 8;
        var coordinateSizeX = Convert.ToDouble(MaxWidth) / segmentsCountX;
        var segmentsCountY = (int)(Convert.ToDouble(MaxHeight) / coordinateSizeX);
        var coordinateSizeY = Convert.ToDouble(MaxHeight) / segmentsCountY;

        for (var i = 0; i < segmentsCountX; i++)
        {
            var svgLine = _svgDoc.CreateElement("line");
            svgLine.SetAttribute("x1", (i * coordinateSizeX).ToString(CultureInfo.InvariantCulture));
            svgLine.SetAttribute("y1", "0");
            svgLine.SetAttribute("x2", (i * coordinateSizeX).ToString(CultureInfo.InvariantCulture));
            svgLine.SetAttribute("y2", MaxHeight);

            if (i % 8 == 0)
            {
                svgLine.SetAttribute("stroke", "blue");
                svgLine.SetAttribute("stroke-width", "1");
            }
            else
            {
                svgLine.SetAttribute("stroke", "black");
                svgLine.SetAttribute("stroke-width", "0.5");
            }
            
            svgRoot.AppendChild(svgLine);
        }

        for (var i = 0; i < segmentsCountY; i++)
        {
            var svgLine = _svgDoc.CreateElement("line");
            svgLine.SetAttribute("x1", "0");
            svgLine.SetAttribute("y1", (i * coordinateSizeY).ToString(CultureInfo.InvariantCulture));
            svgLine.SetAttribute("x2", MaxWidth);
            svgLine.SetAttribute("y2", (i * coordinateSizeY).ToString(CultureInfo.InvariantCulture));
            svgLine.SetAttribute("stroke", "black");
            svgLine.SetAttribute("stroke-width", "0.5");

            svgRoot.AppendChild(svgLine);
        }
    }

    private void VisualizeSolution(XmlElement svgRoot)
    {
        var colors = new [] { "#FFC94A", "#5BBCFF", "#2C7865", "#A490BA", "#F44336" };

        for (var i = 0; i < _problem.SubPathCount; i++)
        {
            var points = new List<Point> { _solution!.SubPaths[i].StartBase.GetPoint() };
            points.AddRange(_solution.SubPaths[i].IntelligenceObjects.Select(x => x.GetPoint()));
            points.Add(_solution.SubPaths[i].EndBase.GetPoint());

            for (var j = 0; j < points.Count - 1; j++)
            {
                var svgLine = _svgDoc.CreateElement("line");
                
                svgLine.SetAttribute("x1", points[j].X.ToString(CultureInfo.InvariantCulture));
                svgLine.SetAttribute("y1", points[j].Y.ToString(CultureInfo.InvariantCulture));
                svgLine.SetAttribute("x2", points[j + 1].X.ToString(CultureInfo.InvariantCulture));
                svgLine.SetAttribute("y2", points[j + 1].Y.ToString(CultureInfo.InvariantCulture));
                svgLine.SetAttribute("stroke", colors[i]);
                svgLine.SetAttribute("stroke-width", "2");
                
                svgRoot.AppendChild(svgLine);
            }
        }
    }

    private void VisualizeBases(XmlElement svgRoot)
    {
        foreach (var point in _problem.Bases)
        {
            const double outerRadius = 10;
            const double innerRadius = 5;
            const int numPoints = 5;

            var centerY = point.Y;

            var pointsBuilder = new StringBuilder();

            for (var j = 0; j < numPoints * 2; j++)
            {
                var radius = j % 2 == 0 ? outerRadius : innerRadius;
                var angle = Math.PI * 2 / (numPoints * 2) * j + Math.PI / 2;
                var x = point.X + radius * Math.Cos(angle);
                var y = centerY - (point.Y + radius * Math.Sin(angle) - centerY);

                pointsBuilder.Append($"{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)} ");
            }

            var svgStar = _svgDoc.CreateElement("polygon");
            svgStar.SetAttribute("points", pointsBuilder.ToString().Trim());
            svgStar.SetAttribute("fill", "#87A922");
            svgStar.SetAttribute("stroke", "black");
            svgStar.SetAttribute("stroke-width", "1.5");
            svgRoot.AppendChild(svgStar);

            var svgText = _svgDoc.CreateElement("text");
            svgText.SetAttribute("x", (point.X - outerRadius + 3).ToString(CultureInfo.InvariantCulture));
            svgText.SetAttribute("y", (point.Y - outerRadius - 7).ToString(CultureInfo.InvariantCulture));
            svgText.SetAttribute("font-size", "12");
            svgText.SetAttribute("font-weight", "bold");
            svgText.SetAttribute("fill", "black");
            svgText.InnerText = $"A{point.Id}";
            svgRoot.AppendChild(svgText);
        }
    }

    private void VisualizeIntelligenceObjects(XmlElement svgRoot)
    {
        foreach (var point in _problem.IntelligenceObjects)
        {
            var svgCircle = _svgDoc.CreateElement("circle");
            svgCircle.SetAttribute("cx", point.X.ToString(CultureInfo.InvariantCulture));
            svgCircle.SetAttribute("cy", point.Y.ToString(CultureInfo.InvariantCulture));
            svgCircle.SetAttribute("r", "8");
            svgCircle.SetAttribute("fill", "#D04848");
            svgCircle.SetAttribute("stroke", "black");
            svgCircle.SetAttribute("stroke-width", "2");
            svgRoot.AppendChild(svgCircle);

            var svgText = _svgDoc.CreateElement("text");
            svgText.SetAttribute("x", (point.Id < 10 ? point.X - 5 : point.X - 7).ToString(CultureInfo.InvariantCulture));
            svgText.SetAttribute("y", (point.Y - 14).ToString(CultureInfo.InvariantCulture));
            svgText.SetAttribute("font-size", "12");
            svgText.SetAttribute("fill", "black");
            svgText.InnerText = $"J{point.Id}";
            svgRoot.AppendChild(svgText);
        }
    }
}
