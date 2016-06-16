using UnityEngine;

namespace Swrve.Messaging
{
/// <summary>
/// Base in-app message element class.
/// </summary>
public abstract class SwrveWidget
{
    /// <summary>
    /// Position of the widget.
    /// </summary>
    public Point Position;

    /// <summary>
    /// Size of the widget.
    /// </summary
    public Point Size;

    /// <summary>
    /// Loaded texture asset.
    /// </summary>
    public Texture2D Texture;

    /// <summary>
    /// Redering bounds.
    /// </summary>
    public Rect Rect;

    /// <summary>
    /// Extra scaling to use when animating.
    /// </summary>
    public float AnimationScale = 1f;

    public SwrveWidget ()
    {
        Position = new Point (0, 0);
        Size = new Point (0, 0);
    }

    public Point GetCenter (float w, float h, float Scale)
    {
        int x = (int)(-w * Scale / 2.0);
        int y = (int)(-h * Scale / 2.0);
        return new Point (x, y);
    }

    public Point GetCenteredPosition (float w, float h, float Scale, float FormatScale)
    {
        Point center = GetCenter (w, h, Scale);
        int x = (int)(center.X + Position.X * FormatScale);
        int y = (int)(center.Y + Position.Y * FormatScale);
        return new Point (x, y);
    }
}
}
