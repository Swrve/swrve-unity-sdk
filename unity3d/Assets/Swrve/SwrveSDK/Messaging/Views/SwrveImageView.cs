using System;
using UnityEngine;

namespace SwrveUnity.Messaging
{
#pragma warning disable 0618
/// <summary>
/// Used internally to render in-app message images using Unity IMGUI.
/// </summary>
public class SwrveImageView : SwrveWidgetView
{
    protected readonly SwrveImage image;

    public SwrveImageView(SwrveImage image)
    {
        this.image = image;
    }

    public void Render(float scale, int centerx, int centery, bool rotatedFormat, ISwrveMessageAnimator animator)
    {
        GUI.color = Color.white;
        if (image.Texture != null) {
            int textureWidth = image.Texture.width;
            int textureHeight = image.Texture.height;

            float computedSize = scale * image.AnimationScale;
            Point centerPoint = image.GetCenteredPosition (textureWidth, textureHeight, computedSize, scale);
            centerPoint.X += centerx;
            centerPoint.Y += centery;
            image.Rect.x = centerPoint.X;
            image.Rect.y = centerPoint.Y;
            image.Rect.width = textureWidth * computedSize;
            image.Rect.height = textureHeight * computedSize;
            GUI.DrawTexture (image.Rect, image.Texture, ScaleMode.StretchToFill, true, 0.0f);
        } else {
            GUI.Box (image.Rect, image.File);
        }
    }
}
#pragma warning restore 0618
}
