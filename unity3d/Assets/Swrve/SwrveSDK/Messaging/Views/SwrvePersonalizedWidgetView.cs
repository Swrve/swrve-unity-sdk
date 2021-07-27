using System;
using UnityEngine;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Used internally to render in-app message personalized text using Unity IMGUI.
/// </summary>
public class SwrveMessagePersonalizedWidgetView : SwrveWidgetView, ISwrveButtonView
{
    protected readonly SwrveWidget widget;

    private bool Pressed = false;

    /// <summary>
    /// Pointer bounds.
    /// </summary>
    public Rect PointerRect;

    // Visible for tests
    public GUIContent content;
    public SwrveButton button;
    public bool isButton;
    protected GUIStyle style;

    protected Color backgroundColor;
    protected Color clickTintColor;

    public SwrveMessagePersonalizedWidgetView(SwrveWidget widget, string resolvedText, SwrveInAppMessageConfig inAppConfig)
    {
        this.widget = widget;
        content = new GUIContent(resolvedText);
        style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = inAppConfig.PersonalizedTextForegroundColor;
        style.font = inAppConfig.PersonalizedTextFont;
        backgroundColor = inAppConfig.PersonalizedTextBackgroundColor;
        clickTintColor = inAppConfig.ButtonClickTintColor;

        isButton = (widget is SwrveButton);
        if (isButton) {
            button = (SwrveButton)widget;
        }
    }

    public override string GetTexturePath()
    {
        if (isButton) {
            return this.button.Image;
        } else {
            return ((SwrveImage)widget).File;
        }
    }

    public override void SetTexture(Texture2D texture)
    {
        this.Texture = texture;

        if (Texture != null) {
            FitTextSizeToImage(Texture.width, Texture.height);
        }
    }

    public void ProcessButtonDown(Vector3 mousePosition)
    {
        if (isButton && PointerRect.Contains(mousePosition)) {
            Pressed = true;
        }
    }

    public SwrveButtonClickResult ProcessButtonUp(Vector3 mousePosition, SwrveMessageTextTemplatingResolver templatingResolver)
    {
        SwrveButtonClickResult clickResult = null;
        if (isButton && PointerRect.Contains(mousePosition) && Pressed) {
            string resolvedAction = templatingResolver.ActionResolution[button];
            clickResult = new SwrveButtonClickResult(button, resolvedAction);
        }

        Pressed = false;

        return clickResult;
    }

    private void FitTextSizeToImage(int maxWidth, int maxHeight)
    {
        float testTextSize = 200;
        style.fontSize = (int)testTextSize;
        Vector2 size = style.CalcSize(content);
        float scalex = testTextSize / size.x;
        float scaley = testTextSize / size.y;
        style.fontSize = (int)(Math.Min(maxWidth * scalex, maxHeight * scaley));
    }

    public override void Render(float scale, int centerx, int centery, bool rotatedFormat)
    {
        if (Texture != null) {
            int textureWidth = Texture.width;
            int textureHeight = Texture.height;

            float computedSize = scale;
            Point centerPoint = widget.GetCenteredPosition(textureWidth, textureHeight, computedSize, scale);
            centerPoint.X += centerx;
            centerPoint.Y += centery;
            Rect.x = centerPoint.X;
            Rect.y = centerPoint.Y;
            Rect.width = textureWidth * computedSize;
            Rect.height = textureHeight * computedSize;

            if (isButton) {
                if (rotatedFormat) {
                    // Rotate 90 degrees the hit area
                    Point widgetCenter = button.GetCenter(textureWidth, textureHeight, computedSize);
                    PointerRect.x = centerx - (widget.Position.Y * scale) + widgetCenter.Y;
                    PointerRect.y = centery + (widget.Position.X * scale) + widgetCenter.X;
                    PointerRect.width = Rect.height;
                    PointerRect.height = Rect.width;
                } else {
                    PointerRect = Rect;
                }
            }
            ImgGUI.color = (Pressed)? backgroundColor * clickTintColor : backgroundColor;
            ImgGUI.DrawTexture (Rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0.0f);

            ImgGUI.color = (Pressed)? Color.white * clickTintColor : Color.white;
            ImgGUI.Label(Rect, content, style);
        }
    }

    // Visible for tests
    public SwrveButton GetButton()
    {
        return button;
    }
}
}
