using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using SwrveUnity.Helpers;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Swrve SwrveInAppCampaign message campaign.
    /// </summary>
    public class SwrveInAppCampaign : SwrveBaseCampaign
    {
        /// <summary>
        /// Message contained in the campaign.
        /// </summary>
        public SwrveMessage Message;

        private SwrveInAppCampaign(DateTime initialisedTime) : base(initialisedTime)
        {
        }

        /// <summary>
        /// Search for a message related to the given trigger event at the given
        /// time. This function will return null if too many messages were dismissed,
        /// the campaign start is in the future, the campaign end is in the past or
        /// the given event is not contained in the trigger set.
        /// </summary>
        /// <param name="triggerEvent">
        /// Event triggered. Must be a trigger for the campaign.
        /// </param>
        /// <param name="campaignReasons">
        /// At the exit of the function will include the reasons why a campaign the campaigns
        /// in memory were not shown or chosen.
        /// </param>
        /// <returns>
        /// In-app message that contains the given event in its trigger list and satisfies all the
        /// rules.
        /// </returns>
        public SwrveMessage GetMessageForEvent(string triggerEvent, IDictionary<string, string> payload, List<SwrveQaUserCampaignInfo> qaCampaignInfoList, Dictionary<string, string> personalizationProperties)
        {
            if (Message == null)
            {
                string reason = "No message in campaign " + Id;
                LogAndAddReason(reason, false, qaCampaignInfoList);
                return null;
            }

            if (CheckCampaignLimits(triggerEvent, payload, qaCampaignInfoList))
            {
                SwrveLog.Log(string.Format("[{0}] {1} matches a trigger in {2}", this, triggerEvent, Id));

                return GetNextMessage(qaCampaignInfoList, personalizationProperties);
            }
            return null;
        }

        protected SwrveMessage GetNextMessage(List<SwrveQaUserCampaignInfo> qaCampaignInfoList, Dictionary<string, string> personalizationProperties)
        {
            if (Message.IsDownloaded(personalizationProperties))
            {
                return Message;
            }

            string reason = "Campaign " + this.Id + " hasn't finished downloading.";
            LogAndAddReason(reason, false, qaCampaignInfoList);
            return null;
        }

        /// <summary>
        /// Get all the assets in the in-app campaign message.
        /// </summary>
        /// <returns>
        /// All the assets in the in-app campaign.
        /// </returns>
        public HashSet<SwrveAssetsQueueItem> GetImageAssets(Dictionary<string, string> personalizationProperties)
        {
            HashSet<SwrveAssetsQueueItem> assetsQueueImages = new HashSet<SwrveAssetsQueueItem>();
            assetsQueueImages.UnionWith(Message.SetOfAssets(personalizationProperties));
            return assetsQueueImages;
        }

        /// <summary>
        /// Notify that the message was shown to the user. This function
        /// has to be called only once when the message is displayed to
        /// the user.
        /// This is automatically called by the SDK and will only need
        /// to be manually called if you are implementing your own
        /// in-app message rendering code.
        /// </summary>
        public void MessageWasShownToUser(SwrveMessageFormat messageFormat)
        {
            base.WasShownToUser();
        }

        public static SwrveInAppCampaign LoadFromJSON(ISwrveAssetsManager swrveAssetsManager, Dictionary<string, object> campaignData, int id, DateTime initialisedTime, Color? defaultBackgroundColor, List<SwrveQaUserCampaignInfo> qaUserCampaignInfoList)
        {
            SwrveInAppCampaign campaign = new SwrveInAppCampaign(initialisedTime);

            object _message = null;
            campaignData.TryGetValue("message", out _message);

            if (_message == null)
            {
                string reason = "Campaign [" + id + "] JSON message is null, skipping.";
                campaign.LogAndAddReason(reason, false, qaUserCampaignInfoList);
                return null;
            }

            Dictionary<string, object> messageData = (Dictionary<string, object>)_message;
            SwrveMessage message = SwrveMessage.LoadFromJSON(swrveAssetsManager, campaign, messageData, defaultBackgroundColor);
            if (message.Formats.Count > 0)
            {
                campaign.Message = message;
            }

            if (campaign.Message == null)
            {
                string reason = "Campaign [" + id + "] no message found, skipping.";
                campaign.LogAndAddReason(reason, false, qaUserCampaignInfoList);
            }

            if (!campaign.Message.IsSupportedBySDK())
            {
                string reason = "Campaign [" + id + "] is not currently supported by SDK, skipping.";
                campaign.LogAndAddReason(reason, false, qaUserCampaignInfoList);
                campaign = null;
            }

            return campaign;
        }

        #region SwrveBaseCampaign Abstract Methods implementation
        public override bool AreAssetsReady(Dictionary<string, string> personalizationProperties)
        {
            return this.Message.IsDownloaded(personalizationProperties);
        }

        public override bool SupportsOrientation(SwrveOrientation orientation)
        {
            return this.Message.SupportsOrientation(orientation);
        }

        public override SwrveQaUserCampaignInfo.SwrveCampaignType GetCampaignType()
        {
            return SwrveQaUserCampaignInfo.SwrveCampaignType.Iam;
        }

        #endregion

    }
}
