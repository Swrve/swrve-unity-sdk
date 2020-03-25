Unity Swrve SDK Multiple FCM Providers Sample
---------------------------------------------
Example of how to integrate Swrve Push Notifications when your application already makes use of another push notification provider.

It showcases a custom FCM messaging service that relays the information to the Swrve messaging service See: [MyFirebaseMessagingService.java](src/main/java/com/swrve/sdk/sample/MyFirebaseMessagingService.java)

How to use
----------
- Download the release .zip version from https://github.com/Swrve/swrve-unity-sdk/releases. Get the same version as your Swrve Unity SDK version.
- Introduce the changes you need in `MyFirebaseMessagingService.java`
- Compile the library into an .AAR (`../../gradlew assemble`)
- Copy the .AAR from `build/outputs/aar/` to your Unity3D project's `Assets/Plugins/Android/`
- Add the service `MyFirebaseMessagingService` in your Unity3D project's `Assets/Plugins/Android/AndroidManifest.xml` file as the first service with the `com.google.firebase.MESSAGING_EVENT` intent filter (to make sure it is picked up before any other service):

```xml
<service android:name="com.swrve.sdk.sample.MyFirebaseMessagingService"
    android:exported="false">
    <intent-filter>
        <action android:name="com.google.firebase.MESSAGING_EVENT" />
    </intent-filter>
</service>
```
