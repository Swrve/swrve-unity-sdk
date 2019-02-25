Unity Swrve SDK Multiple FCM Providers Sample
---------------------------------------------
Example of how to integrate Swrve Push Notifications when your application already makes use of another push notification provider.

It showcases a custom FCM messaging service that relays the information to the Swrve messaging service See:[MyFirebaseMessagingService](src/main/java/com/swrve/sdk/sample/MyFirebaseMessagingService.java)

How to use
----------
- Introduce the changes you need in MyFirebaseMessagingService
- Compile the library into an .AAR (`./gradlew assemble`)
- Copy the .AAR from `build/outputs/aar/` to `Assets/Plugins/Android/`
- Introduce your service in your `Assets/Plugins/Android/AndroidManifest.xml` file as the first service with the `com.google.firebase.MESSAGING_EVENT` intent filter:

```xml
<service android:name="com.swrve.sdk.sample.MyFirebaseMessagingService"
    android:exported="false">
    <intent-filter>
        <action android:name="com.google.firebase.MESSAGING_EVENT" />
    </intent-filter>
</service>
```
