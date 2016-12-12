using System.Collections.Generic;
using System.Linq;
using SwrveUnity.Helpers;
using SwrveUnityMiniJSON;

namespace SwrveUnity.Messaging
{
/// <summary>
/// In-app message.
/// </summary>
public class SwrveConversation : SwrveBaseMessage
{
    /// <summary>
    /// Identifies the message in a campaign.
    /// </summary>
    public string Conversation;
    
    public HashSet<SwrveAssetsQueueItem> ConversationAssets;

    public ISwrveAssetsManager SwrveAssetsManager;

    /// <summary>
    /// Priority of the message.
    /// </summary>
    public int Priority = 9999;

    private SwrveConversation (ISwrveAssetsManager swrveAssetsManager, SwrveConversationCampaign campaign)
    {
        this.SwrveAssetsManager = swrveAssetsManager;
        this.Campaign = campaign;
        this.ConversationAssets = new HashSet<SwrveAssetsQueueItem> ();
    }

    /// <summary>
    /// Load a conversation from a JSON response.
    /// </summary>
    /// <param name="campaign">
    /// Parent in-app campaign.
    /// </param>
    /// <param name="conversationData">
    /// JSON object with the conversation data.
    /// </param>
    /// <returns>
    /// Parsed conversation wrapper for native layer.
    /// </returns>
    public static SwrveConversation LoadFromJSON (ISwrveAssetsManager swrveAssetsManager, SwrveConversationCampaign campaign, Dictionary<string, object> conversationData)
    {
        SwrveConversation conversation = new SwrveConversation (swrveAssetsManager, campaign);
        conversation.Id = MiniJsonHelper.GetInt (conversationData, "id");
        List<object> pages = (List<object>)conversationData ["pages"];
        for(int i = 0; i < pages.Count; i++) {
            Dictionary<string, object> page = (Dictionary<string, object>)pages [i];
            List<object> contents = (List<object>)page ["content"];
            for(int j = 0; j < contents.Count; j++) {
                Dictionary<string, object> content = (Dictionary<string, object>)contents[j];
                if ("image" == (string)content ["type"]) {
                    string asset = (string)content ["value"];
                    conversation.ConversationAssets.Add (new SwrveAssetsQueueItem(asset, asset));
                }
            }
        }
        conversation.Conversation = Json.Serialize (conversationData);
        if (conversationData.ContainsKey ("priority")) {
            conversation.Priority = MiniJsonHelper.GetInt (conversationData, "priority");
        }
        
        return conversation;
    }

    /// <summary>
    /// Get all the assets in the in-app message.
    /// </summary>
    /// <returns>
    /// All the assets in the in-app message.
    /// </returns>
    public HashSet<SwrveAssetsQueueItem> SetOfAssets ()
    {
        return this.ConversationAssets;
    }

    /// <summary>
    /// Check if the campaign assets have been downloaded.
    /// </summary>
    /// <returns>
    /// True if the campaign assets have been downloaded.
    /// </returns>
    public bool IsDownloaded ()
    {
        HashSet<SwrveAssetsQueueItem> assets = this.SetOfAssets ();
        return assets.All (asset => this.SwrveAssetsManager.AssetsOnDisk.Contains(asset.Name));
    }

  	override public string GetBaseFormattedMessageType() {
  		  return "Conversation";
  	}
}
}