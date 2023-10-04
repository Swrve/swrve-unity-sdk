using System;
using System.Linq;
using System.Collections.Generic;

namespace SwrveUnity.Messaging
{
    public class SwrveConditions
    {
        public enum TriggerOperatorType
        {
            AND,
            EQUALS,
            CONTAINS,
            NUMBER_EQUALS,
            NUMBER_GT,
            NUMBER_LT,
            NUMBER_BETWEEN,
            NUMBER_NOT_BETWEEN,
            OR
        }

        const string OP_KEY = "op";
        const string OP_EQ_KEY = "eq";

        const string OP_CONTAINS_KEY = "contains";
        const string OP_AND_KEY = "and";
        const string OP_OR_KEY = "or";
        const string OP_NUMBER_EQUALS_KEY = "number_eq";
        const string OP_NUMBER_GT_KEY = "number_gt";
        const string OP_NUMBER_LT_KEY = "number_lt";
        const string OP_NUMBER_BETWEEN_KEY = "number_between";
        const string OP_NUMBER_NOT_BETWEEN_KEY = "number_not_between";

        const string KEY_KEY = "key";
        const string VALUE_KEY = "value";
        const string ARGS_KEY = "args";

        private string key;
        private TriggerOperatorType? op;
        private object value;

        private List<SwrveConditions> args;

        public string GetKey()
        {
            return this.key;
        }

        public TriggerOperatorType? GetOp()
        {
            return this.op;
        }

        public object GetValue()
        {
            return this.value;
        }

        public List<SwrveConditions> GetArgs()
        {
            return this.args;
        }

        private SwrveConditions(TriggerOperatorType? op)
        {
            this.op = op;
        }

        private SwrveConditions(TriggerOperatorType? op, string key, object value) : this(op)
        {
            this.key = key;
            this.value = value;
        }

        private SwrveConditions(TriggerOperatorType? op, List<SwrveConditions> args) : this(op)
        {
            this.args = args;
        }

        private bool isEmpty()
        {
            return this.op == null;
        }

