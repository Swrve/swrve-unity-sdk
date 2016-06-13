#if UNITY_5

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Swrve.IAP;
using System.Linq;

public class MainMenuComponent : MonoBehaviour {
    public string eventTriggerName;

    public Transform panel;

  	void Start () {
        new Dictionary<string, UnityAction> {
            {"Named Event", SendEvent},
            {"User update", SendUserAttributes},
            {"Purchase Item", PurchaseItem},
            {"IAP: Item", InAppItemPurchase},
            {"IAP: Virtual Currency", InAppCurrencyPurchase},
            {"Apple IAP", RealIap},
            {"Currency Given", CurrencyGiven},
            {"AB Test Resources", UserResources},
            {"Send To Swrve", SendToSwrve},
            {"Trigger Message", TriggerMessage},
            {"Save To Disk", SaveToDisk}
        }.ToList().ForEach(kvp => {
            HomeMenuComponent.SetButton (kvp, panel);
        });
    }

    void SendEvent() {
        // Trigger a custom event
        SwrveComponent.SDK.NamedEvent (@"button pressed", new Dictionary<string, string> () {
            { "foo", "bar" }
        });
    }
    void SendUserAttributes() {
        // Update a user property
        SwrveComponent.SDK.UserUpdate (new Dictionary<string, string> () {
            { "health", "100"}, { "gold", "20" }
        });
    }

    void PurchaseItem() {
        // Notify of an item purchase
        SwrveComponent.SDK.Purchase (@"someItem", @"gold", 20, 1);
    }

    void InAppItemPurchase() {
        // Notify of an in-app purchase
        SwrveComponent.SDK.Iap (1, @"productId", 1.99, @"USD");
    }

    void InAppCurrencyPurchase() {
        // Nofity of an in-app purchase with a some currency reward
        IapRewards rewards = new IapRewards (@"gold", 200);
        SwrveComponent.SDK.Iap (1, @"productId", 0.99, @"USD", rewards);
    }

    void RealIap() {
        IapRewards rewards = new IapRewards (@"gold", 100);
        rewards.AddCurrency (@"keys", 5);
        rewards.AddItem (@"sword", 1);
#if UNITY_IPHONE
        // IAP validation happens on our servers. Provide if possible the receipt from Apple.
        IapReceipt receipt = RawReceipt.FromString("receipt-from-apple");
        SwrveComponent.SDK.IapApple (1, @"productId", 4.99, @"EUR", rewards, receipt);
#elif UNITY_ANDROID
        // IAP validation happens on our servers. Provide if possible the purchase data from Google.
        string purchaseData = "purchase-data-from-google-play";
        string dataSignature = "data-signature-from-google-play";
        SwrveComponent.SDK.IapGooglePlay (@"productId", 4.99, @"EUR", rewards, purchaseData, dataSignature);
#endif
    }

    void CurrencyGiven() {
        // Notify of currency given
        SwrveComponent.SDK.CurrencyGiven (@"gold", 20);
    }

    void UserResources() {
        // Obtain the latest value of the resource item01.attribute or its default value
        int attributeValue = SwrveComponent.SDK.ResourceManager.GetResourceAttribute<int> ("item01", "attribute", 99);
        UnityEngine.Debug.Log ("User resource attribute: " + attributeValue);
    }

    void SendToSwrve() {
        // Send the queued events in the buffer to Swrve
        SwrveComponent.SDK.SendQueuedEvents ();
    }

    void TriggerMessage() {
        // Trigger an in-app message. You will need to setup the campaign
        // in the In-App message section in the dashboard.
        if (!string.IsNullOrEmpty (eventTriggerName)) {
            SwrveComponent.SDK.NamedEvent (eventTriggerName);
        }
    }

    void SaveToDisk() {
        // Flush the queued events to disk
        SwrveComponent.SDK.FlushToDisk ();
    }
}

#endif
