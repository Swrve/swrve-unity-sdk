using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using Swrve.Helpers;
using SwrveMiniJSON;

namespace Swrve.Messaging
{
/// <summary>
/// Swrve Talk campaign.
/// </summary>
public class SwrveCampaign
{
    public enum CampaignType {
        Conversation,
        Messages,
        Invalid
    }

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
    /// Identifies the campaign type, conversation or messages.
    /// </summary>
    public CampaignType campaignType = CampaignType.Invalid;

    /// <summary>
    // Flag indicating if it is a MessageCenter campaign
    /// </summary>
    protected bool messageCenter;

    /// <summary>
    // MessageCenter subject of the campaign
    /// </summary>
    protected string subject;
    
    /// <summary>
    /// List of messages contained in the campaign.
    /// </summary>
    public List<SwrveMessage> Messages;

    /// <summary>
    /// The Swrve Conversation associated with this campaign.
    /// </summary>
    public SwrveConversation Conversation;

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
    public int Impressions {
        get {
            return this.State.Impressions;
        }
        set {
            this.State.Impressions = value;
        }
    }

    /// <summary>
    /// Next message to be shown if round robin campaign.
    /// </summary>
    public int Next  {
        get {
            return this.State.Next;
        }
        set {
            this.State.Next = value;
        }
    }
     
    /// <summary>
    /// Get the status of the campaign.
    /// </summary>
    /// <returns>
    /// Status of the campaign.
    /// </returns>
    public SwrveCampaignState.Status Status {
        get {
            return this.State.CurStatus;
        }
        set {
            this.State.CurStatus = value;
        }
    }
        
    /**
     * Used internally to identify campaigns that have been marked as MessageCenter campaigns on the dashboard.
     *
     * @return true if the campaign is an MessageCenter campaign.
     */
    public bool IsMessageCenter() {
      return messageCenter;
    }

    protected void SetIsMessageCenter(bool isMessageCenter) {
        this.messageCenter = isMessageCenter;
    }

    /**
     * @return the name of the campaign.
     */
    public string Subject {
        get { return subject; }
        protected set { this.subject = value; }
    }

    /// <summary>
    /// Indicates if the campaign serves messages randomly or using round robin.
    /// </summary>
    public bool RandomOrder = false;

    /// <summary>
    /// Used internally to save the state of the campaign.
    /// </summary>
    public SwrveCampaignState State;

    protected readonly DateTime swrveInitialisedTime;
    protected readonly string assetPath;
    protected DateTime showMessagesAfterLaunch;
    protected DateTime showMessagesAfterDelay {
        get {
            return this.State.ShowMessagesAfterDelay;
        }
        set {
            this.State.ShowMessagesAfterDelay = value;
        }
    }
    protected int minDelayBetweenMessage;
    protected int delayFirstMessage = DefaultDelayFirstMessage;
    protected int maxImpressions;

    private SwrveCampaign (DateTime initialisedTime, string assetPath)
    {
        this.State = new SwrveCampaignState();
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
        int messagesCount = Messages.Count;

        if (messagesCount == 0) {
            LogAndAddReason (campaignReasons, "No messages in campaign " + Id);
            return null;
        }

        if (checkCampaignLimits (triggerEvent, campaignReasons)) {
            SwrveLog.Log (triggerEvent + " matches a trigger in " + Id);

            return GetNextMessage (messagesCount, campaignReasons);
        }
        return null;
    }

    public string GetConversationForEvent(string triggerEvent, Dictionary<int, string> campaignReasons)
    {
        if (null == Conversation) {
            LogAndAddReason (campaignReasons, "No conversation in campaign " + Id);
            return null;
        }

        if (checkCampaignLimits (triggerEvent, campaignReasons)) {
            SwrveLog.Log (triggerEvent + " matches a trigger in " + Id);

            return Conversation.Conversation;
        }
        return null;
    }

    public bool checkCampaignLimits(string triggerEvent, Dictionary<int, string> campaignReasons)
    {
        // Use local time to track throttle limits (want to show local time in logs)
        DateTime localNow = SwrveHelper.GetNow ();

        if (!HasMessageForEvent (triggerEvent)) {
            SwrveLog.Log ("There is no trigger in " + Id + " that matches " + triggerEvent);
            return false;
        }

        if (!IsActive (campaignReasons)) {
            return false;
        }

        if (Impressions >= maxImpressions) {
            LogAndAddReason (campaignReasons, "{Campaign throttle limit} Campaign " + Id + " has been shown " + maxImpressions + " times already");
            return false;
        }

        if (!string.Equals (triggerEvent, SwrveSDK.DefaultAutoShowMessagesTrigger, StringComparison.OrdinalIgnoreCase) && IsTooSoonToShowMessageAfterLaunch (localNow)) {
            LogAndAddReason (campaignReasons, "{Campaign throttle limit} Too soon after launch. Wait until " + showMessagesAfterLaunch.ToString (WaitTimeFormat));
            return false;
        }

        if (IsTooSoonToShowMessageAfterDelay (localNow)) {
            LogAndAddReason (campaignReasons, "{Campaign throttle limit} Too soon after last message. Wait until " + showMessagesAfterDelay.ToString (WaitTimeFormat));
            return false;
        }

        return true;
    }

