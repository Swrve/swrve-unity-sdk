using System;
using System.Collections.Generic;
using Swrve.Helpers;

namespace Swrve.Messaging
{
public class SwrveCampaignState
{
    public int Impressions;

    public int Next;

    public DateTime ShowMessagesAfterDelay;

    public SwrveCampaignState () {
        ShowMessagesAfterDelay = SwrveHelper.GetNow ();
    }

    public SwrveCampaignState (int campaignId, Dictionary<string, object> savedStatesJson)
    {
        // Load next
        if (savedStatesJson.ContainsKey ("Next" + campaignId)) {
            Next = MiniJsonHelper.GetInt (savedStatesJson, "Next" + campaignId);
        }
        // Load impressions
        if (savedStatesJson.ContainsKey ("Impressions" + campaignId)) {
            Impressions = MiniJsonHelper.GetInt (savedStatesJson, "Impressions" + campaignId);
        }
        // Load ShowMessagesAfterDelay
        if (savedStatesJson.ContainsKey ("ShowMessagesAfterDelay" + campaignId)) {
            // Saved as number UnixEpoch Milliseconds
            DateTime initDate = SwrveHelper.UnixEpoch;
            ShowMessagesAfterDelay = initDate.AddMilliseconds (MiniJsonHelper.GetLong (savedStatesJson, "ShowMessagesAfterDelay" + campaignId));
        }
                
    }
}
}
