using UnityEngine;
using System.Collections.Generic;
using System;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Swrve SwrveEmbeddedCampaign message campaign.
    /// </summary>
    public class SwrveEmbeddedCampaign : SwrveBaseCampaign
    {
        public SwrveEmbeddedMessage Message = null;

        private SwrveEmbeddedCampaign(DateTime initialisedTime) : base(initialisedTime)
        {
            this.Message = null;
        }

        public static SwrveEmbeddedCampaign LoadFromJSON(Dictionary<string, object> campaignData, DateTime initialisedTime, List<SwrveQaUserCampaignInfo> qaUserCampaignInfoList)
        {
            SwrveEmbeddedCampaign campaign = new SwrveEmbeddedCampaign(initialisedTime);
            campaign.Message = SwrveEmbeddedMessage.LoadFromJSON(campaign, (Dictionary<string, object>)campaignData["embedded_message"]);
            return campaign;
        }

        public SwrveEmbeddedMessage GetMessageForEvent(string triggerEvent, IDictionary<string, string> payload, List<SwrveQaUserCampaignInfo> qaCampaignInfoList)
        {
            if (Message == null)
            {
                string reason = "No embedded message in campaign " + Id;
                LogAndAddReason(reason, false, qaCampaignInfoList);
                return null;
            }

            if (CheckCampaignLimits(triggerEvent, payload, qaCampaignInfoList))
            {
                SwrveLog.Log(string.Format("[{0}] {1} matches a trigger in {2}", this, triggerEvent, Id));
                return Message;
            }
            return null;
        }

        #region SwrveBaseCampaign Abstract Methods implementation
        public override bool AreAssetsReady(Dictionary<string, string> personalizationProperties)
        {
            return (this.Message.data != null);
        }

        public override bool SupportsOrientation(SwrveOrientation orientation)
        {
            // there is no orientation defined as part of embedded so this is purely up to the developer whether or not to display it
            return true;
        }

        public override SwrveQaUserCampaignInfo.SwrveCampaignType GetCampaignType()
        {
            return SwrveQaUserCampaignInfo.SwrveCampaignType.Embedded;
        }

        #endregion
    }
}
