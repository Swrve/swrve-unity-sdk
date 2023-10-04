using System;
using System.Collections.Generic;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Implement this interface to respond to emebedded messages being shown.
    /// Config.SwrveEmbeddedMessageConfig.EmbeddedListener = new YourEmebeddedMessageListener();
    /// </summary>
    public interface ISwrveEmbeddedListener
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
        /// <param name="isControl">
        /// Bool determining if message is a control or treatment.
        /// </param>
        void OnMessage(SwrveEmbeddedMessage message, Dictionary<string, string> personalizationProperties, bool isControl);
    }
}