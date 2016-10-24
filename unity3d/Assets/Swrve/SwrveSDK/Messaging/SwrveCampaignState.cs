using System;
using System.Collections.Generic;
using SwrveUnity.Helpers;

namespace SwrveUnity.Messaging
{
public class SwrveCampaignState
{
    const string SEEN_KEY = "seen";
    const string DELETED_KEY = "deleted";

    /**
     * The status of the campaign
     */
    public enum Status {
        Unseen,
        Seen,
        Deleted
    }

    public int Impressions;

    public int Next;

    public DateTime ShowMessagesAfterDelay;

    /// <summary>
    // MessageCenter status of the campaign
    /// </summary>
    public SwrveCampaignState.Status CurStatus;

    public SwrveCampaignState () {
        ShowMessagesAfterDelay = SwrveHelper.GetNow ();
    }

    public SwrveCampaignState (int campaignId, Dictionary<string, object> savedStatesJson)
    {
        string curKey;

        // Load next
        curKey = "Next" + campaignId;
        if (savedStatesJson.ContainsKey (curKey)) {
            Next = MiniJsonHelper.GetInt (savedStatesJson, curKey);
        }

        // Load impressions
        curKey = "Impressions" + campaignId;
        if (savedStatesJson.ContainsKey (curKey)) {
            Impressions = MiniJsonHelper.GetInt (savedStatesJson, curKey);
        }

        // Load cur status
        curKey = "Status" + campaignId;
        if (savedStatesJson.ContainsKey (curKey)) {
            CurStatus = ParseStatus (MiniJsonHelper.GetString (savedStatesJson, curKey));
        }
        else {
            CurStatus = Status.Unseen;
        }
    }

    /**
    * Convert from String to SwrveCampaignStatus.
    *
    * @param status String campaign status.
    * @return SwrveCampaignStatus
    */
    public static Status ParseStatus(string status) {
        if (status.ToLower().Equals(SEEN_KEY)) {
            return Status.Seen;
        } else if (status.ToLower().Equals(DELETED_KEY)) {
            return Status.Deleted;
        }
        
        return Status.Unseen;
    }

    public override string ToString() {
        return string.Format (
            "[SwrveCampaignState] Impressions: {0}, Next: {1}, ShowMessagesAfterDelay: {2}, CurStatus: {3}",
            Impressions, Next, ShowMessagesAfterDelay, CurStatus
        );
    }
}
}
