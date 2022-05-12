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
        public Dictionary<SwrveWidget, string> DynamicImageResolution = new Dictionary<SwrveWidget, string>();

        /// <summary>
        /// Check the validity of personalization against a SwrveInAppCampaign
        /// </summary>
        public bool ResolveTemplating(SwrveInAppCampaign campaign, Dictionary<string, string> properties)
        {
            if (!ResolveTextTemplating(campaign.Message, properties))
            {
                return false;
            }

            if (!ResolveDynamicImageTemplating(campaign.Message, properties))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check the validity of personalization against a SwrveMessage
        /// </summary>
        public bool ResolveTemplating(SwrveMessage message, Dictionary<string, string> properties)
        {
            if (!ResolveTextTemplating(message, properties))
            {
                return false;
            }

            if (!ResolveDynamicImageTemplating(message, properties))
            {
                return false;
            }

            return true;
        }

        protected bool ResolveDynamicImageTemplating(SwrveMessage message, Dictionary<string, string> properties)
        {
            for (int fi = 0; fi < message.Formats.Count; fi++)
            {
                SwrveMessageFormat format = message.Formats[fi];
                var pagesKeys = format.Pages.Keys;
                foreach (long key in pagesKeys)
                {
                    SwrveMessagePage page = format.Pages[key];
                    for (int ii = 0; ii < page.Images.Count; ii++)
                    {
                        SwrveImage image = page.Images[ii];
                        if (image.DynamicImageUrl != null)
                        {
                            if (!ResolveWidgetPropertyWithDynamicUrl(image, DynamicImageResolution, image.DynamicImageUrl, properties))
                            {
                                if (image.File == null)
                                {
                                    // there is no image fallback, we cannot display this now
                                    return false;
                                }
                            }
                        }
                    }

                    for (int bi = 0; bi < page.Buttons.Count; bi++)
                    {
                        SwrveButton button = page.Buttons[bi];
                        if (button.DynamicImageUrl != null)
                        {
                            if (!ResolveWidgetPropertyWithDynamicUrl(button, DynamicImageResolution, button.DynamicImageUrl, properties))
                            {
                                if (button.Image == null)
                                {
                                    // there is no button image fallback, we cannot display this now
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        protected bool ResolveTextTemplating(SwrveMessage message, Dictionary<string, string> properties)
        {
            try
            {
                for (int fi = 0; fi < message.Formats.Count; fi++)
                {
                    SwrveMessageFormat format = message.Formats[fi];
                    var pagesKeys = format.Pages.Keys;
                    foreach (long key in pagesKeys)
                    {
                        SwrveMessagePage page = format.Pages[key];
                        for (int ii = 0; ii < page.Images.Count; ii++)
                        {
                            SwrveImage image = page.Images[ii];

                            if (!ResolveWidgetProperty(image, TextResolution, image.Text, properties))
                            {
                                return false;
                            }
                        }

                        for (int bi = 0; bi < page.Buttons.Count; bi++)
                        {
                            SwrveButton button = page.Buttons[bi];

                            if (!ResolveWidgetProperty(button, TextResolution, button.Text, properties))
                            {
                                return false;
                            }

                            // Need to personalize action
                            string personalizedButtonAction = button.Action;
                            if ((button.ActionType == SwrveActionType.Custom || button.ActionType == SwrveActionType.CopyToClipboard) && !string.IsNullOrEmpty(personalizedButtonAction))
                            {
                                if (!ResolveWidgetProperty(button, ActionResolution, personalizedButtonAction, properties))
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                ActionResolution[button] = personalizedButtonAction;
                            }
                        }
                    }
                }
            }
            catch (SwrveSDKTextTemplatingException exp)
            {
                SwrveLog.LogInfo("Not showing campaign, error with personalization" + exp.Message);
                return false;
            }
            return true;
        }

        private bool ResolveWidgetProperty(SwrveWidget widget, Dictionary<SwrveWidget, string> cacheDest, string property, Dictionary<string, string> properties)
        {
            if (!string.IsNullOrEmpty(property))
            {
                // Need to render dynamic text
                string personalizedText = SwrveTextTemplating.Apply(property, properties);
                if (string.IsNullOrEmpty(personalizedText))
                {
                    SwrveLog.LogInfo("Text template could not be resolved: " + property + " in given properties.");
                    return false;
                }
                else if (SwrveTextTemplating.HasPatternMatch(personalizedText))
                {
                    SwrveLog.LogInfo("Not showing campaign with personalization outside of Message Center / without personalization info provided.");
                    return false;
                }
                cacheDest[widget] = personalizedText;
            }

            return true;
        }

        private bool ResolveWidgetPropertyWithDynamicUrl(SwrveWidget widget, Dictionary<SwrveWidget, string> cacheDest, string property, Dictionary<string, string> properties)
        {
            try
            {
                return ResolveWidgetProperty(widget, cacheDest, property, properties);
            }
            catch (SwrveSDKTextTemplatingException exp)
            {
                SwrveLog.LogInfo("Not showing campaign, error with personalization" + exp.Message);
                return false;
            }
        }
    }
}