        private bool matchesEquals(IDictionary<string, string> payload)
        {
            return (this.op == TriggerOperatorType.EQUALS) &&
                   payload.ContainsKey(this.key) &&
                   string.Equals(payload[this.key], this.value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private bool matchesContains(IDictionary<string, string> payload)
        {
            return (this.op == TriggerOperatorType.CONTAINS) &&
                   payload.ContainsKey(this.key) &&
                   payload[this.key].ToLower().Contains(this.value.ToString().ToLower());
        }

        private bool matchesNumeric(IDictionary<string, string> payload)
        {
            if (!payload.ContainsKey(this.key))
                return false;

            // paylaod values are string, incase there are multiple campaigns with the same trigger event and keys with a mix of
            // string and numeric operators, we use int.TryParse to check if the value is a number or not for some
            // of the numeric operators, to prevent an exception be thrown, which could potentially block other campaigns from being displayed

            if (this.op == TriggerOperatorType.NUMBER_GT)
            {
                int payloadValue;
                bool payloadValueIsInt = int.TryParse(payload[this.key], out payloadValue);

                return payloadValueIsInt && payloadValue > Convert.ToInt32(this.value);
            }
            else if (this.op == TriggerOperatorType.NUMBER_LT)
            {
                int payloadValue;
                bool payloadValueIsInt = int.TryParse(payload[this.key], out payloadValue);

                return payloadValueIsInt && payloadValue < Convert.ToInt32(this.value);
            }
            else if (this.op == TriggerOperatorType.NUMBER_EQUALS)
            {
                int payloadValue;
                bool payloadValueIsInt = int.TryParse(payload[this.key], out payloadValue);

                return payloadValueIsInt && payloadValue == Convert.ToInt32(this.value);
            }
            else if (this.op == TriggerOperatorType.NUMBER_BETWEEN)
            {
                Dictionary<string, object> values = new Dictionary<string, object>();
                values = this.value as Dictionary<string, object>;

                int lower = 0;
                int upper = 0;
                if (values.ContainsKey("lower"))
                    lower = Convert.ToInt32(values["lower"]);

                if (values.ContainsKey("upper"))
                    upper = Convert.ToInt32(values["upper"]);

                return (values.ContainsKey("lower") && values.ContainsKey("upper") && int.Parse(payload[this.key]) > lower && int.Parse(payload[this.key]) < upper);
            }
            else if (this.op == TriggerOperatorType.NUMBER_NOT_BETWEEN)
            {
                Dictionary<string, object> values = new Dictionary<string, object>();
                values = this.value as Dictionary<string, object>;

                int lower = 0;
                int upper = 0;
                if (values.ContainsKey("lower"))
                    lower = Convert.ToInt32(values["lower"]);

                if (values.ContainsKey("upper"))
                    upper = Convert.ToInt32(values["upper"]);

                return (values.ContainsKey("lower") && values.ContainsKey("upper") && (int.Parse(payload[this.key]) < lower || int.Parse(payload[this.key]) > upper));
            }
            else return false;
        }

        private bool matchesAll(IDictionary<string, string> payload)
        {
            return this.args.All(cond => cond.Matches(payload));
        }

        private bool matchesAny(IDictionary<string, string> payload)
        {
            return this.args.Any(cond => cond.Matches(payload));
        }

        public bool Matches(IDictionary<string, string> payload)
        {
            if (this.op == TriggerOperatorType.OR)
            {
                return isEmpty() || ((payload != null) && (matchesEquals(payload) || matchesAny(payload)));
            }
            else if (this.op == TriggerOperatorType.AND)
            {
                return isEmpty() || ((payload != null) && (matchesEquals(payload) || matchesAll(payload)));
            }
            else if (this.op == TriggerOperatorType.EQUALS)
            {
                return isEmpty() || ((payload != null) && (matchesEquals(payload)));
            }
            else if (this.op == TriggerOperatorType.CONTAINS)
            {
                return isEmpty() || ((payload != null) && (matchesContains(payload)));
            }
            else if (this.op == TriggerOperatorType.NUMBER_GT
                        || this.op == TriggerOperatorType.NUMBER_LT
                        || this.op == TriggerOperatorType.NUMBER_EQUALS
                        || this.op == TriggerOperatorType.NUMBER_BETWEEN
                        || this.op == TriggerOperatorType.NUMBER_NOT_BETWEEN)
            {
                return isEmpty() || ((payload != null) && (matchesNumeric(payload)));
            }
            else
                return isEmpty();
        }

        public static SwrveConditions LoadFromJson(IDictionary<string, object> json, bool isRoot)
        {
            if (0 == json.Keys.Count)
            {
                if (isRoot)
                {
                    return new SwrveConditions(null);
                }

                return null;
            }

            string op = (string)json[OP_KEY];
            if (op == OP_EQ_KEY)
            {
                string key = (string)json[KEY_KEY];
                string value = (string)json[VALUE_KEY];
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return new SwrveConditions(TriggerOperatorType.EQUALS, key, value);
            }
            else if (op == OP_CONTAINS_KEY)
            {
                string key = (string)json[KEY_KEY];
                string value = (string)json[VALUE_KEY];
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                {
                    return null;
                }

                return new SwrveConditions(TriggerOperatorType.CONTAINS, key, value);
            }
            else if (op == OP_NUMBER_EQUALS_KEY)
            {
                string key = (string)json[KEY_KEY];
                object value = json[VALUE_KEY];
                if (string.IsNullOrEmpty(key) || value == null)
                {
                    return null;
                }

                return new SwrveConditions(TriggerOperatorType.NUMBER_EQUALS, key, value);
            }
            else if (op == OP_NUMBER_GT_KEY)
            {
                string key = (string)json[KEY_KEY];
                object value = (object)json[VALUE_KEY];
                if (string.IsNullOrEmpty(key) || value == null)
                {
                    return null;
                }

                return new SwrveConditions(TriggerOperatorType.NUMBER_GT, key, value);
            }
            else if (op == OP_NUMBER_LT_KEY)
            {
                string key = (string)json[KEY_KEY];
                object value = (object)json[VALUE_KEY];
                if (string.IsNullOrEmpty(key) || value == null)
                {
                    return null;
                }

                return new SwrveConditions(TriggerOperatorType.NUMBER_LT, key, value);
            }
            else if (op == OP_NUMBER_BETWEEN_KEY)
            {
                string key = (string)json[KEY_KEY];
                object value = (object)json[VALUE_KEY];
                if (string.IsNullOrEmpty(key) || value == null)
                {
                    return null;
                }

                return new SwrveConditions(TriggerOperatorType.NUMBER_BETWEEN, key, value);
            }
            else if (op == OP_NUMBER_NOT_BETWEEN_KEY)
            {
                string key = (string)json[KEY_KEY];
                object value = (object)json[VALUE_KEY];
                if (string.IsNullOrEmpty(key) || value == null)
                {
                    return null;
                }

                return new SwrveConditions(TriggerOperatorType.NUMBER_NOT_BETWEEN, key, value);
            }
            else if (isRoot && (op == OP_AND_KEY))
            {
                IList<object> jsonArgs = (IList<object>)json[ARGS_KEY];
                List<SwrveConditions> args = new List<SwrveConditions>();
                IEnumerator<object> it = jsonArgs.GetEnumerator();
                while (it.MoveNext())
                {
                    SwrveConditions condition = LoadFromJson((Dictionary<string, object>)it.Current, false);
                    if (condition == null)
                    {
                        return null;
                    }

                    args.Add(condition);
                }

                if (args.Count == 0)
                {
                    return null;
                }

                return new SwrveConditions(TriggerOperatorType.AND, args);
            }
            else if (isRoot && (op == OP_OR_KEY))
            {
                IList<object> jsonArgs = (IList<object>)json[ARGS_KEY];
                List<SwrveConditions> args = new List<SwrveConditions>();
                IEnumerator<object> it = jsonArgs.GetEnumerator();
                while (it.MoveNext())
                {
                    SwrveConditions condition = LoadFromJson((Dictionary<string, object>)it.Current, false);
                    if (condition == null)
                    {
                        return null;
                    }

                    args.Add(condition);
                }

                if (args.Count == 0)
                {
                    return null;
                }

                return new SwrveConditions(TriggerOperatorType.OR, args);
            }

            return null;
        }

        public override string ToString()
        {
            return "Conditions{" +
                   "key='" + key + '\'' +
                   ", op='" + op + '\'' +
                   ", value='" + value + '\'' +
                   ", args=" + args +
                   '}';
        }
    }
}