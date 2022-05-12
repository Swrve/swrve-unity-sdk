using System;
using System.Linq;
using System.Collections.Generic;

namespace SwrveUnity.Messaging
{
    public class SwrveTrigger
    {
        const string EVENT_NAME_KEY = "event_name";
        const string CONDITIONS_KEY = "conditions";

        private string eventName;

        private SwrveConditions conditions;

        public string GetEventName()
        {
            return this.eventName;
        }

        public SwrveConditions GetConditions()
        {
            return this.conditions;
        }

        public bool CanTrigger(string eventName, IDictionary<string, string> payload)
        {
            return string.Equals(this.eventName, eventName, StringComparison.OrdinalIgnoreCase) &&
                   (conditions == null || conditions.Matches(payload));
        }

        public static SwrveTrigger LoadFromJson(object json)
        {
            IDictionary<string, object> dict = null;
            try
            {
                dict = (IDictionary<string, object>)json;
            }
            catch (Exception e)
            {
                SwrveLog.LogError(string.Format("Invalid object passed in to LoadFromJson, expected Dictionary<string, object>, received {0}, exception: {1}", json, e.Message));
                return null;
            }

            string eventName = null;
            SwrveConditions conditions = null;

            try
            {
                eventName = (string)dict[EVENT_NAME_KEY];
                if (dict.ContainsKey(CONDITIONS_KEY))
                {
                    conditions = SwrveConditions.LoadFromJson((IDictionary<string, object>)dict[CONDITIONS_KEY], true);
                }
            }
            catch (Exception e)
            {
                SwrveLog.LogError(string.Format("Error parsing a SwrveTrigger from json {0}, ex: {1}", dict, e));
            }

            if (string.IsNullOrEmpty(eventName) || (conditions == null))
            {
                return null;
            }

            SwrveTrigger trigger = new SwrveTrigger();
            trigger.eventName = eventName;
            trigger.conditions = conditions;
            return trigger;
        }

        public static IEnumerable<SwrveTrigger> LoadFromJson(List<object> triggers)
        {
            try
            {
                return triggers
                       .Select(dict => LoadFromJson(dict))
                       .Where(dict => dict != null);
            }
            catch (Exception e)
            {
                SwrveLog.LogError(string.Format("Error creating a list of SwrveTriggers, ex: {0}", e));
            }
            return null;
        }

        public static IEnumerable<SwrveTrigger> LoadFromJson(string json)
        {
            try
            {
                object triggers = SwrveUnityMiniJSON.Json.Deserialize(json);
                return LoadFromJson((List<object>)triggers);
            }
            catch (Exception e)
            {
                SwrveLog.LogError(string.Format("Error parsing a SwrveTrigger from json {0}, ex: {1}", json, e));
            }
            return null;
        }

        public override string ToString()
        {
            return "Trigger{" +
                   "eventName='" + eventName + '\'' +
                   ", conditions=" + conditions +
                   '}';
        }
    }
}
