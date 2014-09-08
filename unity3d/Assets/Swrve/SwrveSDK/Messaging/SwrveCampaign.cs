/*
 * SWRVE CONFIDENTIAL
 * 
 * (c) Copyright 2010-2014 Swrve New Media, Inc. and its licensors.
 * All Rights Reserved.
 *
 * NOTICE: All information contained herein is and remains the property of Swrve
 * New Media, Inc or its licensors.  The intellectual property and technical
 * concepts contained herein are proprietary to Swrve New Media, Inc. or its
 * licensors and are protected by trade secret and/or copyright law.
 * Dissemination of this information or reproduction of this material is
 * strictly forbidden unless prior written permission is obtained from Swrve.
 */

using System.Collections.Generic;
using System.Collections;
using System;
using Swrve.Helpers;

namespace Swrve.Messaging
{
/// <summary>
/// Swrve Talk campaign.
/// </summary>
public class SwrveCampaign
{
    protected readonly Random rnd = new Random ();
    protected const string WaitTimeFormat = @"HH\:mm\:ss zzz";
    protected const int DefaultDelayFirstMessage = 180;
    protected const long DefaultMaxShows = 99999;
    protected const int DefaultMinDelay = 60;

    /// <summary>
    /// Identifies the campaign.
    /// </summary>
    public int Id;

    /// <summary>
    /// List of messages contained in the campaign.
    /// </summary>
    public List<SwrveMessage> Messages;

    /// <summary>
    /// List of triggers for the campaign.
    /// </summary>
    public HashSet<string> Triggers;

    /// <summary>
    /// The start date of the campaign.
    /// </summary>
    public DateTime StartDate;

    /// <summary>
    /// The end date of the campaign.
    /// </summary>
    public DateTime EndDate;

    /// <summary>
    /// Number of impressions of this campaign. Used to disable the campaign if
    /// it reaches total impressions.
    /// </summary>
    public int Impressions;

    /// <summary>
    /// Next message to be shown if round robin campaign.
    /// </summary>
    public int Next;

    /// <summary>
    /// Indicates if the campaign serves messages randomly or using round robin.
    /// </summary>
    public bool RandomOrder = false;

    protected readonly DateTime swrveInitialisedTime;
    protected readonly string assetPath;
    protected DateTime showMessagesAfterLaunch;
    protected DateTime showMessagesAfterDelay;
    protected int minDelayBetweenMessage;
    protected int delayFirstMessage = DefaultDelayFirstMessage;
    protected int maxImpressions;

    private SwrveCampaign (DateTime initialisedTime, string assetPath)
    {
        this.swrveInitialisedTime = initialisedTime;
        this.assetPath = assetPath;
        this.Messages = new List<SwrveMessage> ();
        this.Triggers = new HashSet<string> ();
        this.minDelayBetweenMessage = DefaultMinDelay;
        this.showMessagesAfterLaunch = swrveInitialisedTime + TimeSpan.FromSeconds (DefaultDelayFirstMessage);
    }

