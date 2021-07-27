using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Internal class to do text templating.
/// </summary>
public class SwrveTextTemplating
{
    private const string patternMatch = "\\$\\{([^\\}]*)\\}"; // match any content beginning with ${ and ending in }
    private static readonly Regex pattern = new Regex(patternMatch);

    private const string patternFallbackMatch = "\\|fallback=\"([^\\}]*)\"\\}";
    private const string patternFallbackJSONMatch = "\\|fallback=\\\\\"([^\\}]*)\\\\\"\\}";
    private static readonly Regex patternFallback = new Regex(patternFallbackMatch);
    private static readonly Regex patternJSONFallback = new Regex(patternFallbackJSONMatch);

    public static string Apply(string text, Dictionary<string, string> properties)
    {
        if (string.IsNullOrEmpty(text)) {
            return text;
        }

        Match match = pattern.Match(text);
        while (match.Success) {
            string templateFullValue = match.Groups[0].Value;
            string fallback = GetFallBack(templateFullValue);
            string property = match.Groups[1].Value;
            if (fallback != null) {
                property = property.Substring(0, property.IndexOf("|fallback=\"")); // remove fallback text
            }

            if (properties != null && properties.ContainsKey(property) && !string.IsNullOrEmpty(properties[property])) {
                text = text.Replace(templateFullValue, properties[property]);
            } else if (fallback != null) {
                text = text.Replace(templateFullValue, fallback);
            } else {
                throw new SwrveSDKTextTemplatingException("TextTemplating: Missing property value for key " + property);
            }
            match = match.NextMatch();
        }
        return text;
    }

    // Example of expected template syntax:
    // ${item.property|fallback="fallback text"}
    private static string GetFallBack(string templateFullValue)
    {
        string fallback = null;
        Match match = patternFallback.Match(templateFullValue);
        while (match.Success) {
            fallback = match.Groups[1].Value;
            match = match.NextMatch();
        }
        return fallback;
    }

    public static string ApplyToJSON(string json, Dictionary<string, string> properties)
    {
        if (string.IsNullOrEmpty(json)) {
            return json;
        }

        Match match = pattern.Match(json);
        while (match.Success) {
            string templateFullValue = match.Groups[0].Value;
            string fallback = GetFallBackJSON(templateFullValue);
            string property = match.Groups[1].Value;
            if (fallback != null) {
                property = property.Substring(0, property.IndexOf("|fallback=\\\"")); // remove fallback text
            }

            if (properties != null && properties.ContainsKey(property) && !string.IsNullOrEmpty(properties[property])) {
                json = json.Replace(templateFullValue, properties[property]);
            } else if (fallback != null) {
                json = json.Replace(templateFullValue, fallback);
            } else {
                throw new SwrveSDKTextTemplatingException("TextTemplating: Missing property value for key " + property);
            }
            match = match.NextMatch();
        }
        return json;
    }

    // Example of expected template syntax:
    // {\"key\":"${item.property|fallback=\"fallback text\"}"}
    private static string GetFallBackJSON(string templateFullValue)
    {
        string fallback = null;
        Match match = patternJSONFallback.Match(templateFullValue);
        while (match.Success) {
            fallback = match.Groups[1].Value;
            match = match.NextMatch();
        }
        return fallback;
    }

    // Checks if the pattern exists within a given piece of text
    public static bool HasPatternMatch(string text)
    {
        if (text == null) {
            return false;
        }

        Match match = pattern.Match(text);
        return match.Success;
    }
}
}
