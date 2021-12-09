using System;
using UnityEngine;
using SwrveUnity.Helpers;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Used internally to render in-app message personalized text using Unity IMGUI.
    /// </summary>
    public class SwrveTextWidgetView : SwrveWidgetView, ISwrveButtonView
    {
        protected readonly SwrveWidget widget;
        protected readonly SwrveTextViewStyle textViewStyle;
        protected readonly SwrveMessageFormatCalibration calibration;

        private bool Pressed = false;

        /// <summary>
        /// Pointer bounds.
        /// </summary>
        public Rect PointerRect;

        // Visible for tests

        public GUIContent content;
        public GUIStyle style;
        public SwrveButton button;
        public bool isButton;
        public Vector2 scrollPosition = Vector2.zero;
        public Color backgroundColor;
        protected Color clickTintColor;

        public SwrveTextWidgetView(SwrveWidget widget, string resolvedText, SwrveInAppMessageConfig inAppConfig, SwrveTextViewStyle textViewStyle, SwrveMessageFormatCalibration calibration)
        {
            this.widget = widget;
            this.textViewStyle = textViewStyle;
            this.calibration = calibration;
            content = new GUIContent(resolvedText);
            style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = textViewStyle.TextForegroundColor;
            style.font = textViewStyle.TextFont;

            if (widget.IsMultiLine)
            {
                style.wordWrap = true;
                SetTextAlignment();
                int relativeFontSize = SwrveHelper.CalibrateRelativeFontSizeToPlatform(calibration, textViewStyle.FontSize);
                style.fontSize = relativeFontSize;

                if (!widget.IsScrollable)
                {
                    FitMultiLineSizeToImage(widget.Size.X, widget.Size.Y, relativeFontSize);
                }
            }
            backgroundColor = textViewStyle.TextBackgroundColor;
            clickTintColor = inAppConfig.ButtonClickTintColor;

            isButton = (widget is SwrveButton);
            if (isButton)
            {
                button = (SwrveButton)widget;
            }
        }

        public override string GetTexturePath()
        {
            if (isButton)
            {
                return this.button.Image;
            }
            else
            {
                return ((SwrveImage)widget).File;
            }
        }

        public override void SetTexture(Texture2D texture)
        {
            this.Texture = texture;

            if (Texture != null)
            {
                style.fontSize = SwrveHelper.GetTextSizeToFitImage(style, content.text, Texture.width, Texture.height);
            }
        }

        public void ProcessButtonDown(Vector3 mousePosition)
        {
            if (isButton && PointerRect.Contains(mousePosition))
            {
                Pressed = true;
            }
        }

        public SwrveButtonClickResult ProcessButtonUp(Vector3 mousePosition, SwrveMessageTextTemplatingResolver templatingResolver)
        {
            SwrveButtonClickResult clickResult = null;
            if (isButton && PointerRect.Contains(mousePosition) && Pressed)
            {
                string resolvedAction = templatingResolver.ActionResolution[button];
                clickResult = new SwrveButtonClickResult(button, resolvedAction);
            }

            Pressed = false;

            return clickResult;
        }

        private void FitMultiLineSizeToImage(int maxWidth, int maxHeight, int relativeFontSize)
        {
            int currentFontSize = relativeFontSize;
            float contentHeight = style.CalcHeight(content, maxWidth);

            if (contentHeight > maxHeight)
            {
                // due to a scaling issues with CalcHeight in Unity
                currentFontSize = 1;
                style.fontSize = currentFontSize;
                contentHeight = style.CalcHeight(content, maxWidth);

                while ((contentHeight < maxHeight && currentFontSize < 130))
                {
                    currentFontSize++;
                    style.fontSize = currentFontSize;
                    contentHeight = style.CalcHeight(content, maxWidth);
                }

                // remove one as we set it as it went over the loop
                style.fontSize = currentFontSize - 1;
            }
        }

        private void SetTextAlignment()
        {
            switch (textViewStyle.HorizontalAlignment)
            {
                case TextAlignment.Left:
                    style.alignment = TextAnchor.UpperLeft;
                    break;
                case TextAlignment.Center:
                    style.alignment = TextAnchor.UpperCenter;
                    break;
                case TextAlignment.Right:
                    style.alignment = TextAnchor.UpperRight;
                    break;
                default:
                    style.alignment = TextAnchor.UpperLeft;
                    break;
            }
        }

        public override void Render(float scale, int centerx, int centery, bool rotatedFormat)
        {
            if (Texture != null)
            {
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

                if (isButton)
                {
                    if (rotatedFormat)
                    {
                        // Rotate 90 degrees the hit area
                        Point widgetCenter = button.GetCenter(textureWidth, textureHeight, computedSize);
                        PointerRect.x = centerx - (widget.Position.Y * scale) + widgetCenter.Y;
                        PointerRect.y = centery + (widget.Position.X * scale) + widgetCenter.X;
                        PointerRect.width = Rect.height;
                        PointerRect.height = Rect.width;
                    }
                    else
                    {
                        PointerRect = Rect;
                    }
                }
                ImgGUI.color = (Pressed) ? backgroundColor * clickTintColor : backgroundColor;
                ImgGUI.DrawTexture(Rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0.0f);

                ImgGUI.color = (Pressed) ? Color.white * clickTintColor : Color.white;
                ImgGUI.Label(Rect, content, style);

            }
            else if (widget.IsMultiLine)
            {
                int textureWidth = widget.Size.X;
                int textureHeight = widget.Size.Y;

                float computedSize = scale;
                Point centerPoint = widget.GetCenteredPosition(textureWidth, textureHeight, computedSize, scale);
                centerPoint.X += centerx;
                centerPoint.Y += centery;
                Rect.x = centerPoint.X;
                Rect.y = centerPoint.Y;
                Rect.width = textureWidth * computedSize;
                Rect.height = textureHeight * computedSize;

                
                ImgGUI.color = (Pressed) ? backgroundColor * clickTintColor : backgroundColor;
                ImgGUI.DrawTexture(Rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0.0f);
                ImgGUI.color = (Pressed) ? Color.white * clickTintColor : Color.white;

                // calculate how high the content is after generation, if the content height is too large, make it scrollable
                float textHeight = style.CalcHeight(content, Rect.width);
                // we need to know the size of the text we're passing through so calculate the offset of the text for scrolling with the same style in mind
                float offset = style.CalcHeight(new GUIContent("I"), Rect.width);

                if (widget.IsScrollable && textHeight > Rect.height)
                {
                    // Add padding to account for the scrollbar size
                    RectOffset rectOffset = new RectOffset(0, 30, 0, 0);
                    style.padding = rectOffset;

                    scrollPosition = ImgGUI.ClickEventScrollView(Rect, scrollPosition, new Rect(Rect.x, Rect.y, Rect.width - offset, textHeight + offset), false, false);
                    ImgGUI.Label(Rect, content, style);
                    ImgGUI.EndScrollView();
                }
                else
                {
                    ImgGUI.Label(Rect, content, style);
                }

            }
        }

        // Visible for tests
        public SwrveButton GetButton()
        {
            return button;
        }
    }
}