    /// <summary>
    /// Search for a message related to the given trigger event at the given
    /// time. This function will return null if too many messages were dismissed,
    /// the campaign start is in the future, the campaign end is in the past or
    /// the given event is not contained in the trigger set.
    /// </summary>
    /// <param name="triggerEvent">
    /// Event triggered. Must be a trigger for the campaign.
    /// </param>
    /// <param name="campaignReasons">
    /// At the exit of the function will include the reasons why a campaign the campaigns
    /// in memory were not shown or chosen.
    /// </param>
    /// <returns>
    /// In-app message that contains the given event in its trigger list and satisfies all the
    /// rules.
    /// </returns>
    public SwrveMessage GetMessageForEvent (string triggerEvent, Dictionary<int, string> campaignReasons)
    {
        // Use UTC to compare to start/end dates from DB
        DateTime utcNow = SwrveHelper.GetUtcNow ();
        // Use local time to track throttle limits (want to show local time in logs)
        DateTime localNow = SwrveHelper.GetNow ();

        int messagesCount = Messages.Count;

        if (!HasMessageForEvent (triggerEvent)) {
            SwrveLog.Log ("There is no trigger in " + Id + " that matches " + triggerEvent);
            return null;
        }

        if (messagesCount == 0) {
            LogAndAddReason (campaignReasons, "No messages in campaign " + Id);
            return null;
        }

        if (StartDate > utcNow) {
            LogAndAddReason (campaignReasons, "Campaign " + Id + " has not started yet");
            return null;
        }

        if (EndDate < utcNow) {
            LogAndAddReason (campaignReasons, "Campaign" + Id + " has finished");
            return null;
        }

        if (Impressions >= maxImpressions) {
            LogAndAddReason (campaignReasons, "{Campaign throttle limit} Campaign " + Id + " has been shown " + maxImpressions + " times already");
            return null;
        }

        if (!string.Equals (triggerEvent, SwrveSDK.DefaultAutoShowMessagesTrigger, StringComparison.OrdinalIgnoreCase) && IsTooSoonToShowMessageAfterLaunch (localNow)) {
            LogAndAddReason (campaignReasons, "{Campaign throttle limit} Too soon after launch. Wait until " + showMessagesAfterLaunch.ToString (WaitTimeFormat));
            return null;
        }

        if (IsTooSoonToShowMessageAfterDelay (localNow)) {
            LogAndAddReason (campaignReasons, "{Campaign throttle limit} Too soon after last message. Wait until " + showMessagesAfterDelay.ToString (WaitTimeFormat));
            return null;
        }

        SwrveLog.Log (triggerEvent + " matches a trigger in " + Id);

        return GetNextMessage (messagesCount, campaignReasons);
    }

    protected void LogAndAddReason (Dictionary<int, string> campaignReasons, string reason)
    {
        if (campaignReasons != null) {
            campaignReasons.Add (Id, reason);
        }
        SwrveLog.Log (reason);
    }

    /// <summary>
    /// Check if this campaign contains any message configured for the
    /// given event trigger.
    /// </summary>
    /// <returns>
    /// True if this campaign contains a message with the given trigger event.
    /// False otherwise.
    /// </returns>
    public bool HasMessageForEvent (string eventName)
    {
        string lowercaseEventName = eventName.ToLower ();
        return Triggers != null && Triggers.Contains (lowercaseEventName);
    }

    /// <summary>
    /// Get a message by its identifier.
    /// </summary>
    /// <returns>
    /// The message with the given identifier if it could be found.
    /// </returns>
    public SwrveMessage GetMessageForId (int id)
    {
        foreach (SwrveMessage message in Messages) {
            if (message.Id == id) {
                return message;
            }
        }

        return null;
    }

    protected SwrveMessage GetNextMessage (int messagesCount, Dictionary<int, string> campaignReasons)
    {
        if (RandomOrder) {
            List<SwrveMessage> randomMessages = new List<SwrveMessage> (Messages);
            randomMessages.Shuffle ();
            foreach (SwrveMessage message in randomMessages) {
                if (message.isDownloaded (assetPath)) {
                    return message;
                }
            }
        } else if (Next < messagesCount) {
            SwrveMessage message = Messages [Next];
            if (message.isDownloaded (assetPath)) {
                return message;
            }
        }

        LogAndAddReason (campaignReasons, "Campaign " + this.Id + " hasn't finished downloading.");
        return null;
    }

    protected void AddMessage (SwrveMessage message)
    {
        this.Messages.Add (message);
    }

    /// <summary>
    /// Load an in-app campaign from a JSON response.
    /// </summary>
    /// <param name="campaignData">
    /// JSON object with the individual campaign data.
    /// </param>
    /// <param name="initialisedTime">
    /// Time that the SDK was initialised. Used for rules checking.
    /// </param>
    /// <param name="assetPath">
    /// Path to the folder that will store all the assets.
    /// </param>
    /// <returns>
    /// Parsed in-app campaign.
    /// </returns>
    public static SwrveCampaign LoadFromJSON (Dictionary<string, object> campaignData, DateTime initialisedTime, string assetPath)
    {
        SwrveCampaign campaign = new SwrveCampaign (initialisedTime, assetPath);
        campaign.Id = MiniJsonHelper.GetInt (campaignData, "id");

        AssignCampaignTriggers (campaign, campaignData);
        AssignCampaignRules (campaign, campaignData);
        AssignCampaignDates (campaign, campaignData);

        IList<object> jsonMessages = (IList<object>)campaignData ["messages"];
        for (int k = 0, t = jsonMessages.Count; k < t; k++) {
            Dictionary<string, object> messageData = (Dictionary<string, object>)jsonMessages [k];
            SwrveMessage message = SwrveMessage.LoadFromJSON (campaign, messageData);
            if (message.Formats.Count > 0) {
                campaign.AddMessage (message);
            }
        }

        return campaign;
    }

