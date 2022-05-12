using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using SwrveUnity.Helpers;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// In-app message format.
    /// </summary>
    public class SwrveMessagePage
    {
        /// <summary>
        /// Collection of buttons inside the message.
        /// </summary>
        public List<SwrveButton> Buttons;

        /// <summary>
        /// Collection of images inside the message.
        /// </summary>
        public List<SwrveImage> Images;

        /// <summary>
        /// Parent in-app message.
        /// </summary>
        public SwrveMessage Message;

        /// <summary>
        /// Page Name.
        /// </summary>
        public string PageName;

        /// <summary>
        /// Identifies the page.
        /// </summary>
        public long PageId;

        /// <summary>
        /// PageId for swiping forward.
        /// </summary>
        public long SwipeForward;

        /// <summary>
        /// PageId for swiping backward.
        /// </summary>
        public long SwipeBackward;

        private SwrveMessagePage(SwrveMessage message)
        {
            this.Message = message;
            this.Buttons = new List<SwrveButton>();
            this.Images = new List<SwrveImage>();
        }

        /// <summary>
        /// Load an in-app message format from a JSON response.
        /// </summary>
        /// <param name="message">
        /// Parent in-app message.
        /// </param>
        /// <param name="pageData">
        /// JSON object with the individual message format data.
        /// </param>
        /// <returns>
        /// Parsed in-app message format.
        /// </returns>
        public static SwrveMessagePage LoadFromJSON(SwrveMessage message, Dictionary<string, object> pageData)
        {
            SwrveMessagePage page = new SwrveMessagePage(message);

            IList<object> jsonButtons = (List<object>)pageData["buttons"];
            for (int i = 0, j = jsonButtons.Count; i < j; i++)
            {
                SwrveButton button = LoadButtonFromJSON(message, (Dictionary<string, object>)jsonButtons[i]);
                page.Buttons.Add(button);
            }

            IList<object> jsonImages = (List<object>)pageData["images"];
            for (int ii = 0, ji = jsonImages.Count; ii < ji; ii++)
            {
                SwrveImage image = LoadImageFromJSON(message, (Dictionary<string, object>)jsonImages[ii]);
                page.Images.Add(image);
            }

            if (pageData.ContainsKey("page_name"))
            {
                page.PageName = (string)pageData["page_name"];
            }

            if (pageData.ContainsKey("page_id"))
            {
                page.PageId = (long)pageData["page_id"];
            }

            if (pageData.ContainsKey("swipe_forward"))
            {
                page.SwipeForward = (long)pageData["swipe_forward"];
            }

            if (pageData.ContainsKey("swipe_backward"))
            {
                page.SwipeBackward = (long)pageData["swipe_backward"];
            }

            return page;
        }

        protected static int IntValueFromAttribute(Dictionary<string, object> data, string attribute)
        {
            return MiniJsonHelper.GetInt(((Dictionary<string, object>)data[attribute]), "value");
        }

        protected static TextAlignment TextAlignmentFromAttribute(Dictionary<string, object> data, string attribute)
        {
            string alignmentString = (string)data[attribute];
            if (alignmentString.ToLower().Equals("right"))
            {
                return TextAlignment.Right;
            }

            if (alignmentString.ToLower().Equals("center"))
            {
                return TextAlignment.Center;
            }

            return TextAlignment.Left;
        }

        protected static string StringValueFromAttribute(Dictionary<string, object> data, string attribute)
        {
            return (string)(((Dictionary<string, object>)data[attribute])["value"]);
        }

        protected static SwrveButton LoadButtonFromJSON(SwrveMessage message, Dictionary<string, object> buttonData)
        {
            SwrveButton button = new SwrveButton();
            button.Position.X = IntValueFromAttribute(buttonData, "x");
            button.Position.Y = IntValueFromAttribute(buttonData, "y");

            button.Size.X = IntValueFromAttribute(buttonData, "w");
            button.Size.Y = IntValueFromAttribute(buttonData, "h");

            if (buttonData.ContainsKey("image_up"))
            {
                button.Image = StringValueFromAttribute(buttonData, "image_up");
            }

            if (buttonData.ContainsKey("dynamic_image_url"))
            {
                button.DynamicImageUrl = (string)buttonData["dynamic_image_url"];
            }

            if (buttonData.ContainsKey("text"))
            {
                button.Text = StringValueFromAttribute(buttonData, "text");
            }

            button.Message = message;

            if (buttonData.ContainsKey("name"))
            {
                button.Name = (string)buttonData["name"];
            }

            if (buttonData.ContainsKey("button_id"))
            {
                button.ButtonId = (long)buttonData["button_id"];
            }

            string actionTypeStr = StringValueFromAttribute(buttonData, "type");
            SwrveActionType actionType = SwrveActionType.Dismiss;
            if (actionTypeStr.ToLower().Equals("install"))
            {
                actionType = SwrveActionType.Install;
            }
            else if (actionTypeStr.ToLower().Equals("custom"))
            {
                actionType = SwrveActionType.Custom;
            }
            else if (actionTypeStr.ToLower().Equals("copy_to_clipboard"))
            {
                actionType = SwrveActionType.CopyToClipboard;
            }
            else if (actionTypeStr.ToLower().Equals("request_capability"))
            {
                actionType = SwrveActionType.Capability;
            }
            else if (actionTypeStr.ToLower().Equals("page_link"))
            {
                actionType = SwrveActionType.PageLink;
            }

            button.ActionType = actionType;
            button.Action = StringValueFromAttribute(buttonData, "action");
            if (button.ActionType == SwrveActionType.Install)
            {
                string appId = StringValueFromAttribute(buttonData, "game_id");
                if (appId != null && appId != string.Empty)
                {
                    button.AppId = int.Parse(appId);
                }
            }

            return button;
        }

        protected static SwrveImage LoadImageFromJSON(SwrveMessage message, Dictionary<string, object> imageData)
        {
            SwrveImage image = new SwrveImage();
            image.Position.X = IntValueFromAttribute(imageData, "x");
            image.Position.Y = IntValueFromAttribute(imageData, "y");

            image.Size.X = IntValueFromAttribute(imageData, "w");
            image.Size.Y = IntValueFromAttribute(imageData, "h");

            if (imageData.ContainsKey("image"))
            {
                image.File = StringValueFromAttribute(imageData, "image");
            }

            if (imageData.ContainsKey("dynamic_image_url"))
            {
                image.DynamicImageUrl = (string)imageData["dynamic_image_url"];
            }

            if (imageData.ContainsKey("text"))
            {
                image.Text = StringValueFromAttribute(imageData, "text");
            }

            if (imageData.ContainsKey("multiline_text"))
            {
                image.IsMultiLine = true;

                Dictionary<string, object> multiLineData = (Dictionary<string, object>)imageData["multiline_text"];

                if (multiLineData.ContainsKey("value"))
                {
                    image.Text = MiniJsonHelper.GetString(multiLineData, "value");
                }

                if (multiLineData.ContainsKey("font_size"))
                {
                    image.FontSize = MiniJsonHelper.GetFloat(multiLineData, "font_size");
                }

                if (multiLineData.ContainsKey("scrollable"))
                {
                    image.IsScrollable = (bool)multiLineData["scrollable"];
                }

                if (multiLineData.ContainsKey("h_align"))
                {
                    image.HorizontalAlignment = TextAlignmentFromAttribute(multiLineData, "h_align");
                }

                if (multiLineData.ContainsKey("font_postscript_name"))
                {
                    image.FontPostScriptName = MiniJsonHelper.GetString(multiLineData, "font_postscript_name");
                }

                if (multiLineData.ContainsKey("font_family"))
                {
                    image.FontFamily = MiniJsonHelper.GetString(multiLineData, "font_family");
                }

                if (multiLineData.ContainsKey("font_native_style"))
                {
                    image.FontStyle = MiniJsonHelper.GetString(multiLineData, "font_native_style");
                }

                if (multiLineData.ContainsKey("font_file"))
                {
                    image.FontFile = MiniJsonHelper.GetString(multiLineData, "font_file");
                }

                if (multiLineData.ContainsKey("line_height"))
                {
                    image.LineHeight = MiniJsonHelper.GetInt(multiLineData, "line_height");
                }

                if (multiLineData.ContainsKey("font_digest"))
                {
                    image.FontDigest = MiniJsonHelper.GetString(multiLineData, "font_digest");
                }

                if (multiLineData.ContainsKey("padding"))
                {
                    Dictionary<string, object> paddingData = (Dictionary<string, object>)multiLineData["padding"];

                    if (paddingData.ContainsKey("top"))
                        image.Padding.top = MiniJsonHelper.GetInt(paddingData, "top");
                    if (paddingData.ContainsKey("left"))
                        image.Padding.left = MiniJsonHelper.GetInt(paddingData, "left");
                    if (paddingData.ContainsKey("bottom"))
                        image.Padding.bottom = MiniJsonHelper.GetInt(paddingData, "bottom");
                    if (paddingData.ContainsKey("right"))
                        image.Padding.right = MiniJsonHelper.GetInt(paddingData, "right");
                }

                if (multiLineData.ContainsKey("font_file"))
                {
                    image.FontFile = MiniJsonHelper.GetString(multiLineData, "font_file");
                }

                if (multiLineData.ContainsKey("font_color"))
                {
                    image.FontColor = MiniJsonHelper.GetString(multiLineData, "font_color");
                }

                if (multiLineData.ContainsKey("bg_color"))
                {
                    image.BackgroundColor = MiniJsonHelper.GetString(multiLineData, "bg_color");
                }
            }

            image.Message = message;

            return image;
        }
    }
}
