using System;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Implement this to use your own rendering code for in-app messages.
/// Will disable the Swrve implementation.
/// </summary>
public interface ISwrveTriggeredMessageListener
{
    /// <summary>
    /// Called once per message being shown. Pause your game
    /// here if necessary.
    /// </summary>
    /// <param name="message">
    /// Message to be rendered. Contains multiple formats.
    /// </param>
    void OnMessageTriggered (SwrveMessage message);

    /// <summary>
    /// The current message has to be dismissed.
    /// </summary>
    void DismissCurrentMessage ();
}
}

