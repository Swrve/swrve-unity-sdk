namespace SwrveUnity.Messaging
{
/// <summary>
/// Used internally to represent a point in 2D space.
/// </summary>
public struct Point {
    public int X;
    public int Y;

    public Point (int x, int y)
    {
        this.X = x;
        this.Y = y;
    }
}
}
