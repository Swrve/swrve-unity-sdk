package com.swrve.unity;

import android.app.Activity;
import android.app.Notification;
import android.app.NotificationManager;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.preference.PreferenceManager;
import android.support.v4.app.NotificationCompat;

import com.swrve.unity.gcm.MainActivity;
import com.swrve.unity.gcm.SwrveGcmIntentService;
import com.swrve.unity.swrvesdkpushsupport.R;
import com.unity3d.player.UnityPlayer;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;
import org.junit.After;
import org.junit.Assert;
import org.junit.Before;
import org.junit.Test;
import org.robolectric.Robolectric;
import org.robolectric.RuntimeEnvironment;
import org.robolectric.Shadows;
import org.robolectric.shadows.ShadowApplication;
import org.robolectric.shadows.ShadowNotification;

import java.util.Date;
import java.util.List;

import static junit.framework.Assert.assertEquals;
import static junit.framework.Assert.assertTrue;
import static org.robolectric.Shadows.shadowOf;

public class SwrvePushSupportTest extends SwrveBaseTest {

    protected Activity mActivity;

    @Before
    public void setUp() throws Exception {
        super.setUp();

        mActivity = Robolectric.buildActivity(MainActivity.class).create().visible().get();
        mShadowActivity = Shadows.shadowOf(mActivity);
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
        List<Notification> notifications = shadowOf(notificationManager).getAllNotifications();
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
        NotificationCompat.Builder builder = SwrvePushSupport.createNotificationBuilder(mActivity, prefs, msgText, extras);
        assertNotification(builder, msgTitle, msgText, materialIconId, colorId, "content://settings/system/notification_sound");
    }

    @Test
    public void testCreateNotificationBuilderWithDefaults() {

        String msgTitle = "com.swrve.unity.swrvesdkpushsupport";
        String msgText = "hello";
        int iconId = 0;

        SharedPreferences prefs = PreferenceManager.getDefaultSharedPreferences(mActivity);
        prefs.edit().clear().commit();

        NotificationCompat.Builder builder = SwrvePushSupport.createNotificationBuilder(mActivity, prefs, msgText, new Bundle());
        assertNotification(builder, msgTitle, msgText, iconId, 0, null);
    }

    @Test
    public void testSilentPush() throws InterruptedException, JSONException {

        ShadowApplication shadowApplication = ShadowApplication.getInstance();
        long nowMilliseconds = new Date().getTime();

        SwrveGcmIntentService service = Robolectric.setupService(SwrveGcmIntentService.class);
        Bundle bundle = new Bundle();
        bundle.putString("_sp", "10");
        bundle.putString("_siw", "720");
        String rawJson = "{\"key\":\"value\"}";
        bundle.putString("_s.SilentPayload", rawJson);

        service.onMessageReceived(null, bundle);

        List<Intent> intents = shadowApplication.getBroadcastIntents();
        assertEquals(1, intents.size());
        Intent intent = intents.get(0);
        Bundle silentBundle = intent.getExtras();
        assertEquals(false, silentBundle.containsKey("_sp"));
        assertEquals(false, silentBundle.containsKey("_siw"));
        assertEquals(rawJson, silentBundle.getString("_s.SilentPayload"));

        // Test no notification was shown
        NotificationManager notificationManager = (NotificationManager) RuntimeEnvironment.application.getSystemService(Context.NOTIFICATION_SERVICE);
        List<Notification> notifications = shadowOf(notificationManager).getAllNotifications();
        Assert.assertEquals(0, notifications.size());

        // Test that influence data was written
        // Fake UnityPlayer.currentActivity
        UnityPlayer.currentActivity = mActivity;
        String influencedData = SwrvePushSupport.getInfluenceDataJson();
        JSONArray influenceArray = new JSONArray(influencedData);
        assertEquals(1, influenceArray.length());
        JSONObject influenceObject = influenceArray.getJSONObject(0);
        assertEquals("10", influenceObject.getString("trackingId"));
        long maxInfluencedMillis = influenceObject.getLong("maxInfluencedMillis");
        assertTrue(maxInfluencedMillis >= nowMilliseconds);

        influencedData = SwrvePushSupport.getInfluenceDataJson();
        assertEquals("[]", influencedData);
    }

    private void assertNotification(NotificationCompat.Builder builder, String title, String text, int icon, int colorId, String sound)  {
        Notification notification = builder.build();
        ShadowNotification shadowNotification = shadowOf(notification);
        assertEquals("Notification title is wrong", title, shadowNotification.getContentTitle());
        assertEquals("Notification content text is wrong", text, shadowNotification.getContentText());
        assertEquals("Notification icon is wrong", icon, notification.icon);
        assertEquals("Notification icon is wrong", colorId, builder.getColor());
        assertEquals("Notification sound is wrong",  sound, notification.sound == null ? null : notification.sound.toString());
    }
}
