using System;

namespace Swrve.Messaging
{
/// <summary>
/// Implement this interface to handle custom deep-links in your app as result
/// of an in-app custom button.
/// </summary>
public interface ISwrveCustomButtonListener
{
    /// <summary>
    /// This method is invoked when a custom button has been pressed on an in-app message.
    /// </summary>
    /// <param name="customAction">
    /// Custom action of button that was pressed.
    /// </param>
    void OnAction (string customAction);
}
}
