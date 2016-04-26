using UnityEngine;
using System.Collections;

namespace Swrve.Messaging
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

    public string getBaseMessageType() {
        return getBaseFormattedMessageType().ToLower();
    }

    public abstract string getBaseFormattedMessageType();

  	public string GetEventPrefix() {
        return "Swrve." + getBaseFormattedMessageType() + "s." + getBaseMessageType() + "_";
  	}
}
}