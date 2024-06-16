namespace PathPlanning.Entities;

public class Base : Point
{
    public Base(int id, double x, double y) : base(id, x, y)
    {
    }

    public override object Clone()
    {
        return new Base(Id, X, Y);
    }
}
