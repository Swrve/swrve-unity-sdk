package com.swrve.unity;

import android.app.Notification;
import android.app.NotificationManager;
import android.content.Context;
import android.content.SharedPreferences;
import android.os.Build;
import android.os.Bundle;
import android.preference.PreferenceManager;
import android.support.annotation.RequiresApi;
import android.support.v4.app.NotificationCompat;

import com.swrve.sdk.SwrvePushSDK;
import com.swrve.unity.gcm.MainActivity;
import com.swrve.unity.gcm.SwrveGcmDeviceRegistration;
import com.swrve.unity.gcm.SwrveGcmIntentService;
import com.swrve.unity.swrvesdkpushsupport.R;
import com.unity3d.player.UnityPlayer;

import junit.framework.Assert;
import static junit.framework.Assert.assertEquals;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;
import org.robolectric.Robolectric;
import org.robolectric.RuntimeEnvironment;
import org.robolectric.Shadows;
import org.robolectric.annotation.Config;

import java.util.List;

public class SwrvePushSupportTest extends SwrveBasePushSupportTest {

    @Before
    public void setUp() throws Exception {
        super.setUp();

        mActivity = Robolectric.buildActivity(MainActivity.class).create().visible().get();
        mShadowActivity = Shadows.shadowOf(mActivity);
        service = Robolectric.setupService(SwrveGcmIntentService.class);
        SwrvePushSDK.createInstance(mActivity);
    }

    @After
    public void tearDown() throws Exception {
        super.tearDown();
    }

    @Test
    public void testNotifications() throws InterruptedException {

        String msgText = "hello";
        SwrveGcmIntentService service = Robolectric.setupService(SwrveGcmIntentService.class);
        Bundle bundle = new Bundle();
        bundle.putString("_p", "10");
        bundle.putString("text", msgText);
        bundle.putString("custom", "key");

        service.onMessageReceived(null, bundle);

        // Check a notification has been shown
        NotificationManager notificationManager = (NotificationManager) RuntimeEnvironment.application.getSystemService(Context.NOTIFICATION_SERVICE);
        List<Notification> notifications = Shadows.shadowOf(notificationManager).getAllNotifications();
        Assert.assertEquals(1, notifications.size());
        Notification notification = notifications.get(0);
        assertEquals(msgText, notification.tickerText);

        // Check there was no influenced data added
        // Fake UnityPlayer.currentActivity
        UnityPlayer.currentActivity = mActivity;
        String influencedData = SwrvePushSupport.getInfluenceDataJson();
        assertEquals("[]", influencedData);
    }

    @Test
    public void testCreateNotificationBuilderWithPrefs() {

        String msgTitle = "My Awesome App Title";
        String msgText = "hello";
        String icon = "common_google_signin_btn_icon_dark";
        String materialIcon = "common_full_open_on_phone";
        int materialIconId = R.drawable.common_full_open_on_phone;
        int colorId = R.color.common_google_signin_btn_text_dark;

        SharedPreferences prefs = PreferenceManager.getDefaultSharedPreferences(mActivity);
        prefs.edit().putString(SwrvePushSupport.PROPERTY_APP_TITLE, msgTitle).commit();
        prefs.edit().putString(SwrvePushSupport.PROPERTY_ICON_ID, icon).commit();
        prefs.edit().putString(SwrvePushSupport.PROPERTY_MATERIAL_ICON_ID, materialIcon).commit();
        prefs.edit().putInt(SwrvePushSupport.PROPERTY_ACCENT_COLOR, colorId).commit();

        Bundle extras = new Bundle();
        extras.putString("sound", "default");
        NotificationCompat.Builder builder = SwrvePushSupport.createNotificationBuilder(mActivity, prefs, msgText, extras, 0);
        assertNotification(builder, msgTitle, msgText, materialIconId, colorId, "content://settings/system/notification_sound");
    }

    @Test
    public void testCreateNotificationBuilderWithDefaults() {

        String msgTitle = "com.swrve.unity.swrvesdkpushsupport";
        String msgText = "hello";
        int iconId = 0;

        SharedPreferences prefs = PreferenceManager.getDefaultSharedPreferences(mActivity);
        prefs.edit().clear().commit();

        NotificationCompat.Builder builder = SwrvePushSupport.createNotificationBuilder(mActivity, prefs, msgText, new Bundle(), 0);
        assertNotification(builder, msgTitle, msgText, iconId, 0, null);
    }

    @Config(sdk = Build.VERSION_CODES.O)
    @RequiresApi(api = Build.VERSION_CODES.O)
    @Test
    public void testNotificationChannel() throws NoSuchFieldException, IllegalAccessException {
        String channelId = "default-channel-id";
        String channelName = "default-channel-name";

        // Emulate call to register from the C# layer
        UnityPlayer.currentActivity = mActivity;
        SwrveGcmDeviceRegistration.registerDevice("gameObject", "senderId", "appTitle", "common_google_signin_btn_icon_dark", "common_full_open_on_phone", "largeIconId", 0, channelId, channelName, "min");
        Robolectric.flushForegroundThreadScheduler();

        testNotificationChannelAssert(channelId, channelName);
    }

    @Override
    public void serviceOnMessageReceived(Bundle bundle) {
        ((SwrveGcmIntentService)service).onMessageReceived(null, bundle);
    }
}
