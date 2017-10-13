package com.swrve.unity;

import android.os.Build;
import android.os.Bundle;
import android.support.annotation.RequiresApi;

import com.swrve.sdk.SwrvePushSDK;
import com.google.firebase.messaging.RemoteMessage;
import com.swrve.unity.firebase.MainActivity;
import com.swrve.unity.firebase.SwrveFirebaseDeviceRegistration;
import com.swrve.unity.firebase.SwrveFirebaseMessagingService;
import com.unity3d.player.UnityPlayer;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;
import org.robolectric.Robolectric;
import org.robolectric.Shadows;
import org.robolectric.annotation.Config;

import java.lang.reflect.Constructor;

import static junit.framework.Assert.fail;

public class SwrvePushSupportTest extends SwrveBasePushSupportTest {

    @Before
    public void setUp() throws Exception {
        super.setUp();

        mActivity = Robolectric.buildActivity(MainActivity.class).create().visible().get();
        mShadowActivity = Shadows.shadowOf(mActivity);
        service = Robolectric.setupService(SwrveFirebaseMessagingService.class);
        SwrvePushSDK.createInstance(mActivity);
    }

    @After
    public void tearDown() throws Exception {
        super.tearDown();
    }

    @Override
    public void serviceOnMessageReceived(Bundle bundle) {
        try {
            Constructor<RemoteMessage> constructor = RemoteMessage.class.getDeclaredConstructor(Bundle.class);
            constructor.setAccessible(true);
            RemoteMessage message = constructor.newInstance(bundle);
            ((SwrveFirebaseMessagingService) service).onMessageReceived(message);
        } catch(Exception exp) {
            System.err.println(exp.toString());
            fail();
        }
    }

    @Config(sdk = Build.VERSION_CODES.O)
    @RequiresApi(api = Build.VERSION_CODES.O)
    @Test
    public void testNotificationChannel() throws NoSuchFieldException, IllegalAccessException {
        String channelId = "default-channel-id";
        String channelName = "default-channel-name";

        // Emulate call to register from the C# layer
        UnityPlayer.currentActivity = mActivity;
        SwrveFirebaseDeviceRegistration.registerDevice("gameObject", "appTitle", "common_google_signin_btn_icon_dark", "common_full_open_on_phone", "largeIconId", 0, channelId, channelName, "min");
        Robolectric.flushForegroundThreadScheduler();

        testNotificationChannelAssert(channelId, channelName);
    }
}
