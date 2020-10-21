using UnityEngine;

namespace SwrveUnity.Messaging
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
    /// </summary>
    public Point Size;

    /// <summary>
    /// Loaded texture asset.
    /// </summary>
    [System.Obsolete("This property will be removed in 8.0. Please use File in combination of SwrveSDK.AssetPath(file) to load the texture if needed on your own")]
    public Texture2D Texture;

    /// <summary>
    /// Personalized text (render this instead of the image)
    /// </summary>
    public string Text;

    /// <summary>
    /// Redering bounds.
    /// </summary>
    [System.Obsolete("This property will be removed in 8.0")]
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