    public bool IsActive(Dictionary<int, string> campaignReasons=null) {

        // Use UTC to compare to start/end dates from DB
        DateTime utcNow = SwrveHelper.GetUtcNow ();

        if (StartDate > utcNow) {
            if(null != campaignReasons) {
                LogAndAddReason (campaignReasons, "Campaign " + Id + " has not started yet");
            }
            return false;
        }

        if (EndDate < utcNow) {
            if(null != campaignReasons) {
                LogAndAddReason (campaignReasons, "Campaign " + Id + " has finished");
            }
            return false;
        }

        return true;
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
    public static SwrveCampaign LoadFromJSON (SwrveSDK sdk, Dictionary<string, object> campaignData, DateTime initialisedTime, string assetPath)
    {
        SwrveCampaign campaign = new SwrveCampaign (initialisedTime, assetPath);
        campaign.Id = MiniJsonHelper.GetInt (campaignData, "id");

        AssignCampaignTriggers (campaign, campaignData);
        AssignCampaignRules (campaign, campaignData);
        AssignCampaignDates (campaign, campaignData);

        campaign.SetIsMessageCenter (campaignData.ContainsKey ("message_center") && (bool)campaignData ["message_center"]);
        campaign.Subject = campaignData.ContainsKey ("subject") ? (string)campaignData ["subject"] : "";

        if (campaignData.ContainsKey("conversation"))
        {
            campaign.Conversation = SwrveConversation.LoadFromJSON(campaign, (Dictionary<string, object>)campaignData["conversation"]);
            campaign.campaignType = CampaignType.Conversation;
        }
        else if (campaignData.ContainsKey("messages"))
        {
            IList<object> jsonMessages = (IList<object>)campaignData ["messages"];
            for (int k = 0, t = jsonMessages.Count; k < t; k++) {
                Dictionary<string, object> messageData = (Dictionary<string, object>)jsonMessages [k];
                SwrveMessage message = SwrveMessage.LoadFromJSON (sdk, campaign, messageData);
                if (message.Formats.Count > 0) {
                    campaign.AddMessage (message);
                }
            }

            if (0 < campaign.Messages.Count) {
                campaign.campaignType = CampaignType.Messages;
            }
        }

        if (campaign.IsMessageCenter ()) {
            UnityEngine.Debug.Log (string.Format ("message center campaign: {0}, {1}", campaign.campaignType.ToString (), campaign.subject));
        }

        return campaign;
    }

    public bool AreAssetsReady()
    {
        if (this.campaignType == CampaignType.Messages) {
            return this.Messages.All (m => m.isDownloaded (assetPath));
        }
        else if(this.campaignType == CampaignType.Conversation) {
            return this.Conversation.isDownloaded (assetPath);
        }
        return false;
    }

    public bool SupportsOrientation(SwrveOrientation orientation) {
        if (SwrveOrientation.Either == orientation) {
            return true;
        }
        else if (this.campaignType == CampaignType.Messages) {
            return this.Messages.Any (m => m.SupportsOrientation (orientation));
        }
        else if(this.campaignType == CampaignType.Conversation) {
            return orientation == SwrveOrientation.Portrait;
        }
        return false;
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

        if (this.campaignType == CampaignType.Conversation) {
            allAssets.AddRange (Conversation.ListOfAssets ());
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

    protected void SetMessageMinDelayThrottle()
    {
        this.showMessagesAfterDelay = SwrveHelper.GetNow () + TimeSpan.FromSeconds (this.minDelayBetweenMessage);
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
        Status = SwrveCampaignState.Status.Seen;
        IncrementImpressions ();
        SetMessageMinDelayThrottle ();
        if (Messages.Count > 0) {
            if (!RandomOrder) {
                int nextMessage = (Next + 1) % Messages.Count;
                Next = nextMessage;
                SwrveLog.Log ("Round Robin: Next message in campaign " + Id + " is " + nextMessage);
            } else {
                SwrveLog.Log ("Next message in campaign " + Id + " is random");
            }
        }
    }

    /// <summary>
    /// Notify that the a message was dismissed.
    /// This is automatically called by the SDK and will only need
    /// to be manually called if you are implementing your own
    /// in-app message rendering code.
    /// </summary>
    public void MessageDismissed ()
    {
        SetMessageMinDelayThrottle();
    }
}
}
