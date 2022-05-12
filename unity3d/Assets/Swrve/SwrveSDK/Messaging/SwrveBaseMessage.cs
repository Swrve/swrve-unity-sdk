using UnityEngine;
using System.Collections;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Virtual super class of conversations and messages.
    /// </summary>
    public abstract class SwrveBaseMessage
    {

        /// <summary>
        /// Identifies the base message in a campaign.
        /// </summary>
        public int Id;

        /// <summary>
        /// Parent campaign.
        /// </summary>
        public SwrveBaseCampaign Campaign;

        /// <summary>
        /// Priority of the message.
        /// </summary>
        public int Priority = 9999;

        /// <summary>
        /// Check if the message supports the given orientation
        /// </summary>
        /// <returns>
        /// True if there is any format that supports the given orientation.
        /// </returns>
        public abstract bool SupportsOrientation(SwrveOrientation orientation);
    }
}
