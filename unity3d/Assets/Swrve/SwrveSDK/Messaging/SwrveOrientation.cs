using System;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Used for device orientation and specifying orientation filters.
    /// </summary>
    public enum SwrveOrientation
    {
        Portrait,
        Landscape,
        Both
    }

    public static class SwrveOrientationHelper
    {
        const string PORTRAIT_KEY = "portrait";
        const string LANDSCAPE_KEY = "landscape";
        const string BOTH_KEY = "both";

        /// <summary>
        /// Convert from String to SwrveOrientation.
        /// </summary>
        /// <param name="orientation">
        /// Device orientation or filter.
        /// </param>
        /// <returns>
        /// Parsed orientation.
        /// </returns>
        public static SwrveOrientation Parse(string orientation)
        {
            if (orientation.ToLower().Equals(PORTRAIT_KEY))
            {
                return SwrveOrientation.Portrait;
            }
            else if (orientation.ToLower().Equals(BOTH_KEY))
            {
                return SwrveOrientation.Both;
            }

            return SwrveOrientation.Landscape;
        }
    }
}

