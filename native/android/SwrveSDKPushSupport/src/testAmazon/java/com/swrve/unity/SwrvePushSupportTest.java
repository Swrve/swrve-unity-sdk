package com.swrve.unity;

import android.content.Intent;
import android.os.Build;
import android.os.Bundle;
import android.support.annotation.RequiresApi;

import com.swrve.sdk.SwrvePushSDK;
import com.swrve.unity.adm.MainActivity;
import com.swrve.unity.adm.SwrveAdmPushSupport;
import com.unity3d.player.UnityPlayer;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;
import org.robolectric.Robolectric;
import org.robolectric.Shadows;
import org.robolectric.annotation.Config;

public class SwrvePushSupportTest extends SwrveBasePushSupportTest {

    @Before
    public void setUp() throws Exception {
        super.setUp();

        mActivity = Robolectric.buildActivity(MainActivity.class).create().visible().get();
        mShadowActivity = Shadows.shadowOf(mActivity);
        service = Robolectric.setupService(TestSwrveAdmIntentService.class);
        SwrvePushSDK.createInstance(mActivity);
    }

    @After
    public void tearDown() throws Exception {
        super.tearDown();
    }

    @Override
    public void serviceOnMessageReceived(Bundle bundle) {
        Intent intent = new Intent();
        bundle.putString("_s.t", "1000");
        intent.putExtras(bundle);
        ((TestSwrveAdmIntentService)service).onMessage(intent);
    }

    @Config(sdk = Build.VERSION_CODES.O)
    @RequiresApi(api = Build.VERSION_CODES.O)
    @Test
    public void testNotificationChannel() throws NoSuchFieldException, IllegalAccessException {
        String channelId = "default-channel-id";
        String channelName = "default-channel-name";

        // Emulate call to register from the C# layer
        UnityPlayer.currentActivity = mActivity;
        SwrveAdmPushSupport.initialiseAdm("gameObject", "appTitle", "common_google_signin_btn_icon_dark", "common_full_open_on_phone", "largeIconId", 0, channelId, channelName, "min");
        Robolectric.flushForegroundThreadScheduler();

        testNotificationChannelAssert(channelId, channelName);
    }
}
