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
        [Obsolete("This property is deprecated, please use EmbeddedListener instead.")]
        public ISwrveEmbeddedMessageListener EmbeddedMessageListener = null;

        /// <summary>
        /// Embedded message listener.
        /// </summary>
        public ISwrveEmbeddedListener EmbeddedListener = null;

        public SwrveEmbeddedMessageConfig()
        {

        }

        [Obsolete("This constructor is deprecated, please use SwrveEmbeddedMessageConfig(ISwrveEmbeddedListener embeddedListener) instead.")]
        public SwrveEmbeddedMessageConfig(ISwrveEmbeddedMessageListener embeddedMessageListener)
        {
            EmbeddedMessageListener = embeddedMessageListener;
        }

        public SwrveEmbeddedMessageConfig(ISwrveEmbeddedListener embeddedListener)
        {
            EmbeddedListener = embeddedListener;
        }
    }
}
