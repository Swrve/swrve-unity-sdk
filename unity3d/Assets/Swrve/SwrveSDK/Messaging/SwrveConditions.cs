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
            OR
        }

        const string OP_KEY = "op";
        const string OP_EQ_KEY = "eq";

        const string OP_CONTAINS_KEY = "contains";
        const string OP_AND_KEY = "and";
        const string OP_OR_KEY = "or";

        const string KEY_KEY = "key";
        const string VALUE_KEY = "value";
        const string ARGS_KEY = "args";

        private string key;
        private TriggerOperatorType? op;
        private string value;

        private List<SwrveConditions> args;

        public string GetKey()
        {
            return this.key;
        }

        public TriggerOperatorType? GetOp()
        {
            return this.op;
        }

        public string GetValue()
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

        private SwrveConditions(TriggerOperatorType? op, string key, string value) : this(op)
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
                   string.Equals(payload[this.key], this.value, StringComparison.OrdinalIgnoreCase);
        }

        private bool matchesContains(IDictionary<string, string> payload)
        {
            return (this.op == TriggerOperatorType.CONTAINS) &&
                   payload.ContainsKey(this.key) &&
                   payload[this.key].ToLower().Contains(this.value.ToLower());
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