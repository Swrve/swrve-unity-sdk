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
public abstract class SwrveBaseCampaign
{
    const string ID_KEY = "id";
    const string CONVERSATION_KEY = "conversation";
    const string MESSAGES_KEY = "messages";
    const string SUBJECT_KEY = "subject";
    const string MESSAGE_CENTER_KEY = "message_center";

    const string TRIGGERS_KEY = "triggers";
    const string EVENT_NAME_KEY = "event_name";
    const string CONDITIONS_KEY = "conditions";
    const string DISPLAY_ORDER_KEY = "display_order";
    const string RULES_KEY = "rules";
    const string RANDOM_KEY = "random";

    const string DISMISS_AFTER_VIEWS_KEY = "dismiss_after_views";
    const string DELAY_FIRST_MESSAGE_KEY = "delay_first_message";
    const string MIN_DELAY_BETWEEN_MESSAGES_KEY = "min_delay_between_messages";

    const string START_DATE_KEY = "start_date";

    const string END_DATE_KEY = "end_date";

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
    // Flag indicating if it is a MessageCenter campaign
    /// </summary>
    protected bool messageCenter;

    /// <summary>
    // MessageCenter subject of the campaign
    /// </summary>
    protected string subject;

    /// <summary>
    /// List of triggers for the campaign.
    /// </summary>
    protected List<SwrveTrigger> triggers;

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
    public int Next {
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

    protected SwrveBaseCampaign (DateTime initialisedTime)
    {
        this.State = new SwrveCampaignState();
        this.swrveInitialisedTime = initialisedTime;
        this.triggers = new List<SwrveTrigger> ();
        this.minDelayBetweenMessage = DefaultMinDelay;
        this.showMessagesAfterLaunch = swrveInitialisedTime + TimeSpan.FromSeconds (DefaultDelayFirstMessage);
    }

    public bool checkCampaignLimits (string triggerEvent, IDictionary<string, string> payload, SwrveQAUser qaUser)
    {
        // Use local time to track throttle limits (want to show local time in logs)
        DateTime localNow = SwrveHelper.GetNow ();

        if (!CanTrigger (triggerEvent, payload, qaUser)) {
            LogAndAddReason ("There is no trigger in " + Id + " that matches " + triggerEvent, qaUser);
            return false;
        }

        if (!IsActive (qaUser)) {
            return false;
        }

        if (Impressions >= maxImpressions) {
            LogAndAddReason ("{Campaign throttle limit} Campaign " + Id + " has been shown " + maxImpressions + " times already", qaUser);
            return false;
        }

        if (!string.Equals (triggerEvent, SwrveSDK.DefaultAutoShowMessagesTrigger, StringComparison.OrdinalIgnoreCase) && IsTooSoonToShowMessageAfterLaunch (localNow)) {
            LogAndAddReason ("{Campaign throttle limit} Too soon after launch. Wait until " + showMessagesAfterLaunch.ToString (WaitTimeFormat), qaUser);
            return false;
        }

        if (IsTooSoonToShowMessageAfterDelay (localNow)) {
            LogAndAddReason ("{Campaign throttle limit} Too soon after last message. Wait until " + showMessagesAfterDelay.ToString (WaitTimeFormat), qaUser);
            return false;
        }

        return true;
    }

    public bool IsActive (SwrveQAUser qaUser)
    {

        // Use UTC to compare to start/end dates from DB
        DateTime utcNow = SwrveHelper.GetUtcNow ();

        if (StartDate > utcNow) {
            LogAndAddReason (string.Format("Campaign {0} not started yet (now: {1}, end: {2})", Id, utcNow, StartDate), qaUser);
            return false;
        }

        if (EndDate < utcNow) {
            LogAndAddReason (string.Format("Campaign {0} has finished (now: {1}, end: {2})", Id, utcNow, EndDate), qaUser);
            return false;
        }

        return true;
    }

    protected void LogAndAddReason (string reason, SwrveQAUser qaUser)
    {
        if (qaUser != null && !qaUser.campaignReasons.ContainsKey (Id)) {
            qaUser.campaignReasons.Add (Id, reason);
        }
        SwrveLog.Log (reason);
    }

    protected void LogAndAddReason (int ident, string reason, SwrveQAUser qaUser)
    {
        LogAndAddReason (reason, qaUser);
    }

    public List<SwrveTrigger> GetTriggers ()
    {
        return triggers;
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
    public static SwrveBaseCampaign LoadFromJSON(SwrveSDK sdk, Dictionary<string, object> campaignData, DateTime initialisedTime, SwrveQAUser qaUser)
    {
        int id = MiniJsonHelper.GetInt(campaignData, ID_KEY);
        SwrveBaseCampaign campaign = null;

        if(campaignData.ContainsKey(CONVERSATION_KEY))
        {
            campaign = SwrveConversationCampaign.LoadFromJSON(sdk, campaignData, id, initialisedTime);
        }
        else if(campaignData.ContainsKey(MESSAGES_KEY))
        {
            campaign = SwrveMessagesCampaign.LoadFromJSON(sdk, campaignData, id, initialisedTime, qaUser);
        }

        if(campaign == null)
        {
            return null;
        }
        campaign.Id = id;
		
        AssignCampaignTriggers(campaign, campaignData);
        campaign.SetIsMessageCenter(campaignData.ContainsKey(MESSAGE_CENTER_KEY) && (bool)campaignData[MESSAGE_CENTER_KEY]);

        if((!campaign.IsMessageCenter()) && (campaign.GetTriggers().Count == 0))
        {
            campaign.LogAndAddReason("Campaign [" + campaign.Id + "], has no triggers. Skipping this campaign.", qaUser);
            return null;
        }

        AssignCampaignRules(campaign, campaignData);
        AssignCampaignDates(campaign, campaignData);
        campaign.Subject = campaignData.ContainsKey(SUBJECT_KEY) ? (string)campaignData[SUBJECT_KEY] : "";

        if(campaign.IsMessageCenter())
        {
            SwrveLog.Log(string.Format("message center campaign: {0}, {1}", campaign.GetType(), campaign.subject));
        }

        return campaign;
    }

    public abstract bool AreAssetsReady ();

    public abstract bool SupportsOrientation (SwrveOrientation orientation);

    /// <summary>
    /// Get all the assets in the in-app campaign messages.
    /// </summary>
    /// <returns>
    /// All the assets in the in-app campaign.
    /// </returns>
    public abstract List<string> ListOfAssets ();

    protected static void AssignCampaignTriggers (SwrveBaseCampaign campaign, Dictionary<string, object> campaignData)
    {
        IList<object> jsonTriggers = (IList<object>)campaignData [TRIGGERS_KEY];
        for (int i = 0, j = jsonTriggers.Count; i < j; i++) {
            object jsonTrigger = jsonTriggers [i];
            if (jsonTrigger.GetType () == typeof(string)) {
                jsonTrigger = new Dictionary<string, object> {
                    { EVENT_NAME_KEY, jsonTrigger },
                    { CONDITIONS_KEY, new Dictionary<string, object>() }
                };
            }

            try {
                SwrveTrigger trigger = SwrveTrigger.LoadFromJson ((IDictionary<string, object>)jsonTrigger);
                campaign.GetTriggers ().Add (trigger);
            } catch (Exception e) {
                SwrveLog.LogError ("Unable to parse SwrveTrigger from json " + Json.Serialize (jsonTrigger) + ", " + e);
            }
        }
    }

    protected static void AssignCampaignRules (SwrveBaseCampaign campaign, Dictionary<string, object> campaignData)
    {
        Dictionary<string, object> rules = (Dictionary<string, object>)campaignData [RULES_KEY];
        campaign.RandomOrder = ((string)rules [DISPLAY_ORDER_KEY]).Equals (RANDOM_KEY);

        if (rules.ContainsKey (DISMISS_AFTER_VIEWS_KEY)) {
            int totalImpressions = MiniJsonHelper.GetInt (rules, DISMISS_AFTER_VIEWS_KEY);
            campaign.maxImpressions = totalImpressions;
        }

        if (rules.ContainsKey (DELAY_FIRST_MESSAGE_KEY)) {
            campaign.delayFirstMessage = MiniJsonHelper.GetInt (rules, DELAY_FIRST_MESSAGE_KEY);
            campaign.showMessagesAfterLaunch = campaign.swrveInitialisedTime + TimeSpan.FromSeconds (campaign.delayFirstMessage);
        }

        if (rules.ContainsKey (MIN_DELAY_BETWEEN_MESSAGES_KEY)) {
            int minDelay = MiniJsonHelper.GetInt (rules, MIN_DELAY_BETWEEN_MESSAGES_KEY);
            campaign.minDelayBetweenMessage = minDelay;
        }
    }

    protected static void AssignCampaignDates (SwrveBaseCampaign campaign, Dictionary<string, object> campaignData)
    {
        DateTime initDate = SwrveHelper.UnixEpoch;
        campaign.StartDate = initDate.AddMilliseconds (MiniJsonHelper.GetLong (campaignData, START_DATE_KEY));
        campaign.EndDate = initDate.AddMilliseconds (MiniJsonHelper.GetLong (campaignData, END_DATE_KEY));
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

    protected void SetMessageMinDelayThrottle ()
    {
        this.showMessagesAfterDelay = SwrveHelper.GetNow () + TimeSpan.FromSeconds (this.minDelayBetweenMessage);
    }

    /// <summary>
    /// Notify that the a message was dismissed.
    /// This is automatically called by the SDK and will only need
    /// to be manually called if you are implementing your own
    /// in-app message rendering code.
    /// </summary>
    public void MessageDismissed ()
    {
        SetMessageMinDelayThrottle ();
    }

    public bool IsA<T> () where T : SwrveBaseCampaign
    {
        return GetType () == typeof(T);
    }

    /// <summary>
    /// Check if this campaign will trigger for the given event and payload
    /// </summary>
    /// <returns>
    /// True if this campaign contains a message with the given trigger event.
    /// False otherwise.
    /// </returns>
    public bool CanTrigger (string eventName, IDictionary<string, string> payload=null, SwrveQAUser qaUser=null)
    {
        return GetTriggers ().Any (trig => trig.CanTrigger (eventName, payload));
    }
}
}
