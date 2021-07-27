using System;
using UnityEngine;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Used internally to render in-app message images using Unity IMGUI.
/// </summary>
public class SwrveImageView : SwrveWidgetView
{
    protected readonly SwrveImage image;
    public SwrveImageView(SwrveImage image, string dynamicImageSha1Asset)
    {
        this.image = image;
        DynamicImageSha1Asset = dynamicImageSha1Asset;
    }

    public override string GetTexturePath()
    {
        if (DynamicImageSha1Asset != null) {
            return DynamicImageSha1Asset;
        } else {
            // if sha1Asset is populated, then use that instead of TexturePath()
            return this.image.File;
        }
    }

    public override void SetTexture(Texture2D texture)
    {
        this.Texture = texture;
    }

    public override void Render(float scale, int centerx, int centery, bool rotatedFormat)
    {
        ImgGUI.color = Color.white;
        if (Texture != null) {
            int textureWidth = Texture.width;
            int textureHeight = Texture.height;
            var scaleMode = ScaleMode.StretchToFill;

            if (string.IsNullOrEmpty(DynamicImageSha1Asset) == false) {
                textureWidth = image.Size.X;
                textureHeight = image.Size.Y;
                scaleMode = ScaleMode.ScaleToFit;
            }

            float computedSize = scale;
            Point centerPoint = image.GetCenteredPosition(textureWidth, textureHeight, computedSize, scale);
            centerPoint.X += centerx;
            centerPoint.Y += centery;
            Rect.x = centerPoint.X;
            Rect.y = centerPoint.Y;
            Rect.width = textureWidth * computedSize;
            Rect.height = textureHeight * computedSize;
            ImgGUI.DrawTexture (Rect, Texture, scaleMode, true, 0.0f);
        } else {
            ImgGUI.Box (Rect, image.File);
        }
    }
}
}
