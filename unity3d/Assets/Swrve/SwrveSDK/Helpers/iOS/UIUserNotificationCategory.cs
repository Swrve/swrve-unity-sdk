using System.Collections.Generic;
using System.Linq;

public enum UIUserNotificationActionContext {
    UIUserNotificationActionContextDefault,  // the default context of a notification action
    UIUserNotificationActionContextMinimal   // the context of a notification action when space is limited
}

public enum UIUserNotificationActionBehavior {
    UIUserNotificationActionBehaviorDefault,        // the default action behavior
    UIUserNotificationActionBehaviorTextInput       // system provided action behavior, allows text input from the user
}

public enum UIUserNotificationActivationMode {
    UIUserNotificationActivationModeForeground, // activates the application in the foreground
    UIUserNotificationActivationModeBackground  // activates the application in the background, unless it's already in the foreground
}

[System.Serializable]
public class UIUserNotificationAction
{
    const string IDENTIFIER_KEY = "identifier";
    const string TITLE_KEY = "title";
    const string BEHAVIOUR_KEY = "behaviour";
    const string ACTIVATION_MODE_KEY = "activationMode";
    const string AUTHENTICATION_REQUIRED_KEY = "authenticationRequired";
    const string DESTRUCTIVE_KEY = "destructive";

    // The unique identifier for this action.
    public string identifier;

    // The localized title to display for this action.
    public string title;

    // The behavior of this action when the user activates it.
    public UIUserNotificationActionBehavior behaviour;

    // Parameters that can be used by some types of actions.
    // public Dictionary<string, object> parameters;

    // How the application should be activated in response to the action.
    public UIUserNotificationActivationMode activationMode;

    // Whether this action is secure and should require unlocking before being performed.
    // If the activation mode is UIUserNotificationActivationModeForeground, then the action
    // is considered secure and this property is ignored.
    public bool authenticationRequired;

    // Whether this action should be indicated as destructive when displayed.
    public bool destructive;

    public Dictionary<string, object> toDict()
    {
        return new Dictionary<string, object> {
            {IDENTIFIER_KEY, identifier},
            {TITLE_KEY, title},
            {BEHAVIOUR_KEY, (int)behaviour},
            {ACTIVATION_MODE_KEY, (int)activationMode},
            {AUTHENTICATION_REQUIRED_KEY, authenticationRequired},
            {DESTRUCTIVE_KEY, destructive}
        };
    }
}

[System.Serializable]
public class UIUserNotificationCategory
{
    const string IDENTIFIER_KEY = "identifier";
    const string CONTEXT_ACTIONS_KEY = "contextActions";

    // The unique identifier for this category.
    public string identifier;

    public List<UIUserNotificationAction> defaultContextActions;
    public List<UIUserNotificationAction> minimalContextActions;

    public Dictionary<string, object> toDict()
    {
        Dictionary<UIUserNotificationActionContext, List<UIUserNotificationAction>> contextActions =
            new Dictionary<UIUserNotificationActionContext, List<UIUserNotificationAction>>();

        if (0 < defaultContextActions.Count) {
            contextActions [UIUserNotificationActionContext.UIUserNotificationActionContextDefault] = defaultContextActions;
        }
        if (0 < minimalContextActions.Count) {
            contextActions [UIUserNotificationActionContext.UIUserNotificationActionContextMinimal] = minimalContextActions;
        }

        Dictionary<string, object> retval = new Dictionary<string, object> ();
        if(0 < contextActions.Keys.Count) {
            retval [IDENTIFIER_KEY] = identifier;
            retval [CONTEXT_ACTIONS_KEY] = contextActions.ToDictionary (x => (int)x.Key, y => y.Value.Select (a => a.toDict ()).ToList ());
        }
        return retval;
    }
}
