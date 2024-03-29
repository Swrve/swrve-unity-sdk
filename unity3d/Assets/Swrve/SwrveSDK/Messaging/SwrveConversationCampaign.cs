using System.Collections.Generic;
using System;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Swrve conversation campaign.
    /// </summary>
    public class SwrveConversationCampaign : SwrveBaseCampaign
    {
        /// <summary>
        /// The Swrve Conversation associated with this campaign.
        /// </summary>
        public SwrveConversation Conversation;
        private SwrveConversationCampaign(DateTime initialisedTime) : base(initialisedTime)
        {
        }

        /// <summary>
        /// Search for a conversation related to the given trigger event at the given
        /// time. This function will return null if too many conversations were dismissed,
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
        /// SwrveConversation that contains the given event in its trigger list and satisfies all the
        /// rules.
        /// </returns>
        public SwrveConversation GetConversationForEvent(string triggerEvent, IDictionary<string, string> payload, List<SwrveQaUserCampaignInfo> qaCampaignInfoList)
        {
            if (null == Conversation)
            {
                string reason = "No conversation in campaign " + Id;
                LogAndAddReason(reason, false, qaCampaignInfoList);
                return null;
            }

            if (CheckCampaignLimits(triggerEvent, payload, qaCampaignInfoList))
            {
                SwrveLog.Log(string.Format("[{0}] {1} matches a trigger in {2}", this, triggerEvent, Id));
                if (AreAssetsReady(null))
                {
                    return Conversation;
                }
                else
                {
                    string reason = "Assets not downloaded to show conversation in campaign " + Id;
                    LogAndAddReason(reason, false, qaCampaignInfoList);
                }
            }
            return null;
        }

        public static SwrveConversationCampaign LoadFromJSON(ISwrveAssetsManager swrveAssetsManager, Dictionary<string, object> campaignData, int campaignId, DateTime initialisedTime)
        {
            SwrveConversationCampaign campaign = new SwrveConversationCampaign(initialisedTime);
            campaign.Conversation = SwrveConversation.LoadFromJSON(swrveAssetsManager, campaign, (Dictionary<string, object>)campaignData["conversation"]);
            return campaign;
        }

        #region SwrveBaseCampaign Abstract Methods implementation
        public override bool AreAssetsReady(Dictionary<string, string> personalizationProperties)
        {
            return this.Conversation.AreAssetsReady();
        }

        public override bool SupportsOrientation(SwrveOrientation orientation)
        {
            return true;
        }

        public override SwrveQaUserCampaignInfo.SwrveCampaignType GetCampaignType()
        {
            return SwrveQaUserCampaignInfo.SwrveCampaignType.Conversation;
        }

        #endregion
    }
}
