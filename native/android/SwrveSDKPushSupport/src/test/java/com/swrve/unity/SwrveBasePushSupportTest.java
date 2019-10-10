package com.swrve.unity;

import android.app.Activity;
import android.app.Notification;
import android.app.NotificationManager;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Build;
import android.os.Bundle;
import android.preference.PreferenceManager;
import android.support.v4.app.NotificationCompat;

import com.swrve.sdk.SwrveNotificationBuilder;
import com.swrve.sdk.SwrveNotificationConstants;
import com.swrve.sdk.SwrveUnityCommon;
import com.swrve.sdk.SwrveUnityCommonHelper;
import com.unity3d.player.UnityPlayer;

import org.json.JSONArray;
import org.json.JSONObject;
import org.junit.Assert;
import org.junit.Test;
import org.robolectric.RuntimeEnvironment;
import org.robolectric.annotation.Config;
import org.robolectric.shadows.ShadowActivity;
import org.robolectric.shadows.ShadowApplication;
import org.robolectric.shadows.ShadowNotification;

import java.util.Date;
import java.util.List;

import androidx.test.core.app.ApplicationProvider;

import static com.swrve.sdk.SwrveNotificationConstants.SOUND_DEFAULT;
import static com.swrve.sdk.SwrveNotificationConstants.SOUND_KEY;
import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertTrue;
import static org.robolectric.Shadows.shadowOf;

public abstract class SwrveBasePushSupportTest extends SwrveBaseTest {

    protected Activity mActivity;
    protected ShadowActivity mShadowActivity;
    protected Service service;

    public abstract void serviceOnMessageReceived(Bundle bundle);

    @Test
    public void testNotifications()  {

        new SwrveUnityCommon(ApplicationProvider.getApplicationContext());
        String msgText = "hello";
        Bundle bundle = new Bundle();
        bundle.putString("_p", "10");
        bundle.putString(SwrveNotificationConstants.TEXT_KEY, msgText);
        bundle.putString("custom", "key");

        serviceOnMessageReceived(bundle);

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

    @Config(sdk = Build.VERSION_CODES.LOLLIPOP)
    @Test
    public void testCreateNotificationBuilderWithPrefs() {

        new SwrveUnityCommon(ApplicationProvider.getApplicationContext());
        String msgTitle = "Swrve";
        String msgText = "hello";
        String icon = "common_google_signin_btn_icon_dark";
        String materialIcon = "common_full_open_on_phone";
        int iconId = mActivity.getResources().getIdentifier(materialIcon, "drawable", mActivity.getPackageName());
        int colorId = 1;

        SharedPreferences prefs = PreferenceManager.getDefaultSharedPreferences(mActivity);
        prefs.edit().putString("icon_id", icon).commit();
        prefs.edit().putString("material_icon_id", materialIcon).commit();
        prefs.edit().putInt("accent_color", colorId).commit();

        Bundle extras = new Bundle();
        extras.putString(SOUND_KEY, SOUND_DEFAULT);
        SwrveNotificationBuilder swrveNotificationBuilder = SwrvePushSupport.createSwrveNotificationBuilder(mActivity, prefs);
        NotificationCompat.Builder builder = swrveNotificationBuilder.build(msgText, extras, SwrveUnityCommonHelper.getGenericEventCampaignTypePush(), null);
        assertNotification(builder, msgTitle, msgText, iconId, colorId, "content://settings/system/notification_sound");
    }

    @Test
    public void testCreateNotificationBuilderWithDefaults() {

        new SwrveUnityCommon(ApplicationProvider.getApplicationContext());
        String msgTitle = "Swrve";
        String msgText = "hello";
        int iconId = 0;

        SharedPreferences prefs = PreferenceManager.getDefaultSharedPreferences(mActivity);
        prefs.edit().clear().commit();

        SwrveNotificationBuilder swrveNotificationBuilder = SwrvePushSupport.createSwrveNotificationBuilder(mActivity, prefs);
        NotificationCompat.Builder builder = swrveNotificationBuilder.build(msgText, new Bundle(), SwrveUnityCommonHelper.getGenericEventCampaignTypePush(), null);
        assertNotification(builder, msgTitle, msgText, iconId, 0, null);
    }

    @Test
    public void testSilentPush() throws Exception {

        ShadowApplication shadowApplication = ShadowApplication.getInstance();
        long nowMilliseconds = new Date().getTime();

        Bundle bundle = new Bundle();
        bundle.putString("_sp", "10");
        bundle.putString("_siw", "720");
        String rawJson = "{\"key\":\"value\"}";
        bundle.putString("_s.SilentPayload", rawJson);

        serviceOnMessageReceived(bundle);

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

    protected void assertNotification(NotificationCompat.Builder builder, String title, String text, int icon, int colorId, String sound)  {
        Notification notification = builder.build();
        ShadowNotification shadowNotification = shadowOf(notification);
        assertEquals("Notification title is wrong", title, shadowNotification.getContentTitle());
        assertEquals("Notification content text is wrong", text, shadowNotification.getContentText());
        assertEquals("Notification icon is wrong", icon, notification.icon);
        assertEquals("Notification icon is wrong", colorId, builder.getColor());
        assertEquals("Notification sound is wrong",  sound, notification.sound == null ? null : notification.sound.toString());
    }
}
