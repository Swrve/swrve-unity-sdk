package com.swrve.sdk;

import android.annotation.TargetApi;
import android.app.Activity;
import android.os.Build;
import android.util.Log;

import com.swrve.sdk.unitybridge.BuildConfig;
import com.unity3d.player.UnityPlayer;

import org.junit.After;
import org.junit.Before;
import org.junit.runner.RunWith;
import org.robolectric.Robolectric;
import org.robolectric.RobolectricTestRunner;
import org.robolectric.RuntimeEnvironment;
import org.robolectric.Shadows;
import org.robolectric.annotation.Config;
import org.robolectric.shadows.ShadowApplication;
import org.robolectric.shadows.ShadowLog;

@RunWith(RobolectricTestRunner.class)
@Config(sdk = Build.VERSION_CODES.LOLLIPOP)
@TargetApi(Build.VERSION_CODES.GINGERBREAD)
public abstract class SwrveBaseTest {

    protected ShadowApplication shadowApplication;
    protected Activity mActivity;

    @Before
    public void setUp() {
        SwrveLogger.setLogLevel(Log.VERBOSE);
        ShadowLog.stream = System.out;
        shadowApplication = Shadows.shadowOf(RuntimeEnvironment.application);

        mActivity = Robolectric.buildActivity(Activity.class).create().visible().get();
        // Fake UnityPlayer.currentActivity
        UnityPlayer.currentActivity = mActivity;
    }

    @After
    public void tearDown() {
        // empty
    }
}
