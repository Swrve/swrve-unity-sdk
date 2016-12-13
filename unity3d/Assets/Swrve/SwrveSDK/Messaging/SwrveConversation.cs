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
    public HashSet<SwrveAssetsQueueItem> ConversationAssets { get; set; }
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
        for(int i = 0; i < pages.Count; i++) 
        {
            Dictionary<string, object> page = (Dictionary<string, object>)pages [i];

            // Add image and font assets to queue from content
            List<object> contents = (List<object>)page ["content"];
            for(int j = 0; j < contents.Count; j++)
            {
                Dictionary<string, object> content = (Dictionary<string, object>)contents[j];
                string contentType = (string)content ["type"];
                switch(contentType)
                {
                    case "image":
                        conversation.queueImageAsset(content);
                        break;
                    case "html-fragment":
                    case "star-rating":
                        conversation.queueFontAsset(content);
                        break;
                    case "multi-value-input":
                        conversation.queueFontAsset(content);
                        // iterate through options
                        List<object> jsonOptions = (List<object>)content ["values"];
                        for (int k = 0; k < jsonOptions.Count; k++)
                        {
                            Dictionary<string, object> optionData = (Dictionary<string, object>)jsonOptions [k];
                            conversation.queueFontAsset(optionData);
                        }
                        break;
                }
            }

            // Add font assets to queue from button control
            List<object> controls = (List<object>)page ["controls"];
            for(int j = 0; j < controls.Count; j++) 
            {
                Dictionary<string, object> buttonData = (Dictionary<string, object>)controls [j];
                conversation.queueFontAsset(buttonData);
            }
        }

        conversation.Conversation = Json.Serialize (conversationData);
        if (conversationData.ContainsKey ("priority")) {
            conversation.Priority = MiniJsonHelper.GetInt (conversationData, "priority");
        }
        
        return conversation;
    }

    private void queueImageAsset (Dictionary<string, object> content)
    {
        string asset = (string)content ["value"];
        ConversationAssets.Add (new SwrveAssetsQueueItem(asset, asset, true));        
    }

    private void queueFontAsset (Dictionary<string, object> content)
    {
        if (content.ContainsKey ("style") == false)
        {
            return;
        }
        Dictionary<string, object> style = (Dictionary<string, object>)content ["style"];
        if (style.ContainsKey ("font_file") == false || style.ContainsKey ("font_digest") == false)
        {
            return;
        }
        string fontFile = (string)style ["font_file"];
        string fontDigest = (string)style ["font_digest"];
        if(!string.IsNullOrEmpty(fontFile) && !string.IsNullOrEmpty(fontDigest))
        {
            ConversationAssets.Add (new SwrveAssetsQueueItem(fontFile, fontDigest, false));
        }
    }

    /// <summary>
    /// Check if the campaign assets have been downloaded.
    /// </summary>
    /// <returns>
    /// True if the campaign assets have been downloaded.
    /// </returns>
    public bool AreAssetsReady ()
    {
        return ConversationAssets.All (asset => this.SwrveAssetsManager.AssetsOnDisk.Contains(asset.Name));
    }

  	override public string GetBaseFormattedMessageType() {
  		  return "Conversation";
  	}
}
}