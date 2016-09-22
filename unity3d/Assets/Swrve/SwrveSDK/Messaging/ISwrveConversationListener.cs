using System;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Implement this interface to respond to conversation being
/// shown, or dismissed:
/// SwrveSDK.GlobalConversationListener = new YourMessageListener();
/// </summary>
public interface ISwrveConversationListener
{
    /// <summary>
    /// Called once per conversation being shown. Pause your app
    /// here if necessary.
    /// </summary>
    void OnShow ();

    /// <summary>
    /// Called when the conversation has been dismissed. Resume
    /// your app here if necessary.
    /// </summary>
    void OnDismiss ();
}
}
