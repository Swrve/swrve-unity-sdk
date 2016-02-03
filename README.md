Swrve
=
Swrve is a single integrated platform delivering everything you need to drive mobile engagement and create valuable consumer relationships on mobile.

This Unity3D SDK will enable your app to use all of these features.

Table of Contents
-

- [Getting started](#getting-started)
  - [Requirements](#requirements)
    - [Unity 4.0.0+](#unity-400)
    - [Unity 5+ Personal or Unity 4.0.0+ Pro](#unity-5-personal-or-unity-400-pro)
- [Installation Instructions](#installation-instructions)
  - [In-app Messaging](#in-app-messaging)
    - [In-app Messaging Deeplinks](#in-app-messaging-deeplinks)
  - [iOS Push Notifications](#ios-push-notifications)
    - [Creating and uploading your iOS Cert](#creating-and-uploading-your-ios-cert)
    - [Provisioning your app](#provisioning-your-app)
  - [Android Push Notifications](#android-push-notifications)
    - [Using a custom `GcmReceiver`](#using-a-custom-gcmreceiver)
    - [Custom `MainActivity`](#custom-mainactivity)
    - [No custom `MainActivity`](#no-custom-mainactivity)
    - [Advanced Android Push](#advanced-android-push)
  - [Sending Events](#sending-events)
    - [Sending Named Events](#sending-named-events)
    - [Send User Properties](#send-user-properties)
    - [Sending Virtual Economy Events](#sending-virtual-economy-events)
    - [Sending IAP Events and IAP Validation](#sending-iap-events-and-iap-validation)
  - [Integrating Resource A/B Testing](#integrating-resource-ab-testing)
  - [Testing your integration](#testing-your-integration)
- [Upgrade Instructions](#upgrade-instructions)
- [How to run the demo](#how-to-run-the-demo)
- [How to build the SDK](#how-to-build-the-sdk)
- [Contributing](#contributing)
- [License](#license)


Getting started
=

Requirements
-
Swrve supports Unity version 4.0.0 or later on Android and iOS only. **Swrve Unity SDK is not currently supported on Unity Web Player or Windows 8.**

### Unity 4.0.0+
The SDK needs a Unity version higher than 4.0.0 to be able to compile.

### Unity 5+ Personal or Unity 4.0.0+ Pro
Some of the features like push notifications require the use of native plugins. Native plugins are now available in Unity Personal 5+ or previvous versions of Unity Pro.

Installation Instructions
=
1. Download the [latest release](https://github.com/Swrve/swrve-unity-sdk/releases/latest)

2. Unzip the Unity SDK, import the `Swrve.unityPackage` into your project and add the `SwrvePrefab` component to your scene.

  ![Import swrve.unityPackage](/docs/images/name12.jpg)

  ![Add Prefab to Scene](/docs/images/name13.jpg)

3. Initialize the Swrve Component

  There are two methods to initialize the SDK component. Automatic initialization and manual initialization. In almost all cases, you should use manual initialization and ensure that the "initialise on start" property is not set in the Swrve prefab.

  Set the `app_id` and `api_key` fields of the component to your Swrve App ID and Swrve API Key.

  ```
  // By default, Swrve stores all customer data and content in our US data center. 
  //SwrveComponent.Instance.Config.SelectedStack = Swrve.Stack.EU;

  #if PRODUCTION_BUILD
  SwrveComponent.Instance.Init(<production_app_id>, "<production_api_key>");
  #else
  SwrveComponent.Instance.Init(<sandbox_app_id>, "<sandbox_api_key>");
  #endif //PRODUCTION BUILD
  ```

  You can now compile and run your project in Unity.


4. Go to Edit > Project Settings > Script Execution Order and move `SwrveComponent` to the highest priority in the list. This prevents you from getting a `NullReferenceException` that can occur if Swrve is called before the SDK has finished initializing.

In-app Messaging
-

Integrate the in-app messaging functionality so you can use Swrve to send personalized messages to your app users while they’re using your app. If you’d like to find out more about in-app messaging, see [Intro to In-App Messages](http://docs.swrve.com/user-documentation/in-app-messaging/intro-to-in-app-messages/).

Before you can test the in-app message feature in your game, you need to create an In App Message campaign in the Swrve Dashboard.

Unity has no understanding of an active window state, therefore you **must** include code to pause the app when an in-app message is displayed and resume it as soon as it is closed.

```
class CustomMessageListener : ISwrveMessageListener {
   public void OnShow (SwrveMessageFormat format) {
      // Pause app, disable clicks on other UI elements

      // Optionally: custom message display (for example: transparent background)
      // format.Message.BackgroundAlpha = 0f;
   }
   public void OnShowing (SwrveMessageFormat format) {
       // Message displaying, UI elements must continue to be disabled
   }
   public void OnDismiss (SwrveMessageFormat format) {
      // Resume app and clicks in other UI elements
   }
}
SwrveComponent.Instance.SDK.GlobalMessageListener = new CustomMessageListener ();
```

### In-app Messaging Deeplinks ###

When creating in-app messages in Swrve, you **must** configure message buttons to direct users to perform a custom action when clicked. For example, you might configure a button to direct the app user straight to your app store. To enable this feature, you must configure deeplinks by performing the actions outlined below.

If you would like to process the custom action completely on your own, you must add a custom listener to the Swrve SDK before its initialization. There is no default action for deeplinks in the Unity SDK.

```
private class CustomButtonListener : ISwrveCustomButtonListener {
   public void OnAction (string customAction) {
      // Custom button logic
   }
}
SwrveComponent.Instance.SDK.GlobalCustomButtonListener = new CustomButtonListener();
```

For example, if you have already implemented deeplinks for your app, it might make sense to use that for handling in-app messages custom actions

```
private class CustomButtonListener : MonoBehaviour, ISwrveCustomButtonListener
{
    private IEnumerator DelayedNamedEvent(string eventName, Dictionary<string, string> payload = null) {
        yield return new WaitForEndOfFrame();
        SwrveComponent.Instance.SDK.NamedEvent(eventName, payload);
    }

    private void HandleDeeplink(string deeplink) {
        try {
            Uri uri = new Uri (deeplink);
            if (uri.Scheme == "swrve") {
                NameValueCollection query = new NameValueCollection();
                foreach (string vp in Regex.Split(uri.Query.TrimStart ('?'), "&")) {
                    string[] singlePair = Regex.Split(vp, "=");
                    if (singlePair.Length == 2) {
                        query.Add(singlePair[0], singlePair[1]);
                    }
                    else {
                        query.Add(singlePair[0], string.Empty);
                    }
                }

                string eventString = query.Get("event");
                if (!string.IsNullOrEmpty(eventString)) {
                    StartCoroutine(DelayedNamedEvent(eventString));
                }
            }
            else {
                Application.OpenURL(deeplink);
            }
        }
        catch(UriFormatException exception) {
            // log bad uri
        }
    }

    public void OnAction (string customAction) {
        // Custom button logic
        HandleDeeplink(customAction);
    }
}
```

iOS Push Notifications
-

1. Enable push notifications in the SwrveConfig object:

  ```
  #if UNITY_IPHONE
  SwrveComponent.Instance.Config.PushNotificationEnabled = true;
  #endif
  ```

2. Time the push permission request.

  To request permission upon the triggering of certain events, include the following code:

  Changes to `SwrveComponent.Instance.Config` must occur before calling `SwrveComponent.Instance.Init`.

  ```
  using System.Collections.Generic;

  HashSet<string> pushNotificationEvents = new HashSet<string>();
  pushNotificationEvents.Add("custom_event");

  SwrveComponent.Instance.Config.PushNotificationEvents = pushNotificationEvents;
  ```

  To manage the exact timing for requesting permission to send push notifications, use the following code:

  ```
  SwrveComponent.Instance.SDK.NamedEvent("Swrve.push_notification_permission");
  ```

3. If you want to perform custom processing of the push notification payload, create the following class and add this code to the SDK initialization:

  ```
  using Swrve;


  public class CustomPushNotificationListener : ISwrvePushNotificationListener {
  #if UNITY_IPHONE
    public void OnRemoteNotification(UnityEngine.iOS.RemoteNotification notification) {
      // CUSTOM CODE HERE
    }
  #endif // UNITY_IPHONE
  }
  SwrveComponent.Instance.SDK.PushNotificationListener = new CustomPushNotificationListener();
  ```

  For example, if you have already implemented deeplinks for your app, it might make sense to that for handling Push Notification actions:

  ```
  public class CustomPushNotificationListener : ISwrvePushNotificationListener {
  #if UNITY_IPHONE
    public void OnRemoteNotification(UnityEngine.iOS.RemoteNotification notification)
    {
        if (notification.userInfo.Contains("deeplink"))
        {
            string deeplinkUrl = notification.userInfo["deeplink"] as string;
            HandleDeeplink(deeplinkUrl);
        }
    }
  #endif // UNITY_IPHONE
  }
  SwrveComponent.Instance.SDK.PushNotificationListener = new CustomPushNotificationListener();
  ```


### Creating and uploading your iOS Cert ###

To enable your app to send push notifications to Unity iOS devices, you require a push certificate. A push certificate authorizes an app to receive push notifications and authorizes a service to send push notifications to an app. For more information, see [How Do I Manage iOS Push Certificates?](http://docs.swrve.com/faqs/push-notifications/manage-ios-push-certificates-for-push-notifications/)

### Provisioning your app ###

Each time your app is published for distribution, you must provision it against the push certificate that has been created for the app.

Android Push Notifications
-

Unity Android uses a native Android plugin (provided as part of the Unity SDK), so you must include this plugin in your project as detailed below.

1. Copy the JAR files located under `plugins/SwrveGooglePlayPushNotification` into `Assets/Plugins/Android`.

2. Copy the `AndroidManifest.xml` file (in `plugins/SwrveGooglePlayPushNotification`) into `Assets/Plugins/Android` and replace the sample package name `com.example.gcm` with the package name for your app.

  If you are already providing a custom `AndroidManifest.xml` or your project generates a different XML from the sample one, you can make a copy from the Staging folder once you make your first Android player build from Unity. Then include the required permissions, BroadcastListener, service and custom activities.

3. Initializing the SDK. You must provide the project number obtained from the Google Developer Console to the SDK on initialization.

  ```
  #if UNITY_ANDROID
  SwrveComponent.Instance.Config.PushNotificationEnabled = true;
  SwrveComponent.Instance.Config.GCMSenderId = SENDER_ID;
  SwrveComponent.Instance.Config.GCMPushNotificationTitle = "Awesome app";
  #endif // UNITY_ANDROID
  ```

  Changes to `SwrveComponent.Instance.Config` must occur before calling `SwrveComponent.Instance.Init`.

### Using a custom `GcmReceiver` ###

If you are already using push notifications, follow the steps below to integrate Swrve’s push notification functionality:

1. Copy `swrvesdkpushsupport.jar` (located under `plugins/SwrveGooglePlayPushNotification`) into `Assets/Plugins/Android`.

2. Add the following lines to the zzd method of your custom `GcmReceiver`:

  ```
  @Override
  public void zzd(Context context, Intent intent) {
     ...
     // Call the Swrve intent service if the push contains the Swrve payload _p
     if("com.google.android.c2dm.intent.RECEIVE".equals(intent.getAction())) {
       Bundle extras = intent.getExtras();
       if (extras != null) {
         Object rawId = extras.get("_p");
         String msgId = (rawId != null) ? rawId.toString() : null;
         if (!SwrveHelper.isNullOrEmpty(msgId)) {
           // It is a Swrve push!
           ComponentName comp = new ComponentName(context.getPackageName(), com.swrve.unity.gcm.SwrveGcmIntentService.class.getName());
           intent = intent.setComponent(comp);
         }
       }
     }
     super.zzd(context, intent);
     ...
  }
  ```

### Custom `MainActivity` ###

If you have a custom `MainActivity`, call the following functions `onCreate` and `onResume`:

```
public class CustomMainActivity extends UnityPlayerActivity {
   @Override
   protected void onCreate(Bundle arg0) {
      super.onCreate(arg0);
      com.swrve.unity.gcm.MainActivity.processIntent(getApplicationContext(), getIntent());
   }

   @Override
   protected void onResume() {
      super.onResume();
      com.swrve.unity.gcm.MainActivity.processIntent(getApplicationContext(), getIntent());
   }
}
```

### No custom `MainActivity` ###

If you don’t provide a custom `MainActivity`, point to Swrve’s `MainActivity` in your `AndroidManifest.xml`:


```
<activity android:name="com.swrve.unity.gcm.MainActivity" android:label="@string/app_name" android:configChanges="fontScale|keyboard|keyboardHidden|locale|mnc|mcc|navigation|orientation|screenLayout|screenSize|smallestScreenSize|uiMode|touchscreen" android:screenOrientation="portrait">
  <intent-filter>
    <action android:name="android.intent.action.MAIN" />
    <category android:name="android.intent.category.LAUNCHER" />
  </intent-filter>
</activity>
```

### Advanced Android Push ###

This section describes how to configure advanced options for Unity SDK push notification integration.

* **Using custom icons**

  To change the icon for push notifications and your app, place a file called `app_icon.png` under `Assets/Plugins/Android/res/drawable/`. You can also provide multiple resolutions for the same asset. Please consult the Android documentation on how to do so.

  You can also specify the resource to be used for the normal and Material icon with the following configuration properties:

  ```
  #if UNITY_ANDROID
  // Optional configuration
  SwrveComponent.Instance.Config.GCMPushNotificationIconId = "ic_launcher";
  SwrveComponent.Instance.Config.GCMPushNotificationMaterialIconId = "ic_launcher_material";
  SwrveComponent.Instance.Config.GCMPushNotificationLargeIconId = "ic_launcher_material";
  SwrveComponent.Instance.Config.GCMPushNotificationAccentColor = 0; // Black color
  #endif // UNITY_ANDROID
  ```

* **Using custom sounds**

  You can send push notifications with custom sounds. To do so, place your custom sound under `Assets/Plugins/Android/res/raw` and set your sounds in the Swrve service. For more information about adding custom sounds in Swrve, see [Intro to Push Notifications](http://docs.swrve.com/user-documentation/push-notifications/intro-to-push-notifications/).

* **Processing custom payloads**

  To process notifications when they are received or when they open your app, create a new class in your project with the following content:

  ```
  public class CustomPushNotificationListener : ISwrvePushNotificationListener {
  #if UNITY_ANDROID
    public void OnNotificationReceived(Dictionary<string, object> notificationJson) {
      // Code to process received push notifications
    }

    public void OnOpenedFromPushNotification(Dictionary<string, object> notificationJson) {
      // Code to process opened push notifications
    }
  #endif // UNITY_ANDROID
  }
  SwrveComponent.Instance.SDK.PushNotificationListener = new CustomPushNotificationListener();
  ```

  For example, if you have already implemented deeplinks for your app, it might make sense to that for handling Push Notification actions:

  ```
  public class CustomPushNotificationListener : ISwrvePushNotificationListener {
  #if UNITY_ANDROID
      public void OnNotificationReceived(Dictionary<string, object> notificationJson) {
          // this *should* be called when the notification is received, whether the app be open or not
      }

      public void OnOpenedFromPushNotification(Dictionary<string, object> notificationJson) {
          if (notificationJson.ContainsKey("deeplink")) {
              string deeplinkUrl = notificationJson["deeplink"] as string;
              HandleDeeplink(deeplinkUrl);
          }
      }
  #endif // UNITY_ANDROID
  }
  SwrveComponent.Instance.SDK.PushNotificationListener = new CustomPushNotificationListener();
  ```

Sending Events
-

### Sending Named Events ###

```
SwrveComponent.Instance.SDK.NamedEvent("custom.event_name");
```

Rules for sending events:

* Do not send the same named event in differing case.

* Use '.'s in your event name to organize their layout in the Swrve dashboard. Each '.' creates a new tree in the UI which groups your events so they are easy to locate.
* Do not send more than 1000 unique named events.
 * Do not add unique identifiers to event names. For example, Tutorial.Start.ServerID-ABDCEFG
 * Do not add timestamps to event names. For example, Tutorial.Start.1454458885
* When creating custom events, do not use the `swrve.*` or `Swrve.*` namespace for your own events. This is reserved for Swrve use only. Custom event names beginning with `Swrve.` are restricted and cannot be sent.

### Event Payloads ###

An event payload can be added and sent with every event. This allows for more detailed reporting around events and funnels. The associated payload should be a dictionary of key/value pairs; it is restricted to string and integer keys and values. There is a maximum cardinality of 500 key-value pairs for this payload per event. This parameter is optional.

```
Dictionary<string,string> payload = new Dictionary<string,string>() {
  {"key1", "value1"},
  {"key2", "value2"}
};
SwrveComponent.Instance.SDK.NamedEvent("custom.event_name", payload);
```

For example, if you want to track when a user starts the tutorial experience it might make sense to send an event `tutorial.start` and add a payload `time` which captures how long the user spent starting the tutorial.
```
Dictionary<string, string> payload = new Dictionary<string, string>();
payload.Add("time", "100");
SwrveComponent.Instance.SDK.NamedEvent("tutorial.start", payload);
```


### Send User Properties ###

Assign user properties to send the status of the user. For example create a custom user property called `premium`, and then target non-premium users and premium users in the dashboard..

```
Dictionary<string, string> attributes = new Dictionary<string, string>();
attributes.Add("premium", "true");
attributes.Add("level", "12");
attributes.Add("balance", "999");
SwrveComponent.Instance.SDK.UserUpdate(attributes);
```

### Sending Virtual Economy Events ###

If your app has a virtual economy, send the purchase event when users purchase in-app items with virtual currency.

```
string item = "some.item";
string currency = "gold";
int cost = 99;
int quantity = 1;
SwrveComponent.Instance.SDK.Purchase(item, currency, cost, quantity);
```

Send the currency given event when you give users virtual currency. Examples include initial currency balances, retention bonuses and level-complete rewards.

```
string givenCurrency = "gold";
double givenAmount = 99;
SwrveComponent.Instance.SDK.CurrencyGiven(givenCurrency, givenAmount);
```

To ensure virtual currency events are not ignored by the server, make sure the currency name configured in your app matches exactly the Currency Name you enter in the App Currencies section on the App Settings screen (including case-sensitive). If there is any difference, or if you haven’t added the currency in Swrve, the event will be ignored and return an error event called Swrve.error.invalid_currency. Additionally, the ignored events will not be included in your KPI reports. For more information, see [Add Your App](http://docs.swrve.com/getting-started/add-your-app/).

### Sending IAP Events and IAP Validation ###

If your app has in-app purchases, send the IAP event when a user purchases something with real money. The IAP event enables Swrve to build revenue reports for your app and to track the spending habits of your users.

* **In iOS 7+** devices, Apple returns a receipt including multiple purchases. To identify what was  purchased, Swrve needs to be sent the `transactionID` of the item purchased (see `SKPaymentTransaction::transactionIdentifier`).

* **In iOS 6 and lower**, Apple only returns one transaction per receipt, so the Swrve IAP method without the `transactionId` can be used.

* If `paymentProvider` was Apple, use either `Base64EncodedReceipt` or `RawReceipt`, depending on whether the receipt is already encoded. Third party Unity IAP plugins can return the receipt already encoded (and this behavior may be different between iOS7+ receipts and iOS6 receipts).

* For Google Play, the `receipt` and `receiptSignature` should be provided.

* For other platforms (including Amazon) no receipt validation is available.


```
IapRewards rewards = new IapRewards(<rewardCurrency>, <rewardAmount>);

#if UNITY_IPHONE
if (iOS 7+) {
  SwrveComponent.Instance.SDK.IapApple(quantity,
                                       productId,
                                       <localCost>,
                                       <localCurrency>,
                                       rewards,
                                       Base64EncodedReceipt.FromString(base64Receipt),
                                       transactionId);
}
else { // iOS 6
  SwrveComponent.Instance.SDK.IapApple(quantity,
                                       productId,
                                       <localCost>,
                                       <localCurrency>,
                                       rewards,
                                       Base64EncodedReceipt.FromString(base64Receipt));
}
#elif UNITY_ANDROID
SwrveComponent.Instance.SDK.IapGooglePlay(productId,
                                          <localCost>,
                                          <localCurrency>,
                                          rewards,
                                          receipt,
                                          receiptSignature);
#else
// no receipt validation
SwrveComponent.Instance.SDK.Iap(quantity,
                                productId,
                                <localCost>,
                                <localCurrency>,
                                rewards);
#endif
```

If the purchase is of an item, and not a currency, you can omit the rewards argument:
```
SwrveComponent.Instance.SDK.Iap(1, "bird", 9.99, "USD");
```

The correct Apple bundle ID (iOS) or the Google Server Validation Key (Android) should be added to the integration settings page of the Swrve dashboard. This allows Swrve validate purchases made in the app.

Integrating Resource A/B Testing
-

To get the latest version of a resource from Swrve using the Resource Manager, use the following:

```
// Get the SwrveResourceManager which holds all resources
SwrveResourceManager resourceManager = SwrveComponent.Instance.SDK.ResourceManager;

// Then, wherever you need to use a resource, pull it from SwrveResourceManager.
// For example:
string welcomeScreen = resourceManager.GetResourceAttribute<string>(
  "new_app_config",
  "welcome_text",
  "Welcome!"
);
```

If you want to be notified whenever resources change, you can add a callback function as follows:

```
SwrveComponent.Instance.SDK.ResourcesUpdatedCallback = delegate() {
  // Callback functionality
};
```

Testing your integration
-
When you have completed the steps above, the next step is to test the integration. See [Testing Your Integration](http://docs.swrve.com/developer-documentation/advanced-integration/testing-your-integration/) for more details.


Upgrade Instructions
=
If you’re moving from an earlier version of the Unity SDK to the current version, see the [Unity SDK Upgrade Guide](/docs/upgrade_guide.md) for upgrade instructions.

How to run the demo
=
- Open the project 'unity3d' from the repository.
- Open the demo scene under `'Assets/Swrve/UnitySwrveDemo/DemoScene.unity'`.
- Under the Hierarchy view, click on SwrvePrefab. This is the SDK component in your scene.
- Introduce your `appId` and `apiKey` in the Inspector view with the values provided by Swrve.
- Click the Play button at the top.

How to build the SDK
=
- Open the project 'unity3d' from the repository.
- Click on the menu 'Swrve Demo / Export unityPackage'. This will generate a unityPackage that you can include in your project.

Contributing
=
We would love to see your contributions! Follow these steps:

1. Fork this repository.
2. Create a branch (`git checkout -b my_awesome_feature`)
3. Commit your changes (`git commit -m "Awesome feature"`)
4. Push to the branch (`git push origin my_awesome_feature`)
5. Open a Pull Request.

License
=
© Copyright Swrve Mobile Inc or its licensors. Distributed under the [Apache 2.0 License](LICENSE).  
Google Play Services Library Copyright © 2012 The Android Open Source Project. Licensed under the [Apache 2.0 License](http://www.apache.org/licenses/LICENSE-2.0).  
SharpZipLib Copyright © David Pierson, Mike Krueger. Distributed under the [GPL License](http://www.gnu.org/licenses/gpl.txt) with the [GPL Classpath exception](http://www.gnu.org/software/classpath/license.html).