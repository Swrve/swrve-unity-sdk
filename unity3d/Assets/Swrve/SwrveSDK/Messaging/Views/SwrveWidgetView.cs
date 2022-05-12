using System;
using UnityEngine;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Used internally to render in-app message widgets using Unity IMGUI.
    /// </summary>
    public abstract class SwrveWidgetView
    {
        /// <summary>
        /// Loaded texture asset. This comes from PreloadFormatAssets.
        /// </summary>
        protected Texture2D Texture;


        /// <summary>
        /// Sha1 asset name for an dynamic image view
        /// </summary>
        protected string DynamicImageSha1Asset;


        /// <summary>
        /// Redering bounds.
        /// </summary>
        public Rect Rect;

        // Set texture while pre-loading the format.
        public abstract string GetTexturePath();

        // Set texture while pre-loading the format.
        public abstract void SetTexture(Texture2D texture);

        public abstract void Render(float scale, int centerx, int centery, bool rotatedFormat);

        public void Unload()
        {
            if (Texture != null)
            {
                Texture2D.Destroy(Texture);
                Texture = null;
            }
        }
    }
}
