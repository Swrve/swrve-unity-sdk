using System;
using UnityEngine;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Used internally to render in-app message images using Unity IMGUI.
/// </summary>
public class SwrveButtonView : SwrveWidgetView, ISwrveButtonView
{
    protected readonly SwrveButton button;

    protected readonly Color clickTintColor;

    // Visible for tests
    public bool Pressed = false;

    public Rect PointerRect;

    public SwrveButtonView(SwrveButton button, Color clickTintColor, string dynamicImageSha1Asset)
    {
        this.button = button;
        this.clickTintColor = clickTintColor;
        DynamicImageSha1Asset = dynamicImageSha1Asset;
    }

    public override string GetTexturePath()
    {
        if (string.IsNullOrEmpty(DynamicImageSha1Asset) == false) {
            return DynamicImageSha1Asset;
        }

        return this.button.Image;
    }

    public override void SetTexture(Texture2D texture)
    {
        this.Texture = texture;
    }

    public void ProcessButtonDown(Vector3 mousePosition)
    {
        if (PointerRect.Contains(mousePosition)) {
            Pressed = true;
        }
    }

    public SwrveButtonClickResult ProcessButtonUp(Vector3 mousePosition, SwrveMessageTextTemplatingResolver templatingResolver)
    {
        SwrveButtonClickResult clickResult = null;
        if (PointerRect.Contains(mousePosition) && Pressed) {
            string resolvedAction = templatingResolver.ActionResolution[button];
            clickResult = new SwrveButtonClickResult(button, resolvedAction);
        }

        Pressed = false;

        return clickResult;
    }

    public override void Render(float scale, int centerx, int centery, bool rotatedFormat)
    {
        if (Texture != null) {
            int textureWidth = Texture.width;
            int textureHeight = Texture.height;
            var scaleMode = ScaleMode.StretchToFill;

            if (string.IsNullOrEmpty(DynamicImageSha1Asset) == false) {
                textureWidth = button.Size.X;
                textureHeight = button.Size.Y;
                scaleMode = ScaleMode.ScaleToFit;
            }

            float computedSize = scale;
            Point centerPoint = button.GetCenteredPosition(textureWidth, textureHeight, computedSize, scale);
            Rect.x = centerPoint.X + centerx;
            Rect.y = centerPoint.Y + centery;
            Rect.width = textureWidth * computedSize;
            Rect.height = textureHeight * computedSize;

            if (rotatedFormat) {
                // Rotate 90 degrees the hit area
                Point widgetCenter = button.GetCenter(textureWidth, textureHeight, computedSize);
                PointerRect.x = centerx - (button.Position.Y * scale) + widgetCenter.Y;
                PointerRect.y = centery + (button.Position.X * scale) + widgetCenter.X;
                PointerRect.width = textureHeight;
                PointerRect.height = textureWidth;
            } else {
                PointerRect = Rect;
            }

            ImgGUI.color = (Pressed) ? clickTintColor : Color.white;
            ImgGUI.DrawTexture (Rect, Texture, scaleMode, true, 0.0f);
        } else {
            ImgGUI.Box (Rect, button.Image);
        }
    }

    // Visible for tests
    public SwrveButton GetButton()
    {
        return button;
    }
}
}
