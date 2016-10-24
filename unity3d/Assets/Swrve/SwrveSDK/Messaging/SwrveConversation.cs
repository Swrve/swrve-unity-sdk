using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using UnityEngine;
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
    
    public List<string> ConversationAssets;

    public ISwrveAssetController assetController;

    /// <summary>
    /// Priority of the message.
    /// </summary>
    public int Priority = 9999;

    private SwrveConversation (ISwrveAssetController assetController, SwrveConversationCampaign campaign)
    {
        this.assetController = assetController;
        this.Campaign = campaign;
        this.ConversationAssets = new List<string> ();
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
    public static SwrveConversation LoadFromJSON (SwrveSDK sdk, SwrveConversationCampaign campaign, Dictionary<string, object> conversationData)
    {
        SwrveConversation conversation = new SwrveConversation (sdk, campaign);
        conversation.Id = MiniJsonHelper.GetInt (conversationData, "id");
        List<object> pages = (List<object>)conversationData ["pages"];
        for(int i = 0; i < pages.Count; i++) {
            Dictionary<string, object> page = (Dictionary<string, object>)pages [i];
            List<object> contents = (List<object>)page ["content"];
            for(int j = 0; j < contents.Count; j++) {
                Dictionary<string, object> content = (Dictionary<string, object>)contents[j];
                if ("image" == (string)content ["type"]) {
                    conversation.ConversationAssets.Add ((string)content ["value"]);
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
    public List<string> ListOfAssets ()
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
        List<string> assets = this.ListOfAssets ();
        return assets.All (asset => assetController.IsAssetInCache (asset));
    }

  	override public string GetBaseFormattedMessageType() {
  		  return "Conversation";
  	}
}
}