Unity SDK Upgrade Guide
=

This guide provides information about how you can upgrade to the latest Swrve Unity SDK. For information about the changes that have been made in each Unity SDK release, see [Unity SDK Release Notes](/docs/release_notes.md).

- [Upgrading to Unity SDK v3.6](#upgrading-to-unity-sdk-v36)
  - [SDK Shared Instance](#sdk-shared-instance)
- [Upgrading to Unity SDK v3.5](#upgrading-to-unity-sdk-v35)
  - [Custom events starting with "Swrve."](#custom-events-starting-with-swrve)
  - [Google Push and material configuration](#google-push-and-material-configuration)
- [Upgrading to Unity SDK v3.4.3](#upgrading-to-unity-sdk-v343)
  - [Google Cloud Messaging Push Integration](#google-cloud-messaging-push-integration)
- [Upgrading to Unity SDK v3.4.2](#upgrading-to-unity-sdk-v342)
- [Upgrading to Unity SDK v3.4.1](#upgrading-to-unity-sdk-v341)
- [Upgrading to Unity SDK v3.4](#upgrading-to-unity-sdk-v34)
  - [In-app Message background color default](#in-app-message-background-color-default)
  - [iOS IapApple](#ios-iapapple)
- [Upgrading to Unity SDK v3.3.2](#upgrading-to-unity-sdk-v332)
- [Upgrading to Unity SDK v3.3.1](#upgrading-to-unity-sdk-v331)
- [Upgrading to Unity SDK v3.3](#upgrading-to-unity-sdk-v33)
- [Upgrading to Unity SDK v3.2](#upgrading-to-unity-sdk-v32)
  - [Swrve ID](#swrve-id)
  - [Link token and link server deprecated](#link-token-and-link-server-deprecated)
- [Upgrading to Unity SDK v3.1.3](#upgrading-to-unity-sdk-v313)
- [Upgrading to Unity SDK v3.1](#upgrading-to-unity-sdk-v31)
  - [App Version](#app-version)
  - [Google Cloud Messaging Push Integration](#google-cloud-messaging-push-integration-1)
  - [In-app Messages at session start](#in-app-messages-at-session-start)
- [Upgrading to Unity SDK v3.0](#upgrading-to-unity-sdk-v30)
  - [Real-time Targeting](#real-time-targeting)
- [Upgrading to Unity SDK v2.10](#upgrading-to-unity-sdk-v210)
  - [Supported Unity Version](#supported-unity-version)
  - [Custom Action Processing](#custom-action-processing)
- [Upgrading to Unity SDK v2.9](#upgrading-to-unity-sdk-v29)
- [Upgrading to Unity SDK v2.8](#upgrading-to-unity-sdk-v28)
- [Upgrading to Unity SDK v2.7](#upgrading-to-unity-sdk-v27)
  - [Code changes](#code-changes)
  - [In-app Messaging](#in-app-messaging)
  - [Automatic User ID Generation](#automatic-user-id-generation)
- [Upgrading to Unity SDK v2.6](#upgrading-to-unity-sdk-v26)
  - [Deprecation of the `buy_in` function:](#deprecation-of-the-buy_in-function)
  - [Deprecation of the old IAP function](#deprecation-of-the-old-iap-function)
- [Upgrading to Unity SDK v2.4](#upgrading-to-unity-sdk-v24)
  - [Migration from Unity SDK 2.3 to Unity SDK 2.4](#migration-from-unity-sdk-23-to-unity-sdk-24)


Upgrading to Unity SDK v3.6
=

This section provides information to enable you to upgrade to Swrve Unity SDK v3.6.

SDK Shared Instance
-
* The SDK shared instance now uses Object.FindObjectsOfType instead of Resources.FindObjectsOfTypeAll. Ensure your prefab is in the scene and active.

Upgrading to Unity SDK v3.5
=
This section provides information to enable you to upgrade to Swrve Unity SDK v3.5.

Custom events starting with "Swrve."
-
Custom events that start with `Swrve.*` or `swrve.*` are now restricted. You need to rename any custom `Swrve.` events or they won’t be sent.

Google Push and material configuration
-
The Google Push Plugin has been updated. You need to update the JARs of your project when upgrading to Unity SDK 3.5.

Also, there are now extra configurations to control Android L+ features. The new configurations are:

* Material icon – `Config.GCMPushNotificationMaterialIconId = "ic_launcher_material"`;
* Large icon – `Config.GCMPushNotificationLargeIconId = "ic_launcher_largel"`;
* Accent color – `Config.GCMPushNotificationAccentColor = 0; // Black color`

Upgrading to Unity SDK v3.4.3
=

This section provides information to enable you to upgrade to Swrve Unity SDK v3.4.3.

Google Cloud Messaging Push Integration
-
The GCM integration has been updated to the latest Google library. To upgrade your app, do the following:

* Update `AndroidManifest.xml` with the following changes:
* Update the JARs from the Google Push plugin and the SDK source
* In your broadcast receiver, change the package name `android:name="com.swrve.unity.gcm.SwrveGcmBroadcastReceiver"` to `android:name="com.google.android.gms.gcm.GcmReceiver"` and add `android:exported="true"`.
* Add `<action android:name="com.google.android.c2dm.intent.RECEIVE" />` to the intent service `com.swrve.unity.gcm.SwrveGcmIntentService`.
* Add a new service `<service android:name="com.swrve.unity.gcm.SwrveGcmInstanceIDListenerService">` with the intent filter action `<action android:name="com.google.android.gms.iid.InstanceID"/>`.
* Upgrade the value of `com.google.android.gms.version` to `7895000`.

When you’re done, your `AndroidManifest.xml` should have a section like this:

```
<application android:icon="@drawable/app_icon" android:label="@string/app_name" android:debuggable="false">
<!-- Swrve Push Plugin -->
<meta-data android:name="com.google.android.gms.version" android:value="7895000" />
<receiver
    android:name="com.google.android.gms.gcm.GcmReceiver"
    android:exported="true"
    android:permission="com.google.android.c2dm.permission.SEND" >
    <intent-filter>
        <action android:name="com.google.android.c2dm.intent.REGISTRATION" />
        <action android:name="com.google.android.c2dm.intent.RECEIVE" />
        <category android:name="com.example.gcm" />
    </intent-filter>
</receiver>
<service android:name="com.swrve.unity.gcm.SwrveGcmIntentService">
    <intent-filter>
        <action android:name="com.google.android.c2dm.intent.RECEIVE" />
    </intent-filter>
</service>
<service
    android:name="com.swrve.unity.gcm.SwrveGcmInstanceIDListenerService"
    android:exported="false" >
    <intent-filter>
       <action android:name="com.google.android.gms.iid.InstanceID" />
    </intent-filter>
</service>
<!-- End Swrve Push Plugin -->
```

Upgrading to Unity SDK v3.4.2
=
No code changes are required to upgrade to Swrve Unity SDK v3.4.2.

Upgrading to Unity SDK v3.4.1
=
No code changes are required to upgrade to Swrve Unity SDK v3.4.1.

Upgrading to Unity SDK v3.4
=
This section provides information to enable you to upgrade to Swrve Unity SDK v3.4.

In-app Message background color default
-
The in-app message background color is now transparent by default. If you want to maintain a solid black background, you must configure it before initializing the SDK as follows:

```
config.DefaultBackgroundColor = Color.black;
```

iOS IapApple
-
You now have to specify what type of receipt your plugin provides: `Base64EncodedReceipt` or `RawReceipt`. For example:

```
IapApple (1, “productId”, 0.99, “gold”, the_base_64_encoded_receipt);
```

becomes

```
IapApple (1, “productId”, 0.99, “gold”, Base64EncodedReceipt.FromString(the_base_64_encoded_receipt));
```

Upgrading to Unity SDK v3.3.2
=
No code changes are required to upgrade to Swrve Unity SDK v3.3.2.

Upgrading to Unity SDK v3.3.1
=
No code changes are required to upgrade to Swrve Unity SDK v3.3.1.

Upgrading to Unity SDK v3.3
=
No code changes are required to upgrade to Swrve Unity SDK v3.3.

Upgrading to Unity SDK v3.2
=
This section provides information to enable you to upgrade to Swrve Unity SDK v3.2.

Swrve ID
-
Use of platform-specific unique device identifiers has been removed. The SDK now generates a random UUID if no custom user ID is provided at initialization.

**Important!** If you are upgrading from a Unity SDK version prior to 2.6, make sure to provide the Unity device unique ID as a custom ID as follows:

```
Config.UserId = SystemInfo.deviceUniqueIdentifier;
```

If you don’t, all your users will become new users when they upgrade their app, as they will be given new user IDs.

Link token and link server deprecated
-
Cross application install tracking has been deprecated. Please remove any reference to these attributes or methods:

* `SwrveConfig.LinkToken`
* `SwrveConfig.LinkServer`
* `SwrveSDK.ClickThru (int gameId, string source)`

Upgrading to Unity SDK v3.1.3
=
No code changes are required to upgrade to Swrve Unity SDK v3.1.3.

Upgrading to Unity SDK v3.1
=
This section provides information to enable you to upgrade to Swrve Unity SDK v3.1.

App Version
-
Swrve now automatically retrieves the app version if it has not been set in the Swrve Component Config. For Unity iOS, Swrve reads the app version from the Bundle version, while for Unity Android, Swrve reads it from the `AndroidManifest.xml` file. As a result, Swrve automatically starts sending the new app version if you update it in Xcode or in the Android manifest. It’s automatically logged as the `swrve.app_version` user property, making it available for segmentation and targeting.

You can continue to set the app version in the Swrve Component Config if you require; however, in this instance you are responsible for manually updating the app version each time it changes.

Google Cloud Messaging Push Integration
-
The GCM integration on Unity Android has been simplified. To integrate this simplified functionality, do the following:

1. Remove `swrvesdkpushsupport.jar` and replace it with the new JAR file located in `plugins/SwrveGooglePlayPushNotification`.

2. Update `AndroidManifest.xml` with the following changes:

  In your broadcast receiver, change `android:name="com.your_company.SwrveGcmBroadcastReceiver"` to `android:name="com.swrve.unity.gcm.SwrveGcmBroadcastReceiver"`

  Add `<action android:name="com.google.android.c2dm.intent.REGISTRATION" />` to the broadcast receiver

  Change `<service android:name="com.your_company.SwrveGcmIntentService"/>` to `<service android:name="com.swrve.unity.gcm.SwrveGcmIntentService"/>`

  When you’re through, your `AndroidManifest.xml` should have a section like this:

  ```
  <application android:icon="@drawable/app_icon" android:label="@string/app_name" android:debuggable="false">
  <!-- Swrve Push Plugin -->
  <meta-data android:name="com.google.android.gms.version" android:value="4030500" />
  <receiver
    android:name="com.swrve.unity.gcm.SwrveGcmBroadcastReceiver"
    android:permission="com.google.android.c2dm.permission.SEND" >
    <intent-filter>
      <action android:name="com.google.android.c2dm.intent.REGISTRATION" />
      <action android:name="com.google.android.c2dm.intent.RECEIVE" />
      <category android:name="com.example.gcm" />
    </intent-filter>
  </receiver>
  <service android:name="com.swrve.unity.gcm.SwrveGcmIntentService"/>
  <!-- End Swrve Push Plugin -->
  ```

3. On the SDK initialization config, set up the app title that is displayed for each push notification as follows:

  ```
  SwrveConfig config = new SwrveConfig();
  config.GCMPushNotificationTitle = “Awesome app”;
  ```

In-app Messages at session start
-
The Session Start check box has been added to the Set Target screen of the in-app message wizard to enable you to configure in-app message display at session start. If you were previously using the following code for that purpose, you must now remove it:

```
config.AutoShowMessageAfterDownloadEventNames
```

The session start timeout (the time that messages have to load and display after the session has started) is set to 5 seconds by default. You can modify the timeout value using the following:

```
config.AutoShowMessagesMaxDelay = 5; // (seconds)
```

If the user is on a slow network and the images cannot be downloaded within the timeout period you specify, they are not displayed in that session. They are instead cached and shown at the next session start for that user.

Upgrading to Unity SDK v3.0
=

This section provides information to enable you to upgrade to Swrve Unity SDK v3.0.

Real-time Targeting
-
Swrve has the ability to update segments on a near real-time basis. Swrve now automatically downloads user resources and campaign messages and keeps them up to date in near real-time during each session in order to reflect the latest segment membership. This is useful for time-sensitive A/B tests and messaging campaigns. These updates are only run if there has been a change in the segment membership of the user, therefore resulting in minimal impact on bandwidth.

Real-time refresh is enabled by default and, if you want to avail of it, you must perform the upgrade tasks detailed below.

* **Configuration**


  `Config.AutomaticallyDownloadCampaigns`, which was used to indicate whether campaigns should be downloaded automatically upon app start-up has been replaced with the following:

  ```
  Config.AutoDownloadCampaignsAndResources
  ```

  By default, `Config.AutoDownloadCampaignsAndResources` is set to `TRUE;` as a result, Swrve automatically keeps campaigns and resources up to date.

  If you decide to set it to `FALSE`, campaigns and resources are always set to cached values at app start-up and it’s up to you to call the following function to update them; for example, at session start or at a key moment in your app flow:

  ```
  SwrveComponent.Instance.SDK.RefreshUserResourcesAndCampaigns();
  ```

  If you disable `Config.AutoDownloadCampaignsAndResources`, the existing `GetUserResources` function no longer works as resources are read from the cache and no longer updated.

* **Campaigns**

  Campaign messages are now downloaded automatically as soon as the user becomes eligible for them (even on an intra-session basis). Therefore, the following functions have been removed:

  ```
  public void ReloadCampaigns ()
  public void NamedEventWithoutMessage (string name, Dictionary<string, string> payload = null)
  public void NamedEvent (string name, ISwrveInstallButtonListener installButtonListener, ISwrveCustomButtonListener customButtonListener, ISwrveMessageListener messageListener, SwrveOrientation orientation, Dictionary<string, string> payload = null)
  ```

  If you disable this new feature, you can reload campaigns manually at any point in your app:

  ```
  public void RefreshUserResourcesAndCampaigns ()
  ```

* **User Resources**

  Swrve now automatically downloads user resources and keeps them up to date with any changes. Swrve supplies a Resource Manager to enable you to retrieve the most up-to-date values for resources at all times; you no longer need to explicitly tell Swrve when you want updates.

  If you use Swrve’s new Resource Manager, you no longer need to use the following two functions:

  ```
  public void GetUserResources (Action<Dictionary<string, Dictionary<string, string>>, string> onResult, Action onError)
  public void GetUserResourcesDiff (Action<Dictionary<string, Dictionary<string, string>>, Dictionary<string, Dictionary<string, string>>, string> onResult, Action onError)
  ```

  The new resource manager behaves like getUserResources; it returns the value of the attribute for the current user. If the value in the Swrve service is changed, the Resource Manager reflects that straight away.

  To call the Resource Manager from the Swrve object, use the following:

  ```
  SwrveResourceManager resourceManager = SwrveComponent.Instance.SDK.ResourceManager;
  ```

  The SwrveResourceManager class has a method to retrieve attribute values for a specific resource:

  ```
  public T GetResourceAttribute<T>(string uuid, string attributeName, T defaultValue)
  ```

  Note that the method takes a default value that is returned when either the resource or the attribute doesn’t exist. For example, to get the price of a sword you might call the following:

  ```
  float attributeValue = SwrveComponent.Instance.SDK.ResourceManager.GetResourceAttribute<float>("sword", "price", 0.99f);
  ```

  These methods always return the most up-to-date value for the attribute.

  Optionally, you can set up a callback function that is called every time updated resources are available. Use this if you want to implement your own resource manager, for example. You can set this as follows as part of your initialization code:

  ```
  SwrveComponent.Instance.SDK.ResourcesUpdatedCallback = delegate() {
    // Callback functionality
  };
  ```

  The callback function takes no arguments, it just lets you know resources have been updated. You must then use the Resource Manager to get the new values. You can store all resources locally and keep them up to date using the following:

  ```
  SwrveComponent.Instance.SDK.ResourcesUpdatedCallback = delegate() {
    myResources = resourceManager.UserResources;
  };
  ```

  The callback is called if there is a change in the user resources and also at the start as soon as resources have been loaded from cache.

* **Event intervals**

  The following config values, which controlled whether and how often events were automatically sent to Swrve, have been removed as Swrve now sends events automatically as part of the real-time targeting feature:

  ```
  /// Automatically send events in intervals
  public bool AutomaticallySendQueuedEvents = true;

  /// Automatically send events in intervals (specified in seconds).
  public int SendQueuedEventsInterval = 30;
  ```

* **Buyin function**

  The deprecated function BuyIn has been removed in this release:

  ```
  public void BuyIn (string rewardCurrency, int rewardAmount, double localCost, string localCurrency, string paymentProvider)
  ```

  If you are still using the BuyIn function, you must replace it with the IAP function.

* **`public void NamedEvent` function**

  The function `public void NamedEvent` has been removed in this release:

  ```
  public void NamedEvent (string name, ISwrveInstallButtonListener installButtonListener, ISwrveCustomButtonListener customButtonListener, ISwrveMessageListener messageListener, SwrveOrientation orientation, Dictionary<string, string> payload = null)
  ```

  This function, which enabled you to specify your own specific listeners as callbacks for specific actions, is no longer required as the following global listeners are available:

  * ```GlobalInstallButtonListener```
  * ```GlobalCustomButtonListener```
  * ```GlobalMessageListener```


* **`public void NamedEventWithMessage` function**

  The function public void NamedEventWithMessage has been removed in this release:

  ```
  public void NamedEventWithoutMessage (string name, Dictionary<string, string> payload = null)
  ```

Upgrading to Unity SDK v2.10
=
This section provides information to enable you to upgrade to Swrve Unity SDK v2.10.

Supported Unity Version
-
SDK 2.10 requires that you use Unity version 4.0.0 or later.

Custom Action Processing
-
The custom button processing has been divided into two individual listeners: one for custom actions and another for install actions with the ability to override the default behavior.

You must transform your code from the following:

```
private class CustomButtonListener : ISwrveButtonListener {
  public bool OnAction (SwrveMessageFormat format, SwrveActionType type, string action, int appId) {
        // Custom button logic
        return true;
      }
   }
SwrveComponent.Instance.SDK.GlobalButtonListener = new CustomButtonListener();
```

into the following:


```
private class CustomButtonListener : ISwrveCustomButtonListener {
  public void OnAction (string customAction) {
    // Custom button logic
  }
}
SwrveComponent.Instance.SDK.GlobalCustomButtonListener = new CustomButtonListener();
```

Upgrading to Unity SDK v2.9
=
No code changes are required to upgrade to Swrve Unity SDK v2.9.

Upgrading to Unity SDK v2.8
=
No code changes are required to upgrade to Swrve Unity SDK v2.8.

Upgrading to Unity SDK v2.7
=
This section provides information to enable you to upgrade to Swrve Unity SDK v2.7.

Code changes
-
The new SDK includes missing namespaces and has moved the core code to a new location. The SDK is now located in `Assets/Swrve/SwrveSDK` and is divided into `SwrveSDK` and `SwrveComponent`. To continue using the `MonoBehaviour` approach, upgrade your Script property to point to the script in the new location. You can use the SDK as a normal object. However, it still requires a container `MonoBehaviour` to run specific Unity functions. In this manner, you can manage where it lives and when it is initialized. For an example of a `MonoBehaviour` container, see the `SwrveComponent`.

In-app Messaging
-
`SwrveComponent` and `SwrveTalkComponent` are now included in the same `MonoBehaviour`. If you were previously using Swrve’s in-app messaging functionality, point your script property to the new `SwrveComponent` and change the configuration to enable in-app messaging using the editor or the following code:

```
SwrveComponent.Instance.Config.TalkEnabled = true;
```

Automatic User ID Generation
--
In older SDK versions you had to create your own Swrve user_id during initialization. From version 2.7, the SDK generates a user ID for you if you have not already provided one.

If you were using a previous version of the SDK in which you specified a user ID, you must continue to provide the ID to the init function so that your existing users do not become new users:

```
SwrveComponent.Instance.Init(APP_ID, API_KEY, USER_ID);
```

Alternatively, you can pass the user ID in the config object:

```
SwrveConfig config = new SwrveConfig();
config.UserId = 
// Generation of your user id
 
SwrveComponent.Instance.Init(APP_ID, API_KEY, config);
```

Upgrading to Unity SDK v2.6
=

This section provides information to enable you to upgrade to Swrve Unity SDK v2.6.

Base64 encoding is now performed by the Swrve SDK; to avoid duplication of event encoding, pass the unencoded receipt from the Apple IAP transactions through to the IAP function.

Deprecation of the `buy_in` function:
-
The old buy-in function was used to record purchases of in-app currency that was paid for with real-world money:


```
public void BuyIn (string rewardCurrency,
                   int rewardAmount,
                   double localCost,
                   string localCurrency,
                   string paymentProvider);
```

This is now replaced by creating a IapRewards object with the in-app currency details, and a call to `Iap()` (where <...> matches one of the arguments to BuyIn):

(if <paymentProvider> was “Apple”)


```
IapRewards rewards = new IapRewards(<rewardCurrency>, <rewardAmount>);
IapApple(quantity,
         productId,
         <localCost>,
         <localCurrency>,
         rewards,
         receipt);
```

(for any other value of <paymentProvider>)

```
IapRewards rewards = new IapRewards(<rewardCurrency>, <rewardAmount>);
Iap(quantity,
    productId,
    <localCost>, 
    <localCurrency>,
    rewards);
```

The productId should match a resource name in Swrve, and quantity the number of these products purchased.

For example, you might have `productId = "bagOfGold"` and `quantity = 1`, and then record the in-app currencies as `rewardCurrency = "gold"` and `rewardAmount = 200` to mean that the user purchased 1 bag of gold from the app store and for this received 200 gold coins.

Deprecation of the old IAP function
-

The old Iap function was a replacement for the BuyIn function. It was also used to record purchases of in-app currency that were paid for with real money, except it included receipt validation and could only be used with the Apple iTunes Store:

```
public void Iap(string rewardCurrency, int rewardAmount, double localCost, string localCurrency, string base64Receipt);
```

It can be replaced in a similar way by the new IapApple function:

```
IapRewards rewards = new IapRewards(<rewardCurrency>, <rewardAmount>);
IapApple(quantity,
         productId,
         <localCost>,
         <localCurrency>,
         rewards,
         receipt);
```

The `productId` should match a resource name in Swrve, and quantity the number of these products purchased.

For example, you might have `productId = "bagOfGold"` and `quantity = 1`, and then record the in-app currencies as `rewardCurrency = "gold"` and `rewardAmount = 200` to mean that the user purchased 1 bag of gold from the app store and for this received 200 gold coins.

Note: Base64 encoding is now performed by the Swrve SDK; to avoid duplication of event encoding, pass the unencoded receipt from the Apple IAP transactions through to the IAP function.

Upgrading to Unity SDK v2.4
=
This section provides information to enable you to upgrade to Swrve Unity SDK v2.4.

Migration from Unity SDK 2.3 to Unity SDK 2.4
-
The SDK app store setup has been changed from the `enum SwrveAppStore` to a string. You can now provide a custom value and set that app store in the dashboard. The initialisation turns into:

* `swrve.Init(APP_ID, API_KEY, USER_ID, "google" / "amazon" / "apple" | "other");`

Due to changes to iOS 7, MAC address related functions have been deprecated in the Unity SDK for iOS targets. Please remove calls to the following functions or types:

* `swrve.AutomaticallySendMacAddressMD5` (property)
* `swrve.SetMacAddress (method)swrve.SetMacAddressSHA1` (method)
* `swrve.SetMacAddressMD5` (method)

The SDK initialisation can now throw exceptions. The SDK throws an exception if:

* Using the Analytics SDK without an user id
* Using the Analytics SDK without an api key
* Using the Talk SDK without a link token
* Using the Talk SDK without a language
* Using the Talk SDK without an app store

