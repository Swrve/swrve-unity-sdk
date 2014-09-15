using System;

namespace Swrve.Messaging
{
/// <summary>
/// Implement this interface to handle callbacks of install buttons
/// inside your in-app messages.
/// </summary>
public interface ISwrveInstallButtonListener
{
    /// <summary>
    /// This method is invoked when an install button has been pressed on an in-app message.
    /// </summary>
    /// <param name="gameAppStoreUrl">
    /// App store install link specified for the game.
    /// </param>
    /// <returns>
    /// Returning false stops the normal flow of event processing
    /// to enable custom logic. Return true otherwise.
    /// </returns>
    bool OnAction (string gameAppStoreUrl);
}
}
