﻿using System;
using System.Collections.Generic;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Implement this interface to respond to emebedded messages being shown.
    /// Config.SwrveEmbeddedMessageConfig.EmbeddedMessageListener = new YourEmebeddedMessageListener();
    /// </summary>
    [Obsolete("This interface is deprecated, please use ISwrveEmbeddedMessageControlListener instead.")]
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
        [Obsolete("This method is deprecated, please use OnMessage(SwrveEmbeddedMessage message, Dictionary<string, string> personalizationProperties, bool isControl) instead.")]
        void OnMessage(SwrveEmbeddedMessage message, Dictionary<string, string> personalizationProperties);
    }
}
