namespace PathPlanning.Entities;

public class Point : ICloneable
{
    public int Id { get; }

    public double X { get; }

    public double Y { get; }

    protected Point(int id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
    }

    public Point GetPoint()
    {
        return new Point(Id, X, Y);
    }

    public virtual object Clone()
    {
        return new Point(Id, X, Y);
    }
}