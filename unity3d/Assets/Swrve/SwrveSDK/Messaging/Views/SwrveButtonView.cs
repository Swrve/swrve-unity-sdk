using System;
using UnityEngine;

namespace SwrveUnity.Messaging
{
#pragma warning disable 0618
/// <summary>
/// Used internally to render in-app message images using Unity IMGUI.
/// </summary>
public class SwrveButtonView : SwrveWidgetView
{
    protected readonly SwrveButton button;

    protected readonly Color clickTintColor;

    public SwrveButtonView(SwrveButton button, Color clickTintColor)
    {
        this.button = button;
        this.clickTintColor = clickTintColor;
    }

    public void Render(float scale, int centerx, int centery, bool rotatedFormat, ISwrveMessageAnimator animator)
    {
        if (button.Texture != null) {
            int textureWidth = button.Texture.width;
            int textureHeight = button.Texture.height;

            float computedSize = scale * button.AnimationScale;
            Point centerPoint = button.GetCenteredPosition (textureWidth, textureHeight, computedSize, scale);
            button.Rect.x = centerPoint.X + centerx;
            button.Rect.y = centerPoint.Y + centery;
            button.Rect.width = textureWidth * computedSize;
            button.Rect.height = textureHeight * computedSize;

            if (rotatedFormat) {
                // Rotate 90 degrees the hit area
                Point widgetCenter = button.GetCenter (textureWidth, textureHeight, computedSize);
                button.PointerRect.x = centerx - (button.Position.Y * scale) + widgetCenter.Y;
                button.PointerRect.y = centery + (button.Position.X * scale) + widgetCenter.X;
                button.PointerRect.width = textureHeight;
                button.PointerRect.height = textureWidth;
            } else {
                button.PointerRect = button.Rect;
            }
            if (animator != null) {
                animator.AnimateButtonPressed (button);
            } else {
                GUI.color = (button.Pressed) ? clickTintColor : Color.white;
            }
            GUI.DrawTexture (button.Rect, button.Texture, ScaleMode.StretchToFill, true, 0.0f);
        } else {
            GUI.Box (button.Rect, button.Image);
        }
    }
}
#pragma warning restore 0618
}
