using System;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Implement this interface to respond to messages being
/// shown, rendered or dismissed:
/// SwrveSDK.GlobalMessageListener = new YourMessageListener();
/// </summary>
public interface ISwrveMessageListener
{
    /// <summary>
    /// Called once per message being shown. Pause your app
    /// here if necessary.
    /// </summary>
    /// <param name="format">
    /// In-app message information.
    /// </param>
    void OnShow (SwrveMessageFormat format);

    /// <summary>
    /// Called every frame a message is being displayed.
    /// </summary>
    /// <param name="format">
    /// In-app message information.
    /// </param>
    void OnShowing (SwrveMessageFormat format);

    /// <summary>
    /// Called when the message has been dismissed. Resume
    /// your app here if necessary.
    /// </summary>
    /// <param name="format">
    /// In-app message information.
    /// </param>
    void OnDismiss (SwrveMessageFormat format);
}
}
