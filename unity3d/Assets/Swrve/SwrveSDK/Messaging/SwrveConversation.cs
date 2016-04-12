using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using Swrve.Helpers;
using SwrveMiniJSON;

namespace Swrve.Messaging
{
/// <summary>
/// In-app message.
/// </summary>
public class SwrveConversation
{
    /// <summary>
    /// Identifies the message in a campaign.
    /// </summary>
    public string Conversation;
    
    public List<string> ConversationAssets;

    /// <summary>
    /// Parent in-app campaign.
    /// </summary>
    public SwrveCampaign Campaign;

    private SwrveConversation (SwrveCampaign campaign)
    {
        this.Campaign = campaign;
        this.ConversationAssets = new List<string> ();
    }

    /// <summary>
    /// Load an in-app message from a JSON response.
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
    public static SwrveConversation LoadFromJSON (SwrveCampaign campaign, Dictionary<string, object> conversationData)
    {
            SwrveConversation conversation = new SwrveConversation (campaign);
            foreach(object page in (List<object>)conversationData["pages"]) {
                foreach (object _content in (List<object>)((Dictionary<string, object>)page)["content"]) {
                    Dictionary<string, object> content = (Dictionary<string, object>)_content;
                    if ("image" == (string)content ["type"]) {
                        conversation.ConversationAssets.Add ((string)content ["value"]);
                    }
                }
            }
            conversation.Conversation = Json.Serialize (conversationData);
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
    public bool isDownloaded (string assetPath)
    {
        List<string> assets = this.ListOfAssets ();
        foreach (string asset in assets) {
            if (!CrossPlatformFile.Exists (assetPath + "/" + asset)) {
                return false;
            }
        }

        return true;
    }
}
}