using UnityEngine;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Used internally to process clicks.
    /// </summary>
    public interface ISwrveButtonView
    {
        void ProcessButtonDown(Vector3 mousePosition);

        SwrveButtonClickResult ProcessButtonUp(Vector3 mousePosition, SwrveMessageTextTemplatingResolver templatingResolver);
    }
}
