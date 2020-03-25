Unity Swrve SDK Bridge for Multidex
-----------------------------------
If you need to enable multidex but still use a Swrve Application class use this project.

How to use
----------
- Download the release .zip version from https://github.com/Swrve/swrve-unity-sdk/releases. Get the same version as your Swrve Unity SDK version.
- Find the already built .aar inside `build/outputs/aar/` and copy it to your Unity3D project's `Assets/Plugins/Android/`
- Point your application class in your `Assets/Plugins/Android/AndroidManifest.xml` to `SwrveUnityApplicationMultiDex`:
```xml
<application android:name="com.swrve.sdk.SwrveUnityApplicationMultiDex">
```
