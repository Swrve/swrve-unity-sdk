using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using SwrveUnity.Helpers;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Check the validity of all message formats with the given personalization before displaying the message.
/// This class also caches the values for each UI element.
/// </summary>
public class SwrveMessageTextTemplatingResolver
{
    public Dictionary<SwrveWidget, string> TextResolution = new Dictionary<SwrveWidget, string>();
    public Dictionary<SwrveWidget, string> ActionResolution = new Dictionary<SwrveWidget, string>();

    public bool ResolveTemplating(SwrveInAppCampaign campaign, Dictionary<string, string> properties)
    {
        for (int i = 0; i < campaign.Messages.Count; i++) {
            if (!ResolveTemplating(campaign.Messages[i], properties)) {
                return false;
            }
        }

        return true;
    }

    public bool ResolveTemplating(SwrveMessage message, Dictionary<string, string> properties)
    {
        try {
            for (int fi = 0; fi < message.Formats.Count; fi++) {
                SwrveMessageFormat format = message.Formats[fi];
                for (int ii = 0; ii < format.Images.Count; ii++) {
                    SwrveImage image = format.Images[ii];

                    if (!ResolveWidgetProperty(image, TextResolution, image.Text, properties)) {
                        return false;
                    }
                }

                for (int bi = 0; bi < format.Buttons.Count; bi++) {
                    SwrveButton button = format.Buttons[bi];

                    if (!ResolveWidgetProperty(button, TextResolution, button.Text, properties)) {
                        return false;
                    }

                    // Need to personalize action
                    string personalizedButtonAction = button.Action;
                    if ((button.ActionType == SwrveActionType.Custom || button.ActionType == SwrveActionType.CopyToClipboard) && !string.IsNullOrEmpty(personalizedButtonAction)) {
                        if (!ResolveWidgetProperty(button, ActionResolution, personalizedButtonAction, properties)) {
                            return false;
                        }
                    } else {
                        ActionResolution[button] = personalizedButtonAction;
                    }
                }
            }
        } catch(SwrveSDKTextTemplatingException exp) {
            UnityEngine.Debug.LogError("Not showing campaign, error with personalization" + exp.Message);
            return false;
        }
        return true;
    }

    private bool ResolveWidgetProperty(SwrveWidget widget, Dictionary<SwrveWidget, string> cacheDest, string property, Dictionary<string, string> properties)
    {
        if (!string.IsNullOrEmpty(property)) {
            // Need to render dynamic text
            string personalizedText = SwrveTextTemplating.Apply(property, properties);
            if (string.IsNullOrEmpty(personalizedText)) {
                UnityEngine.Debug.Log("Text template could not be resolved: " + property + " in given properties.");
                return false;
            } else if (SwrveTextTemplating.HasPatternMatch(personalizedText)) {
                UnityEngine.Debug.Log("Not showing campaign with personalization outside of Message Center / without personalization info provided.");
                return false;
            }
            cacheDest[widget] = personalizedText;
        }

        return true;
    }
}
}
