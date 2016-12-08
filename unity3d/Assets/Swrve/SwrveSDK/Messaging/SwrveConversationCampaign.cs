using System.Collections.Generic;
using System;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Swrve Talk campaign.
/// </summary>
public class SwrveConversationCampaign : SwrveBaseCampaign
{
    /// <summary>
    /// The Swrve Conversation associated with this campaign.
    /// </summary>
    public SwrveConversation Conversation;
    private SwrveConversationCampaign (DateTime initialisedTime) : base(initialisedTime)
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
    public SwrveConversation GetConversationForEvent(string triggerEvent, IDictionary<string, string> payload, SwrveQAUser qaUser)
    {
        if (null == Conversation) {
            LogAndAddReason ("No conversation in campaign " + Id, qaUser);
            return null;
        }

        if (checkCampaignLimits (triggerEvent, payload, qaUser)) {
            SwrveLog.Log (string.Format ("[{0}] {1} matches a trigger in {2}", this, triggerEvent, Id));
            if (AreAssetsReady ()) {
                return Conversation;
            } else {
                LogAndAddReason ("Assets not downloaded to show conversation in campaign " + Id, qaUser);
            }
        }
        return null;
    }

    public override bool AreAssetsReady()
    {
        return this.Conversation.IsDownloaded ();
    }

    public override bool SupportsOrientation(SwrveOrientation orientation)
    {
        return true;
    }

    /// <summary>
    /// Get all the assets in the in-app campaign messages.
    /// </summary>
    /// <returns>
    /// All the assets in the in-app campaign.
    /// </returns>
    public override HashSet<SwrveAssetsQueueItem> SetOfAssets ()
    {
        return Conversation.SetOfAssets();
    }

    new public static SwrveConversationCampaign LoadFromJSON (ISwrveAssetsManager swrveAssetsManager, Dictionary<string, object> campaignData, int campaignId, DateTime initialisedTime)
    {
        SwrveConversationCampaign campaign = new SwrveConversationCampaign (initialisedTime);
        campaign.Conversation = SwrveConversation.LoadFromJSON (swrveAssetsManager, campaign, (Dictionary<string, object>)campaignData ["conversation"]);
        return campaign;
    }
}
}
