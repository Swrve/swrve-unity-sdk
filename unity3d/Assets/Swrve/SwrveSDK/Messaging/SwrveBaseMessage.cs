using UnityEngine;
using System.Collections;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Virtual super class of conversations and messages.
/// </summary>
public abstract class SwrveBaseMessage {
        
    /// <summary>
    /// Identifies the base message in a campaign.
    /// </summary>
    public int Id;

    /// <summary>
    /// Parent campaign.
    /// </summary>
    public SwrveBaseCampaign Campaign;

    public string GetBaseMessageType() {
        return GetBaseFormattedMessageType().ToLower();
    }

    public abstract string GetBaseFormattedMessageType();

  	public string GetEventPrefix() {
        return "Swrve." + GetBaseFormattedMessageType() + "s." + GetBaseMessageType() + "_";
  	}
}
}