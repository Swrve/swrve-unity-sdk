using System.Collections.Generic;

namespace SwrveUnity.Messaging
{
public interface ISwrveMessagePersonalizationProvider
{
    /// <summary>
    /// Invoked when a campaign is getting ready to show and might need personalization sources.
    /// </summary>
    /// <param name="eventPayload">
    /// Payload of the event that triggered the campaign, if any
    /// </param>
    Dictionary<string, string> Personalize(IDictionary<string, string> eventPayload);
}
}
