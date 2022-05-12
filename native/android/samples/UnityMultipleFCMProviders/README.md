Unity Swrve SDK Multiple FCM Providers Sample
---------------------------------------------
This sample shows how to integrate Swrve Push Messaging with another push messaging provider.

It uses a custom FCM messaging service that passes the information to a Swrve helper service. See: [MyFirebaseMessagingService.java](src/main/java/com/swrve/sdk/sample/MyFirebaseMessagingService.java). If the helper service returns false from `SwrvePushServiceDefault` then the push message is not consumed by Swrve and is available for another push service.

How to use
----------
- Download the source release .zip version from https://github.com/Swrve/swrve-unity-sdk/releases. Get the same version as your Swrve Unity SDK version.
- Download native source from [swrve-android-sdk](https://github.com/Swrve/swrve-android-sdk) repository.
- Ensure the `ANDROID_REPO_LOC` property in [gradle.properties](../../gradle.properties) is pointing to the location of the downloaded native source `swrve-android-sdk`.
- Open Android Studio project from folder [native/android/](../../../../native/android/).
- Introduce the changes you need in `MyFirebaseMessagingService.java` from the `UnityMultipleFCMProviders` project
- Compile the library into an .AAR by running `./gradlew UnityMultipleFCMProviders:assemble` from `native/android/` folder
- Copy the .AAR from `samples/UnityMultipleFCMProviders/build/outputs/aar/` to your Unity3D project's `Assets/Plugins/Android/`
- Add the service `MyFirebaseMessagingService` in your Unity3D project's `Assets/Plugins/Android/AndroidManifest.xml` file as the first service with the `com.google.firebase.MESSAGING_EVENT` intent filter (to make sure it is picked up before any other service):

```
<service android:name="com.swrve.sdk.sample.MyFirebaseMessagingService"
    android:exported="false">
    <intent-filter>
        <action android:name="com.google.firebase.MESSAGING_EVENT" />
    </intent-filter>
</service>
```
