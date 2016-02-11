Swrve Unity SDK Release Notes
==

- [Swrve Unity SDK Release Notes](#unity-sdk-download)
  - [Release 3.5](#release-35)
  - [Release 3.4.3](#release-343)
  - [Release 3.4.2](#release-342)
  - [Release 3.4.1](#release-341)
  - [Release 3.4](#release-34)
  - [Release 3.3.2](#release-332)
    - [WARNING](#warning)
  - [Release 3.3.1](#release-331)
  - [Release 3.3](#release-33)
  - [Release 3.2](#release-32)
  - [Release 3.1.3](#release-313)
  - [Release 3.1.2 Beta](#release-312-beta)
  - [Release 3.1.1 Beta](#release-311-beta)
  - [Release 3.1](#release-31)
  - [Release 3.0.1](#release-301)
  - [Release 3.0](#release-30)
  - [Release 2.10](#release-210)
  - [Release 2.9](#release-29)
  - [Release 2.8.2](#release-282)
  - [Release 2.8.1](#release-281)
  - [Release 2.8](#release-28)
  - [Release 2.8 Push Beta](#release-28-push-beta)
  - [Release 2.7](#release-27)
  - [Previous Releases Summary](#previous-releases-summary)

Release 3.5
-
Release Date: November 30, 2015

Unity SDK release 3.5 is focused on the following:

* Custom events beginning with Swrve. or swrve. are now blocked and no longer sent by the SDK. For more information on creating custom events, see [Sending Events](http://docs.swrve.com/developer-documentation/advanced-integration/sending-events/).
* It is now easier to configure the SDK for EU data storage. For more information see [How Do I Configure the Swrve SDK for EU Data Storage?](http://docs.swrve.com/faqs/sdk-integration/configure-sdk-for-eu-data-storage/)
* It is now possible to log Google Advertising ID and Android ID, and iOS IDFA and IDVA as user properties. For more information, see [How Do I Log Advertising and Vendor IDs?](http://docs.swrve.com/faqs/app-integration/log-advertising-and-vendor-ids/)
* Added new Material icon, big icon and accent support for notifications.

Unity SDK release 3.5 includes the following bug fixes:

* Fixed issue with message rules so that resetting rules for QA users now only occurs once.
* The SDK now catches exceptions when the disk is full.
* The SDK now catches an exception when parsing a string as an integer after I/O problems.
* The SDK now checks the application's state when the Apple push is received without interfering with other providers.

Release 3.4.3
-
Release Date: October 23, 2015

Unity SDK release 3.4.3 is focused on the following:

* The Android push notifications plugin has been updated to the latest GCM library.

Release 3.4.2
-
Release Date: August 18, 2015

Unity SDK release 3.4.2 is focused on the following:

* iOS apps now default to use HTTPS to support iOS 9.
* The SDK now logs device region (for example, US, GB or FR) for iOS apps using the new swrve.device_region property.

Release 3.4.1
-
Release Date: June 4, 2015

Unity SDK release 3.4.1 includes the following bug fixes:

* Limits on the default maxBuffer size for sending receipts resulted in new 64 base encoded receipts from Apple being too long and therefore not registering in Swrve as in-app purchases. This limit has been increased to allow the SDK to successfully send iOS 8 purchase receipts. The SDK will now flush the buffer if it gets too big.
* Previously, the SDK stopped displaying in-app messages once the maxBuffer size of cached events was reached. The SDK now continues to display messages even if the maxBuffer size is reached.
* An issue regarding in-app message co-routine leaks has been fixed.

Release 3.4
-
Release Date: April 8, 2015

Unity SDK release 3.4 is focused on the following:

* The default background for in-app messages has been changed from solid black to transparent. You can configure the background color in the code, or contact your Customer Success Manager to configure it in the in-app message wizard template.
* The SDK won't execute on unsupported platforms (for example, any non-desktop iOS or Android platforms).
* iOS purchases have to explicitly define if the receipt is already encoded.

Unity SDK release 3.4 includes the following bug fixes:

* The SDK now releases pre-downloaded texture (images) from memory after initial download.
* The SDK will only display push notifications that come from Swrve as opposed to push notifications from a customer's app.
* Support for Android 5.0.2 and Android 5.1+.

Release 3.3.2
-
Release Date: March 13, 2015

Unity SDK release 3.3.2 is focused on the following:

* Added support for Unity 5.

### WARNING

* There is a bug in Unity 5.0.0 (iOS only) affecting all HTTPS requests. Any endpoints configured to use HTTPS will not work (that is, the SDK won't send events, get campaigns or user resources). Additionally, the image CDN can be set up with HTTPS for some apps, in which case the campaign metadata is downloaded but the images are not, so in-app messages won't be available. This issue is fixed in Unity version 5.0.1 or higher.

Release 3.3.1
-
Release Date: January 21, 2015

Unity SDK release 3.3.1 is focused on the following:

* Addition of a transaction ID to iOS IAP methods.
* The SDK now pulls campaigns and resources only when events are sent.

Unity SDK release 3.3.1 includes the following bug fixes:

* For Android, push notification's pending intent now have an individual ID.
* The SDK prevents sending empty Android carrier information.
* Avoids crashing when resources haven't been loaded.

Release 3.3
-
Release Date: November 11, 2014

Unity SDK release 3.3 is focused on the following:

* The SDK now logs mobile carrier information by default. Swrve tracks the name, country ISO code and carrier code of the registered SIM card. You can use the country ISO code to track and target users by country location.

Unity SDK release 3.3 includes the following bug fixes:

* Better error notification when an empty response is received by the server.
* By default, custom deeplinks now open in a browser.
* Filters non-alphanumeric values in the iOS token.
* Workaround for when JNI loading code includes negative values.
* GZIP empty content crash fix.
* Sends language property in ISO format.

Release 3.2
-
Release Date: October 21, 2014

Unity SDK release 3.2 is focused on the following:

* SDKs no longer use platform-specific unique device ids. The SDK now generates a random UUID if no custom user ID is provided at initialization. If you are using an SDK version prior to Unity SDK 2.6 please refer to the [Unity SDK Upgrade Guide](/docs/upgrade_guide.md).
* Deprecated cross application install tracking.
* Use http for in-app campaigns requests.

Unity SDK release 3.2 includes the following bug fixes:

* Added post-build script on iOS to only notify Swrve of push notifications when the app is running in the background.
* In-app message interval is also calculated from when the message is dismissed.
* Error notification when no AppStore is set up.
* The SDK now works when run on the Unity Editor.
* Avoids crashing when there is no app store URL linked to an in-app message install button.
* Avoids crashing when a QA user goes back to being a normal user.

Release 3.1.3
-
Release Date: September 12, 2014

Upgrade Instructions: None - all updates have been done internally so no code changes are required to upgrade to this version of the SDK.

Unity SDK release 3.1.3 includes support for devices running the Golden Master (GM) version of iOS 8 and is focused on the following:

* Support for iOS 8 push notification token registration.
* Support for iOS 8 screen rotation API.

Please note that SDK release 3.1.3 replaces previous Beta versions of the SDK for use in preparing your app for iOS 8.

Release 3.1.2 Beta
-
Release Date: August 29, 2014

Upgrade Instructions: None - no code changes are required to upgrade to this version of the SDK.

Unity SDK release 3.1.2 Beta is focused on the following:

* Includes support for testing devices running iOS 8 Beta 5.

Please note iOS 8 is not released yet, so SDK release 3.1.2 is a beta only. Use the SDK to test for iOS 8 readiness but do not submit until iOS 8 is officially released and your app has been built using production tools.

Release 3.1.1 Beta
-
Release Date: August 8, 2014

Upgrade Instructions: None - no code changes are required to upgrade to this version of the SDK.

Unity SDK release 3.1.1 Beta is focused on the following:

* Using new native push registration method from iOS 8 when available.

Please note iOS 8 is not released yet, so SDK release 3.1.1 is a beta only. Use the SDK to test for iOS 8 readiness but do not integrate it into your production apps. The Unity SDK release 3.1.1 Beta is not available for general download; see above.

Release 3.1
-
Release Date: July 30, 2014

Unity SDK release 3.1 is focused on the following:

* Unity Android Google Cloud Messaging push notification integration has now been simplified. There is no longer any need to include the broadcast listener and service in the main app package; it is sufficient to target the broadcast listener and service provided with the SDK.
* Support for in-app message delivery at session start has been enhanced.
* Swrve now automatically retrieves the app version if it has not been set in the Swrve Component Config.
* The android.permission.ACCESS_WIFI_STATE and android.permission.CHANGE_WIFI_STATE permissions have been removed.
* Internet performance metrics have now been added to the SDK.

Unity SDK release 3.1 includes the following bug fix:

* An issue whereby an attempt was made to send logging data to QA devices despite logging being disabled has been fixed.
* Google Cloud Messaging registration ids are also obtained from the Broadcast Receiver to workaround Google Play bugs.

Release 3.0.1
-
Release Date: May 27, 2014

Unity SDK release 3.0.1 includes the following bug fix:

* An issue whereby the install time user property was sent in the incorrect format has been corrected.
* Stripping of the class System.Security.Cryptography.MD5CryptoServiceProvider is now avoided to make stripping-enabled running possible.

Release 3.0
-
Release Date: May 15, 2014

Unity SDK release 3.0 is focused on the following:

* Real-time targeting enhancements - Swrve now automatically downloads user resources and campaign messages and keeps them up to date in near real-time during each session in order to reflect the latest segment membership. This is useful for time-sensitive A/B tests and messaging campaigns.
* Config values which controlled whether and how often events were automatically sent to Swrve have been removed as Swrve now sends events automatically as part of the real-time targeting feature.
* Google IAP functionality has been added to the Unity SDK.
* The deprecated BuyIn function has been removed.
* The public void NamedEvent and public void NamedEventWithMessage functions have been removed.

Unity SDK release 3.0 includes the following bug fixes:

* Exceptions have been removed from control logic.
* An issue whereby button events were processed in the wrong order has been corrected.

Release 2.10
-
Release Date: April 1, 2014

Unity SDK release 2.10 is focused on the following:

* Processing of the Install and Deep Link in-app message actions has been simplified.
* Registration of QA devices has been simplified.
* If push notifications are enabled, the default time to request push notification permission is now at session start.
* `swrve.timezone_name` is now tracked in the Unity SDK.

Unity SDK release 2.10 includes the following bug fixes:

* In-app messaging orientation issues have been fixed.
* The SDK now sets a client_time timestamp in every event in each batch.
* Rejected events are now dropped by the SDK.
* An issue with null JSON references has been fixed.

Unity SDK 2.10 requires that you use Unity version 4.0.0 or later.

Release 2.9
-
Release Date: March 4, 2014

Unity SDK release 2.9 is focused on the following:

* Push notifications are now supported for both Unity Android and Unity iOS devices.
* The automatic campaign event is now changed to swrve.messages_downloaded.
* A defaultLanguage (set to en by default) has been created on the config. This value is used if a language has not been specified or if the SDK could not detect the language from the device.
* An iOS and Android native plugin now obtains the language from the device.
* Campaign, user resources and saved events signing processes have been added.
* GZIP support for campaigns and user resources has been added.

Release 2.8.2
-
Unity SDK release 2.8.2 is focused on the following:

* All references to IDfA have been removed from Swrve's Unity SDKs.
* Cross-company install tracking for cross promotions on Unity now only works for apps that are from the same company.

Release 2.8.1
-
Unity SDK release 2.8.1 is focused on correcting an issue that affects customers who:

* Are letting the Unity SDK generate unique user IDs for them.
* Have not used Unity SDK 2.6 but progressed directly to use 2.7.
* Have the app installed in iOS 6 and upgraded to iOS 7 without reinstalling the app.

Release 2.8
-
Unity SDK release 2.8 is focused on Swrve's redesigned in-app messaging functionality. It includes the following:

* Swrve's in-app message format rules are now enforced.
* The device friendly name is no longer logged as a user property.

For information about upgrading to Unity SDK 2.8, see [Swrve Unity SDK Upgrade Guide](/docs/upgrade_guide.md).

Release 2.8 Push Beta
-
This SDK release contains the same updates as the 2.8 release but also enables you to send push notifications to Android devices on the Google Play app store. If you're interested in Beta testing push notifications, contact your Swrve account manager for more information.

Release 2.7
-
Unity SDK release 2.7 is focused on push notification changes, the auto-generation of user IDs, and the unification of the in-app messaging and analytics SDKs. It includes the following:

* Significant push notification SDK enhancements have been made.
* User IDs are now auto-generated (though they can be overwritten).
* The in-app messaging and analytics SDKs are now unified.

For information about upgrading to Unity SDK 2.7, see [Swrve Unity SDK Upgrade Guide](/docs/upgrade_guide.md).

Previous Releases Summary
-
* Nov 12, 2013 - v2.6 - Added support for extended IAP event and bug fixes.
* Oct 22, 2013 - v2.5.2 - Bug fixes.
* Oct 18, 2013 - v2.5.1 - Bug fixes.
* Oct 16, 2013 - v2.5 - Added support for in-app messaging per campaign dismissal rules and bug fixes.
* Oct 2, 2013 - v2.4.1- Bug fixes.
* Sep 17, 2013 - v2.4 - Added support for in-app messaging QA logging.
* Aug 20, 2013 - v2.3 - Added support for in-app messaging QA user functionality.
* July 26, 2013 - v.2.2 - Support for new iTunes connect receipt validation.
* July 2, 2013 - v.2.0.1 - Added support for in-app messaging and other features.
* Feb 21, 2013 - v 1.0 - First public release.
* Aug 24, 2012 - First beta release.
* June 5, 2012 - Fixed install, dismiss and custom button processing.
