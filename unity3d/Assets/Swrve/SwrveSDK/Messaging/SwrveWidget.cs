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
    /// Personalized text (render this instead of the image).
    /// </summary>
    public string Text;

    /// <summary>
    /// Url which points to a dynamic image to be used in place of 'Image'.
    /// </summary>
    public string DynamicImageUrl;

    /// <summary>
    /// Parent message associated with this button
    /// </summary>
    public SwrveMessage Message;

    /// <summary>
    /// if true, text value will have the below items applied to it
    /// </summary>
    public bool IsMultiLine;

    /// <summary>
    /// Font Size (used primarily for multi-line)
    /// </summary>
    public float FontSize;

    /// <summary>
    /// If it's text, is it scrollable? (used primarily for multi-line)
    /// </summary>
    public bool IsScrollable;

    /// <summary>
    /// What is the h alignment of the text?
    /// </summary>
    public TextAlignment HorizontalAlignment;

    public SwrveWidget()
    {
        Position = new Point(0, 0);
        Size = new Point(0, 0);
    }

    public Point GetCenter(float w, float h, float Scale)
    {
        int x = (int)(-w * Scale / 2.0);
        int y = (int)(-h * Scale / 2.0);
        return new Point(x, y);
    }

    public Point GetCenteredPosition(float w, float h, float Scale, float FormatScale)
    {
        Point center = GetCenter(w, h, Scale);
        int x = (int)(center.X + Position.X * FormatScale);
        int y = (int)(center.Y + Position.Y * FormatScale);
        return new Point(x, y);
    }
}
}
