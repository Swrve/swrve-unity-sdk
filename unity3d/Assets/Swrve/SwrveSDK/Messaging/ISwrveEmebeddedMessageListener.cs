using System;
using System.Collections.Generic;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Implement this interface to respond to emebedded messages being shown.
/// Config.SwrveEmbeddedMessageConfig.EmbeddedMessageListener = new YourEmebeddedMessageListener();
/// </summary>
public interface ISwrveEmbeddedMessageListener
{
    /// <summary>
    /// Called once per message being shown. Pause your app
    /// here if necessary.
    /// </summary>
    /// <param name="message">
    /// Embedded message information.
    /// </param>
    /// <param name="personalizationProperties">
    /// String dictionary containing any personalization found.
    /// </param>
    void OnMessage(SwrveEmbeddedMessage message, Dictionary<string, string> personalizationProperties);
}
}
