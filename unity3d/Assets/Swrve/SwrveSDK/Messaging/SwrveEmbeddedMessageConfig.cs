using System;
using SwrveUnity.Messaging;
using UnityEngine;

namespace SwrveUnity
{
    /// <summary>
    /// Configuration for the Swrve SDK embedded messages.
    /// </summary>
    public class SwrveEmbeddedMessageConfig
    {
        /// <summary>
        /// Embedded message listener.
        /// </summary>
        public ISwrveEmbeddedMessageListener EmbeddedMessageListener = null;

        public SwrveEmbeddedMessageConfig()
        {

        }

        public SwrveEmbeddedMessageConfig(ISwrveEmbeddedMessageListener embeddedMessageListener)
        {
            EmbeddedMessageListener = embeddedMessageListener;
        }
    }
}
