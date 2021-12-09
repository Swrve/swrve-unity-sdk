using System;
using System.Collections.Generic;
using SwrveUnity.Helpers;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Calibration object used for formatting / resizing text provided in objects
    /// </summary>
    public class SwrveMessageFormatCalibration
    {

        /// <summary>
        /// bounding box width
        /// </summary>
        public int Width;

        /// <summary>
        /// bounding box height
        /// </summary>
        public int Height;

        /// <summary>
        /// reference text for calibration formatting.
        /// </summary>
        public string Text;

        /// <summary>
        /// base font size provided
        /// </summary>
        public int BaseFontSize;

        public static SwrveMessageFormatCalibration LoadFromJSON(Dictionary<string, object> calibrationData)
        {
            SwrveMessageFormatCalibration calibration = new SwrveMessageFormatCalibration();

            if (calibrationData.ContainsKey("width"))
            {
                calibration.Width = MiniJsonHelper.GetInt(calibrationData, "width");
            }

            if (calibrationData.ContainsKey("height"))
            {
                calibration.Height = MiniJsonHelper.GetInt(calibrationData, "height");
            }

            if (calibrationData.ContainsKey("text"))
            {
                calibration.Text = (string)calibrationData["text"];
            }

            if (calibrationData.ContainsKey("base_font_size"))
            {
                calibration.BaseFontSize = MiniJsonHelper.GetInt(calibrationData, "base_font_size");
            }

            return calibration;
        }
    }
}