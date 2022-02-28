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

    /// <summary>
    /// The name of the Post Script Font
    /// </summary>
    public string FontPostScriptName;

    /// <summary>
    /// Family of the font, probably only used by UI
    /// </summary>
    public string FontFamily;

    /// <summary>
    /// Style of the fond, probably only used by UI
    /// </summary>
    public string FontStyle;

    /// <summary>
    /// Used to store fonts / has a value of “_system_font_” when system-font is the selected font
    /// </summary>
    public string FontFile;

    /// <summary>
    /// Defines the amount of space above and below inline elements
    /// </summary>
    public int LineHeight;

    /// <summary>
    /// Font Digest
    /// </summary>
    public string FontDigest;

    /// <summary>
    /// Used to create space around an element's content
    /// </summary>
    public SwrvePadding Padding;
    
    /// <summary>
    /// The color of the font
    /// </summary>
    public string FontColor;

    /// <summary>
    /// The color of the background
    /// </summary>
    public string BackgroundColor;    


    
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

    public struct SwrvePadding
    {
        public int top;
        public int bottom;
        public int left;
        public int right;
    }
}
}
