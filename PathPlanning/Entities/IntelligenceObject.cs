namespace PathPlanning.Entities;

public class IntelligenceObject : Point
{
    public double Weight { get; }

    public bool IsVisited { get; private set; }

    public double TimeForMovement { get; private set; }

    public IntelligenceObject(int id, double x, double y, double w) : base(id, x, y)
    {
        Weight = w;
    }

    public void Visit(double timeForMovement)
    {
        IsVisited = true;
        TimeForMovement = timeForMovement;
    }

    public override object Clone()
    {
        var intelligenceObject = new IntelligenceObject(Id, X, Y, Weight);

        if (IsVisited)
        {
            intelligenceObject.Visit(TimeForMovement);
        }

        return intelligenceObject;
    }
}
