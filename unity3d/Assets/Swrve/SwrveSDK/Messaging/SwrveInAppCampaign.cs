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
    /// List of messages contained in the campaign.
    /// </summary>
    public List<SwrveMessage> Messages;

    private SwrveInAppCampaign (DateTime initialisedTime) : base (initialisedTime)
    {
        this.Messages = new List<SwrveMessage> ();
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
    public SwrveMessage GetMessageForEvent (string triggerEvent, IDictionary<string, string> payload, List<SwrveQaUserCampaignInfo> qaCampaignInfoList)
    {
        int messagesCount = Messages.Count;

        if (messagesCount == 0) {
            string reason = "No messages in campaign " + Id;
            LogAndAddReason (reason, false, qaCampaignInfoList);
            return null;
        }

        if (CheckCampaignLimits (triggerEvent, payload, qaCampaignInfoList)) {
            SwrveLog.Log (string.Format ("[{0}] {1} matches a trigger in {2}", this, triggerEvent, Id));

            return GetNextMessage (messagesCount, qaCampaignInfoList);
        }
        return null;
    }

    /// <summary>
    /// Get a message by its identifier.
    /// </summary>
    /// <returns>
    /// The message with the given identifier if it could be found.
    /// </returns>
    public SwrveMessage GetMessageForId (int id)
    {
        for(int mi = 0; mi < Messages.Count; mi++) {
            SwrveMessage message = Messages[mi];
            if (message.Id == id) {
                return message;
            }
        }

        return null;
    }

    protected SwrveMessage GetNextMessage (int messagesCount, List<SwrveQaUserCampaignInfo> qaCampaignInfoList)
    {
        if (RandomOrder) {
            List<SwrveMessage> randomMessages = new List<SwrveMessage> (Messages);
            randomMessages.Shuffle ();
            for(int mi = 0; mi < randomMessages.Count; mi++) {
                SwrveMessage message = randomMessages[mi];
                if (message.IsDownloaded ()) {
                    return message;
                }
            }
        } else if (Next < messagesCount) {
            SwrveMessage message = Messages [Next];
            if (message.IsDownloaded ()) {
                return message;
            }
        }

        string reason = "Campaign " + this.Id + " hasn't finished downloading.";
        LogAndAddReason (reason, false, qaCampaignInfoList);
        return null;
    }

    protected void AddMessage (SwrveMessage message)
    {
        this.Messages.Add (message);
    }

    /// <summary>
    /// Get all the assets in the in-app campaign messages.
    /// </summary>
    /// <returns>
    /// All the assets in the in-app campaign.
    /// </returns>
    public HashSet<SwrveAssetsQueueItem> GetImageAssets ()
    {
        HashSet<SwrveAssetsQueueItem> assetsQueueImages = new HashSet<SwrveAssetsQueueItem>();
        for(int mi = 0; mi < Messages.Count; mi++) {
            SwrveMessage message = Messages[mi];
            assetsQueueImages.UnionWith(message.SetOfAssets());
        }
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
    public void MessageWasShownToUser (SwrveMessageFormat messageFormat)
    {
        base.WasShownToUser ();

        if (Messages.Count > 0) {
            if (!RandomOrder) {
                int nextMessage = (Next + 1) % Messages.Count;
                Next = nextMessage;
                SwrveLog.Log ("Round Robin: Next message in campaign " + Id + " is " + nextMessage);
            } else {
                SwrveLog.Log ("Next message in campaign " + Id + " is random");
            }
        }
    }

    public static SwrveInAppCampaign LoadFromJSON (ISwrveAssetsManager swrveAssetsManager, Dictionary<string, object> campaignData, int id, DateTime initialisedTime, Color? defaultBackgroundColor, List<SwrveQaUserCampaignInfo> qaUserCampaignInfoList)
    {
        SwrveInAppCampaign campaign = new SwrveInAppCampaign (initialisedTime);

        object _messages = null;
        campaignData.TryGetValue ("messages", out _messages);
        IList<object> messages = null;
        try {
            messages = (IList<object>)_messages;
        } catch(Exception e) {
            string reason = "Campaign [" + id + "] invalid messages found, skipping.  Error: " + e;
            campaign.LogAndAddReason(reason, false, qaUserCampaignInfoList);
        }

        if (messages == null) {
            string reason = "Campaign [" + id + "] JSON messages are null, skipping.";
            campaign.LogAndAddReason(reason, false, qaUserCampaignInfoList);
            return null;
        }

        for (int k = 0, t = messages.Count; k < t; k++) {
            Dictionary<string, object> messageData = (Dictionary<string, object>)messages [k];
            SwrveMessage message = SwrveMessage.LoadFromJSON (swrveAssetsManager, campaign, messageData, defaultBackgroundColor);
            if (message.Formats.Count > 0) {
                campaign.AddMessage (message);
            }
        }
        if (campaign.Messages.Count == 0) {
            string reason = "Campaign [" + id + "] no messages found, skipping.";
            campaign.LogAndAddReason(reason, false, qaUserCampaignInfoList);
        }

        return campaign;
    }

    #region SwrveBaseCampaign Abstract Methods implementation
    public override bool AreAssetsReady()
    {
        return this.Messages.All (m => m.IsDownloaded ());
    }

    public override bool SupportsOrientation(SwrveOrientation orientation)
    {
        return this.Messages.Any (m => m.SupportsOrientation (orientation));
    }

    public override SwrveQaUserCampaignInfo.SwrveCampaignType GetCampaignType()
    {
        return SwrveQaUserCampaignInfo.SwrveCampaignType.Iam;
    }

    #endregion

}
}
