using System.Collections.Generic;

namespace SwrveUnity
{
    /// <summary>
    /// Used internally by SwrveQaUser to queue/send Qauser logs.
    /// </summary>
    public interface ISwrveQaUserQueue
    {
        void Queue(Dictionary<string, object> qaLogEvent);
        void FlushEvents();
    }
}
