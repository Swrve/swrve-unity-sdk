using System.Collections.Generic;
using System.Linq;

public enum UNNotificationCategoryOptions {
    UNNotificationCategoryOptionNone,  // Default State
    UNNotificationCategoryOptionCustomDismissAction,   // Whether dismiss action should be sent to the UNUserNotificationCenter delegate
    UNNotificationCategoryOptionAllowInCarPlay // Whether notifications of this category should be allowed in CarPlay
}

public enum UNUserNotificationAction {
    UNNotificationActionOptionAuthenticationRequired,  // Whether this action should require unlocking before being performed.
    UNNotificationActionOptionDestructive,   // Whether this action should be indicated as destructive.
    UNNotificationActionOptionForeground // Whether this action should cause the application to launch in the foreground.
}

[System.Serializable]
public class UNNotificationAction
{
    const string IDENTIFIER_KEY = "identifier";
    const string TITLE_KEY = "title";
    const string OPTIONS_KEY = "options";

    public string identifier;
    public string title;

    public List<UNUserNotificationAction> options;

    public Dictionary<string, object> toDict()
    {
        List<string> actionOptions = new List<string>();
        if (0 < options.Count) {
            for (int i = 0; i < options.Count; i++) {
                actionOptions.Add(ExtractActionFromEnum(options[i]));
            }
        }

        Dictionary<string, object> retval = new Dictionary<string, object> ();
        retval [IDENTIFIER_KEY] = identifier;
        retval [TITLE_KEY] = title;
        retval [OPTIONS_KEY] = actionOptions;
        return retval;
    }


    private string ExtractActionFromEnum (UNUserNotificationAction action)
    {
        switch (action) {
        case UNUserNotificationAction.UNNotificationActionOptionForeground:
            return @"foreground";
        case UNUserNotificationAction.UNNotificationActionOptionDestructive:
            return @"destructive";
        case UNUserNotificationAction.UNNotificationActionOptionAuthenticationRequired:
            return @"auth-required";
        default:
            return @"";
        }
    }
}

[System.Serializable]
public class UNNotificationCategory
{
    const string IDENTIFIER_KEY = "identifier";
    const string OPTIONS_KEY = "options";
    const string ACTIONS_KEY = "actions";

    // The unique identifier for this category.
    public string identifier;
    public List<UNNotificationCategoryOptions> options;
    public List<UNNotificationAction> actions;

    public Dictionary<string, object> toDict()
    {
        List<string> categoryOptions = new List<string>();
        if(0 < options.Count) {
            for (int i = 0; i < options.Count; i++) {
                categoryOptions.Add (ExtractOptionFromEnum (options [i]));
            }
        }

        Dictionary<string, object> retval = new Dictionary<string, object> ();
        retval [IDENTIFIER_KEY] = identifier;
        retval [OPTIONS_KEY] = categoryOptions;
        retval [ACTIONS_KEY] = actions.Select(a => a.toDict()).ToList();
        return retval;
    }

    private string ExtractOptionFromEnum (UNNotificationCategoryOptions option)
    {
        switch (option) {
        case UNNotificationCategoryOptions.UNNotificationCategoryOptionCustomDismissAction:
            return @"custom_dismiss";
        case UNNotificationCategoryOptions.UNNotificationCategoryOptionAllowInCarPlay:
            return @"carplay";
        case UNNotificationCategoryOptions.UNNotificationCategoryOptionNone:
        default:
            return @"";
        }
    }
}
