using UnityEngine;

namespace SwrveUnity.SwrveUsers
{

public enum SwrveTrackingState {
    UNKNOWN,
    STARTED,
    EVENT_SENDING_PAUSED,
    STOPPED
};

public static class SwrveTracking
{
    private const string TRACKING_STATE_KEY = "SwrveTrackingState";

    public static void SaveTrackingState(SwrveTrackingState state)
    {
        if (state == SwrveTrackingState.EVENT_SENDING_PAUSED) {
            return; // fail safe - never persist paused state
        }
        PlayerPrefs.SetString(TRACKING_STATE_KEY, state.ToString());
        PlayerPrefs.Save();
    }

    public static SwrveTrackingState GetTrackingState()
    {
        string state = PlayerPrefs.GetString(TRACKING_STATE_KEY, null);
        return SwrveTracking.parse(state);
    }

    public static SwrveTrackingState parse(string state)
    {
        SwrveTrackingState trackingState = SwrveTrackingState.UNKNOWN;
        if (string.IsNullOrEmpty(state)) {
            trackingState = SwrveTrackingState.UNKNOWN;
        } else if (string.Equals(state, "STARTED")) {
            trackingState = SwrveTrackingState.STARTED;
        } else if (string.Equals(state, "EVENT_SENDING_PAUSED")) {
            trackingState = SwrveTrackingState.EVENT_SENDING_PAUSED;
        } else if (string.Equals(state, "STOPPED")) {
            trackingState = SwrveTrackingState.STOPPED;
        } else if (string.Equals(state, "UNKNOWN")) {
            trackingState = SwrveTrackingState.UNKNOWN;
        }
        return trackingState;
    }
}
}
