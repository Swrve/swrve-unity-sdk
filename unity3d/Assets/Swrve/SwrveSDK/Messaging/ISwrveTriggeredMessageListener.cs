using System;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Implement this to use your own rendering code for in-app messages.
    /// Will disable the Swrve implementation.
    /// </summary>
    [Obsolete("Use embedded campaigns instead.")]
    public interface ISwrveTriggeredMessageListener
    {
        /// <summary>
        /// Called once per message being shown. Pause your app
        /// here if necessary.
        /// </summary>
        /// <param name="message">
        /// Message to be rendered. Contains multiple formats.
        /// </param>
        [Obsolete("Use embedded campaigns instead.")]
        void OnMessageTriggered(SwrveMessage message);

        /// <summary>
        /// The current message has to be dismissed.
        /// </summary>
        [Obsolete("Use embedded campaigns instead.")]
        void DismissCurrentMessage();
    }
}
