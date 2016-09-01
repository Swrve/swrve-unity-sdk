using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using SwrveUnity.Messaging;

/// <summary>
/// Swrve SDK demo.
/// </summary>
using SwrveUnity.IAP;


public class DemoGUI : BaseDemoGUI
{
    /// Enable or disable the game UI.
    public bool UIEnabled = true;

    /// Reference to the Swrve Component in the scene.
    private SwrveComponent swrveComponent;

    void Start ()
    {
        swrveComponent = (SwrveComponent)FindObjectOfType (typeof(SwrveComponent));

        // In-app messaging setup
        swrveComponent.SDK.GlobalMessageListener = new CustomMessageListener (this);
        swrveComponent.SDK.GlobalCustomButtonListener = new CustomButtonListener ();

#if UNITY_5
        Debug.LogWarning("Consider using the UnitySwrveDemo5xx");
#endif
    }

    void Update ()
    {
        if (UIEnabled) {
            if (buttonPressed [(int)Buttons.SendEvent]) {
                // Trigger a custom event
                swrveComponent.SDK.NamedEvent (@"button pressed", new Dictionary<string, string> () {
                    { "foo", "bar"
                    }
                });
            }

            if (buttonPressed [(int)Buttons.SendUserAttributes]) {
                // Update a user property
                swrveComponent.SDK.UserUpdate (new Dictionary<string, string> () {
                    { "health", "100"}, { "gold", "20" }
                });
            }

            if (buttonPressed [(int)Buttons.PurchaseItem]) {
                // Notify of an item purchase
                swrveComponent.SDK.Purchase (@"someItem", @"gold", 20, 1);
            }

            if (buttonPressed [(int)Buttons.InAppItemPurchase]) {
                // Notify of an in-app purchase
                swrveComponent.SDK.Iap (1, @"productId", 1.99, @"USD");
            }

            if (buttonPressed [(int)Buttons.InAppCurrencyPurchase]) {
                // Nofity of an in-app purchase with a some currency reward
                IapRewards rewards = new IapRewards (@"gold", 200);
                swrveComponent.SDK.Iap (1, @"productId", 0.99, @"USD", rewards);
            }

            if (buttonPressed [(int)Buttons.RealIap]) {
                IapRewards rewards = new IapRewards (@"gold", 100);
                rewards.AddCurrency (@"keys", 5);
                rewards.AddItem (@"sword", 1);
#if UNITY_IPHONE
                // IAP validation happens on our servers. Provide if possible the receipt from Apple.
                IapReceipt receipt = RawReceipt.FromString("receipt-from-apple");
                swrveComponent.SDK.IapApple (1, @"productId", 4.99, @"EUR", rewards, receipt);
#elif UNITY_ANDROID
                // IAP validation happens on our servers. Provide if possible the purchase data from Google.
                string purchaseData = "purchase-data-from-google-play";
                string dataSignature = "data-signature-from-google-play";
                swrveComponent.SDK.IapGooglePlay (@"productId", 4.99, @"EUR", rewards, purchaseData, dataSignature);
#endif
            }

            if (buttonPressed [(int)Buttons.CurrencyGiven]) {
                // Notify of currency given
                swrveComponent.SDK.CurrencyGiven (@"gold", 20);
            }

            if (buttonPressed [(int)Buttons.UserResources]) {
                // Obtain the latest value of the resource item01.attribute or its default value
                int attributeValue = swrveComponent.SDK.ResourceManager.GetResourceAttribute<int> ("item01", "attribute", 99);
                UnityEngine.Debug.Log ("User resource attribute: " + attributeValue);
            }

            if (buttonPressed [(int)Buttons.SendToSwrve]) {
                // Send the queued events in the buffer to Swrve
                swrveComponent.SDK.SendQueuedEvents ();
            }

            if (buttonPressed [(int)Buttons.TriggerMessage]) {
                // Trigger an in-app message. You will need to setup the campaign
                // in the In-App message section in the dashboard.
                swrveComponent.SDK.NamedEvent ("campaign_trigger");
            }

            if (buttonPressed [(int)Buttons.SaveToDisk]) {
                // Flush the queued events to disk
                swrveComponent.SDK.FlushToDisk ();
            }
        }

        base.ClearButtons ();
    }

    /// <summary>
    /// Process in-app message custom button clicks.
    /// </summary>
    private class CustomButtonListener : ISwrveCustomButtonListener
    {
        public void OnAction (string customAction)
        {
            // Custom button logic
            UnityEngine.Debug.Log ("Custom action triggered " + customAction);
        }
    }

    /// <summary>
    /// Observe the SDK for in-app messages and pause/resume your game.
    /// </summary>
    private class CustomMessageListener : ISwrveMessageListener
    {
        private DemoGUI container;

        public CustomMessageListener (DemoGUI container)
        {
            this.container = container;
        }

        public void OnShow (SwrveMessageFormat format)
        {
            // Pause game
            container.UIEnabled = false;
        }

        public void OnShowing (SwrveMessageFormat format)
        {
        }

        public void OnDismiss (SwrveMessageFormat format)
        {
            // Resume game
            container.UIEnabled = true;
        }
    }
}
