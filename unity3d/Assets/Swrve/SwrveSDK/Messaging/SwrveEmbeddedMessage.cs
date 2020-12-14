using UnityEngine;
using System.Collections.Generic;
using SwrveUnity.Helpers;
using System;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Embedded message class.
/// </summary>
public class SwrveEmbeddedMessage : SwrveBaseMessage
{
    public IList<string> buttons;
    /// <summary>
    /// Data is the content of the message.
    /// </summary>
    public string data;

    public enum SwrveEmbeddedCampaignType {
        JSON,
        OTHER
    }

    /// <summary>
    /// Type of the data.
    /// </summary>
    public SwrveEmbeddedCampaignType type;

    /// <summary>
    /// Set the type of the Embedded Campaign as an enum from a string type.
    /// </summary>
    public void setType(string typeString)
    {
        if (typeString.Equals(Enum.GetName(typeof(SwrveEmbeddedCampaignType), SwrveEmbeddedCampaignType.JSON), StringComparison.InvariantCultureIgnoreCase)) {
            this.type = SwrveEmbeddedCampaignType.JSON;
        } else if (typeString.Equals(Enum.GetName(typeof(SwrveEmbeddedCampaignType), SwrveEmbeddedCampaignType.OTHER), StringComparison.InvariantCultureIgnoreCase)) {
            this.type = SwrveEmbeddedCampaignType.OTHER;
        }
    }

    public static SwrveEmbeddedMessage LoadFromJSON (SwrveEmbeddedCampaign campaign, Dictionary<string, object> messageData)
    {
        SwrveEmbeddedMessage message = new SwrveEmbeddedMessage();
        message.Campaign = campaign;
        if (messageData.ContainsKey ("priority")) {
            message.Priority = MiniJsonHelper.GetInt (messageData, "priority");
        }

        if (messageData.ContainsKey ("id")) {
            message.Id = MiniJsonHelper.GetInt (messageData, "id");
        }

        if (messageData.ContainsKey ("type")) {
            string typeString = MiniJsonHelper.GetString (messageData, "type");
            message.setType(typeString);
        }

        if (messageData.ContainsKey ("buttons")) {
            List<object> jsonButtons = (List<object>)messageData ["buttons"];
            if(jsonButtons.Count > 0) {
                message.buttons = new List<string>();
            }

            for(int i = 0; i < jsonButtons.Count; i++) {
                string buttonName = (string)jsonButtons[i];
                message.buttons.Add(buttonName);
            }
        }

        if (messageData.ContainsKey ("data")) {
            message.data = MiniJsonHelper.GetString (messageData, "data");
        }

        return message;
    }

    #region SwrveBaseMessage Abstract Methods

    override public bool SupportsOrientation (SwrveOrientation orientation)
    {
        return true;
    }

    #endregion
}

}
