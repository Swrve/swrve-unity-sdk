using System;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// In-app button action types.
    /// </summary>
    public enum SwrveActionType
    {
        Install,
        Dismiss,
        Custom,
        CopyToClipboard,
        Capability,
        PageLink
    }

    public static class SwrveActionTypeExtensions
    {
        public static string QaActionTypeString(this SwrveActionType me)
        {
            switch (me)
            {
                case SwrveActionType.Install:
                    return "install";
                case SwrveActionType.Dismiss:
                    return "dismiss";
                case SwrveActionType.Custom:
                    return "deeplink";
                case SwrveActionType.CopyToClipboard:
                    return "clipboard";
                case SwrveActionType.Capability:
                    return "capability";
                case SwrveActionType.PageLink:
                    return ""; // Not supported as an actionType for campaign-button-clicked qalog event yet
                default:
                    return "";
            }
        }
    }
}