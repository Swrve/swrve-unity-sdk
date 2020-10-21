using System;
using UnityEngine;

namespace SwrveUnity.Messaging
{
#pragma warning disable 0618
/// <summary>
/// Used internally to render in-app message personalized text using Unity IMGUI.
/// </summary>
public class SwrveMessagePersonalizedWidgetView : SwrveWidgetView
{
    protected readonly SwrveWidget widget;

    // Visible for tests
    public GUIContent content;
    protected GUIStyle style;
    protected int width;
    protected int height;
    protected SwrveButton button;
    protected bool isButton;

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

        if (widget.Texture != null) {
            FitTextSizeToImage(widget.Texture.width, widget.Texture.height);
        }

        isButton = (widget is SwrveButton);
        if (isButton) {
            button = (SwrveButton)widget;
        }
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

    public void Render(float scale, int centerx, int centery, bool rotatedFormat, ISwrveMessageAnimator animator)
    {
        if (widget.Texture != null) {
            int textureWidth = widget.Texture.width;
            int textureHeight = widget.Texture.height;

            float computedSize = scale * widget.AnimationScale;
            Point centerPoint = widget.GetCenteredPosition (textureWidth, textureHeight, computedSize, scale);
            centerPoint.X += centerx;
            centerPoint.Y += centery;
            widget.Rect.x = centerPoint.X;
            widget.Rect.y = centerPoint.Y;
            widget.Rect.width = textureWidth * computedSize;
            widget.Rect.height = textureHeight * computedSize;

            bool pressed = false;
            if (isButton) {
                pressed = button.Pressed;
                if (rotatedFormat) {
                    // Rotate 90 degrees the hit area
                    Point widgetCenter = button.GetCenter (textureWidth, textureHeight, computedSize);
                    button.PointerRect.x = centerx - (widget.Position.Y * scale) + widgetCenter.Y;
                    button.PointerRect.y = centery + (widget.Position.X * scale) + widgetCenter.X;
                    button.PointerRect.width = widget.Rect.height;
                    button.PointerRect.height = widget.Rect.width;
                } else {
                    button.PointerRect = button.Rect;
                }
                if (animator != null) {
                    animator.AnimateButtonPressed (button);
                }
            }
            GUI.color = (pressed)? backgroundColor * clickTintColor : backgroundColor;
            GUI.DrawTexture (widget.Rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0.0f);

            GUI.color = (pressed)? Color.white * clickTintColor : Color.white;
            GUI.Label(widget.Rect, content, style);
        }
    }
}
#pragma warning restore 0618
}
