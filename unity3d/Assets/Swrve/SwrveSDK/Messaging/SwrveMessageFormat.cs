using System.Collections;
using System.Collections.Generic;
using SwrveUnity.Helpers;
using UnityEngine;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// In-app message format.
    /// </summary>
    public class SwrveMessageFormat
    {
        /// <summary>
        /// Name of the format.
        /// </summary>
        public string Name;

        /// Language of the format.
        /// </summary>
        public string Language;

        /// <summary>
        /// Orientation of the format.
        /// </summary>
        public SwrveOrientation Orientation;

        /// <summary>
        /// Pages in the format.
        /// </summary>
        public Dictionary<long, SwrveMessagePage> Pages;

        /// <summary>
        /// Size of the format.
        /// </summary>
        public Point Size = new Point(0, 0);

        /// <summary>
        /// Parent in-app message.
        /// </summary>
        public SwrveMessage Message;

        /// <summary
        /// Text Calibration Object assoicated with multi-line (empty if not used)
        /// </summary>
        public SwrveMessageFormatCalibration Calibration;

        /// <summary>
        /// Scale set by the server for this device and format.
        /// </summary>
        public float Scale = 1f;

        /// <summary>
        /// Color of the background.
        /// </summary>
        public Color? BackgroundColor = null;

        /// <summary>
        /// Identifies the page id for the first page.
        /// </summary>
        public long FirstPageId;

        private SwrveMessageFormat(SwrveMessage message)
        {
            this.Message = message;
        }

        /// <summary>
        /// Load an in-app message format from a JSON response.
        /// </summary>
        /// <param name="message">
        /// Parent in-app message.
        /// </param>
        /// <param name="messageFormatData">
        /// JSON object with the individual message format data.
        /// </param>
        /// <returns>
        /// Parsed in-app message format.
        /// </returns>
        public static SwrveMessageFormat LoadFromJSON(SwrveMessage message, Dictionary<string, object> messageFormatData, Color? defaultBackgroundColor)
        {
            SwrveMessageFormat messageFormat = new SwrveMessageFormat(message);

            messageFormat.Name = (string)messageFormatData["name"];
            messageFormat.Language = (string)messageFormatData["language"];
            if (messageFormatData.ContainsKey("scale"))
            {
                messageFormat.Scale = MiniJsonHelper.GetFloat(messageFormatData, "scale", 1);
            }

            if (messageFormatData.ContainsKey("orientation"))
            {
                messageFormat.Orientation = SwrveOrientationHelper.Parse((string)messageFormatData["orientation"]);
            }

            messageFormat.BackgroundColor = defaultBackgroundColor;
            if (messageFormatData.ContainsKey("color"))
            {
                string strColor = (string)messageFormatData["color"];
                Color? c = messageFormat.BackgroundColor;
                if (strColor.Length == 8)
                {
                    // RRGGBB
                    byte a = byte.Parse(strColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte r = byte.Parse(strColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(strColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(strColor.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                    c = new Color32(r, g, b, a);
                }
                else if (strColor.Length == 6)
                {
                    // AARRGGBB
                    byte r = byte.Parse(strColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(strColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(strColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    c = new Color32(r, g, b, 255);
                }
                messageFormat.BackgroundColor = c;
            }

            if (messageFormatData.ContainsKey("calibration"))
            {
                Dictionary<string, object> calibrationData = (Dictionary<string, object>)messageFormatData["calibration"];
                messageFormat.Calibration = SwrveMessageFormatCalibration.LoadFromJSON(calibrationData);
            }

            Dictionary<string, object> sizeJson = (Dictionary<string, object>)messageFormatData["size"];
            messageFormat.Size.X = MiniJsonHelper.GetInt(((Dictionary<string, object>)sizeJson["w"]), "value");
            messageFormat.Size.Y = MiniJsonHelper.GetInt(((Dictionary<string, object>)sizeJson["h"]), "value");

            messageFormat.Pages = new Dictionary<long, SwrveMessagePage>();
            if (messageFormatData.ContainsKey("pages"))
            {
                IList<object> jsonPages = (List<object>)messageFormatData["pages"];
                for (int i = 0, j = jsonPages.Count; i < j; i++)
                {
                    Dictionary<string, object> pageData = (Dictionary<string, object>)jsonPages[i];
                    SwrveMessagePage page = SwrveMessagePage.LoadFromJSON(message, pageData);
                    messageFormat.Pages.Add(page.PageId, page);
                    if (i == 0)
                    {
                        messageFormat.FirstPageId = page.PageId; // the first page is the first element in the array
                    }
                }
            }
            else if (messageFormatData.ContainsKey("buttons") && messageFormatData.ContainsKey("images"))
            {
                // for backward compatibility, convert old IAM's into a single page Dictionary
                SwrveMessagePage page = SwrveMessagePage.LoadFromJSON(message, messageFormatData);
                messageFormat.Pages.Add(0, page);
                messageFormat.FirstPageId = 0;
            }

            return messageFormat;
        }
    }
}
