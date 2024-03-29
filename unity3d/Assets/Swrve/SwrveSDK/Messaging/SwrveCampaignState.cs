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
        public enum Status
        {
            Unseen,
            Seen,
            Deleted
        }

        public int Impressions;

        public DateTime ShowMessagesAfterDelay;

        public long DownloadDate = SwrveHelper.GetNow().Ticks;

        /// <summary>
        // MessageCenter status of the campaign
        /// </summary>
        public Status CurStatus;

        public SwrveCampaignState()
        {
            ShowMessagesAfterDelay = SwrveHelper.GetNow();
        }

        public SwrveCampaignState(int campaignId, Dictionary<string, object> savedStatesJson)
        {
            string curKey;

            // Load impressions
            curKey = "Impressions" + campaignId;
            if (savedStatesJson.ContainsKey(curKey))
            {
                Impressions = MiniJsonHelper.GetInt(savedStatesJson, curKey);
            }

            // Load cur status
            curKey = "Status" + campaignId;
            if (savedStatesJson.ContainsKey(curKey))
            {
                CurStatus = ParseStatus(MiniJsonHelper.GetString(savedStatesJson, curKey));
            }
            else
            {
                CurStatus = Status.Unseen;
            }

            // Load downloadDate
            curKey = "DownloadDate" + campaignId;
            if (savedStatesJson.ContainsKey(curKey))
            {
                DownloadDate = MiniJsonHelper.GetLong(savedStatesJson, curKey);
            }
        }

        /**
        * Convert from String to SwrveCampaignStatus.
        *
        * @param status String campaign status.
        * @return SwrveCampaignStatus
        */
        public static Status ParseStatus(string status)
        {
            if (status.ToLower().Equals(SEEN_KEY))
            {
                return Status.Seen;
            }
            else if (status.ToLower().Equals(DELETED_KEY))
            {
                return Status.Deleted;
            }

            return Status.Unseen;
        }

        public override string ToString()
        {
            return string.Format(
                "[SwrveCampaignState] Impressions: {0}, ShowMessagesAfterDelay: {1}, CurStatus: {2}, DownloadDate: {3}",
                Impressions, ShowMessagesAfterDelay, CurStatus, DownloadDate
            );
        }
    }
}
