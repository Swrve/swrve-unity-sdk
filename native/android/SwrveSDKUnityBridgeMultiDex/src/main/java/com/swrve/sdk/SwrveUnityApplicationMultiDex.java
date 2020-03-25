package com.swrve.sdk;

import android.content.Context;
import androidx.multidex.MultiDex;

/**
 * This class is needed for when you want Swrve to properly process background
 * processes for the Unity SDK, like push notifications. This is a version of SwrveUnityApplication but with
 * SwrveUnityApplicationMultiDex enabled as specified in https://developer.android.com/studio/build/multidex.
 *
 * This file is included in the swrve-unity aar, but needs to be referred to in
 * your AndroidManifest file, with
 * <application android:name="com.swrve.sdk.SwrveUnityApplicationMultiDex">
 *
 * Alternatively you can call SwrveUnityCommon.onCreate(this) from your custom
 * application class and enable multi dex there.
 */
public class SwrveUnityApplicationMultiDex extends SwrveUnityApplication {
    @Override
    protected void attachBaseContext(Context base) {
        super.attachBaseContext(base);
        MultiDex.install(this);
    }
}
