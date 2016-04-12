using System;

namespace Swrve.Messaging
{
/// <summary>
/// Used for device orientation and specifying orientation filters.
/// </summary>
public enum SwrveOrientation {
    Portrait,
    Landscape,
    Both,
    Either
}

public static class SwrveOrientationHelper
{
    /// <summary>
    /// Convert from String to SwrveOrientation.
    /// </summary>
    /// <param name="orientation">
    /// Device orientation or filter.
    /// </param>
    /// <returns>
    /// Parsed orientation.
    /// </returns>
    public static SwrveOrientation Parse (string orientation)
    {
        if (orientation.ToLower ().Equals ("portrait")) {
                return SwrveOrientation.Portrait;
            } else if (orientation.ToLower ().Equals ("both")) {
                return SwrveOrientation.Both;
            } else if (orientation.ToLower ().Equals ("either")) {
                return SwrveOrientation.Either;
            }

        return SwrveOrientation.Landscape;
    }
}
}