    /// <summary>
    /// Get all the assets in the in-app campaign messages.
    /// </summary>
    /// <returns>
    /// All the assets in the in-app campaign.
    /// </returns>
    public List<string> ListOfAssets ()
    {
        List<string> allAssets = new List<string> ();

        foreach (SwrveMessage message in this.Messages) {
            allAssets.AddRange (message.ListOfAssets ());
        }

        return allAssets;
    }

    protected static void AssignCampaignTriggers (SwrveCampaign campaign, Dictionary<string, object> campaignData)
    {
        IList<object> jsonTriggers = (IList<object>)campaignData ["triggers"];
        for (int i = 0, j = jsonTriggers.Count; i < j; i++) {
            string trigger = (string)jsonTriggers [i];
            campaign.Triggers.Add (trigger.ToLower ());
        }
    }

    protected static void AssignCampaignRules (SwrveCampaign campaign, Dictionary<string, object> campaignData)
    {
        Dictionary<string, object> rules = (Dictionary<string, object>)campaignData ["rules"];
        campaign.RandomOrder = ((string)rules ["display_order"]).Equals ("random");

        if (rules.ContainsKey ("dismiss_after_views")) {
            int totalImpressions = MiniJsonHelper.GetInt (rules, "dismiss_after_views");
            campaign.maxImpressions = totalImpressions;
        }

        if (rules.ContainsKey ("delay_first_message")) {
            campaign.delayFirstMessage = MiniJsonHelper.GetInt (rules, "delay_first_message");
            campaign.showMessagesAfterLaunch = campaign.swrveInitialisedTime + TimeSpan.FromSeconds (campaign.delayFirstMessage);
        }

        if (rules.ContainsKey ("min_delay_between_messages")) {
            int minDelay = MiniJsonHelper.GetInt (rules, "min_delay_between_messages");
            campaign.minDelayBetweenMessage = minDelay;
        }
    }

    protected static void AssignCampaignDates (SwrveCampaign campaign, Dictionary<string, object> campaignData)
    {
        DateTime initDate = SwrveHelper.UnixEpoch;
        campaign.StartDate = initDate.AddMilliseconds (MiniJsonHelper.GetLong (campaignData, "start_date"));
        campaign.EndDate = initDate.AddMilliseconds (MiniJsonHelper.GetLong (campaignData, "end_date"));
    }

    public void IncrementImpressions ()
    {
        this.Impressions++;
    }

    protected bool IsTooSoonToShowMessageAfterLaunch (DateTime now)
    {
        return now < showMessagesAfterLaunch;
    }

    protected bool IsTooSoonToShowMessageAfterDelay (DateTime now)
    {
        return now < showMessagesAfterDelay;
    }

    /// <summary>
    /// Notify that the message was shown to the user. This function
    /// has to be called only once when the message is displayed to
    /// the user.
    /// This is automatically called by the SDK and will only need
    /// to be manually called if you are implementing your own
    /// in-app message rendering code.
    /// </summary>
    public void MessageWasShownToUser (SwrveMessageFormat messageFormat)
    {
        IncrementImpressions ();

        if (Messages.Count > 0) {
            // The message was shown. Take the current time so that we can throttle messages
            // from being shown too quickly.
            this.showMessagesAfterDelay = SwrveHelper.GetNow () + TimeSpan.FromSeconds (this.minDelayBetweenMessage);
            if (!RandomOrder) {
                int nextMessage = (Next + 1) % Messages.Count;
                Next = nextMessage;
                SwrveLog.Log ("Round Robin: Next message in campaign " + Id + " is " + nextMessage);
            } else {
                SwrveLog.Log ("Next message in campaign " + Id + " is random");
            }
        }
    }
}
}
