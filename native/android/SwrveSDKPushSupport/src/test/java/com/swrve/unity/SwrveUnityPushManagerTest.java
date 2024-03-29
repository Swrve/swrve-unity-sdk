package com.swrve.unity;

import android.Manifest;
import android.app.Activity;
import android.app.Notification;
import android.app.NotificationManager;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Build;
import android.os.Bundle;
import android.preference.PreferenceManager;

import androidx.core.app.NotificationCompat;
import androidx.test.core.app.ApplicationProvider;

import com.swrve.sdk.SwrveNotificationBuilder;
import com.swrve.sdk.SwrveNotificationConstants;
import com.swrve.sdk.SwrveUnityCommon;
import com.swrve.sdk.SwrveUnityCommonHelper;
import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;

import org.json.JSONArray;
import org.json.JSONObject;
import org.junit.Assert;
import org.junit.Before;
import org.junit.Test;
import org.robolectric.Robolectric;
import org.robolectric.RuntimeEnvironment;
import org.robolectric.annotation.Config;
import org.robolectric.shadows.ShadowApplication;
import org.robolectric.shadows.ShadowNotification;

import java.util.Date;
import java.util.List;

import static com.swrve.sdk.SwrveNotificationConstants.SOUND_DEFAULT;
import static com.swrve.sdk.SwrveNotificationConstants.SOUND_KEY;
import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertTrue;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyBoolean;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.atLeastOnce;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.spy;
import static org.mockito.Mockito.verify;
import static org.robolectric.Shadows.shadowOf;

public class SwrveUnityPushManagerTest extends SwrveBaseTest {

    protected Activity mActivity;

    @Before
    public void setUp() throws Exception {
        super.setUp();
        shadowApplication.grantPermissions(Manifest.permission.POST_NOTIFICATIONS);
        mActivity = Robolectric.buildActivity(UnityPlayerActivity.class).create().visible().get();
        new SwrveUnityCommon(ApplicationProvider.getApplicationContext());
    }

    @Test
    public void testNotifications() throws Exception {
        long nowMilliseconds = new Date().getTime();

        String msgText = "hello";
        Bundle bundle = new Bundle();
        bundle.putString("_p", "10");
        bundle.putString("_siw", "720");
        bundle.putString(SwrveNotificationConstants.TEXT_KEY, msgText);
        bundle.putString("custom", "key");

        SwrvePushManagerUnityImp pushManagerSpy = spy(new SwrvePushManagerUnityImp(mActivity));
        doNothing().when(pushManagerSpy).sendPushDeliveredEvent(any(Bundle.class), anyBoolean(), anyString());
        pushManagerSpy.processMessage(bundle);

        verify(pushManagerSpy, atLeastOnce()).sendPushDeliveredEvent(bundle, true, "");

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
        assertCampaignInfluence(nowMilliseconds, influencedData, "10");
    }

    @Config(sdk = Build.VERSION_CODES.LOLLIPOP)
    @Test
    public void testCreateNotificationBuilderWithPrefs() {

        String msgTitle = "Swrve";
        String msgText = "hello";
        String icon = "common_google_signin_btn_icon_dark";
        String materialIcon = "common_full_open_on_phone";
        int iconId = mActivity.getResources().getIdentifier(materialIcon, "drawable", mActivity.getPackageName());
        int expectedColorInt = -13022805;

        SharedPreferences prefs = PreferenceManager.getDefaultSharedPreferences(mActivity);
        prefs.edit().putString("icon_id", icon).commit();
        prefs.edit().putString("material_icon_id", materialIcon).commit();
        prefs.edit().putString("accent_color_hex", "#3949AB").commit();

        Bundle extras = new Bundle();
        extras.putString(SOUND_KEY, SOUND_DEFAULT);
        SwrveNotificationBuilder swrveNotificationBuilder = SwrvePushSupport.createSwrveNotificationBuilder(mActivity, prefs);
        NotificationCompat.Builder builder = swrveNotificationBuilder.build(msgText, extras, SwrveUnityCommonHelper.getGenericEventCampaignTypePush(), null);
        assertNotification(builder, msgTitle, msgText, iconId, expectedColorInt, "content://settings/system/notification_sound");
    }

    @Test
    public void testCreateNotificationBuilderWithDefaults() {

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

        SwrvePushManagerUnityImp pushManagerSpy = spy(new SwrvePushManagerUnityImp(mActivity));
        doNothing().when(pushManagerSpy).sendPushDeliveredEvent(any(Bundle.class), anyBoolean(), anyString());
        pushManagerSpy.processMessage(bundle);

        verify(pushManagerSpy, atLeastOnce()).sendPushDeliveredEvent(bundle, false, "");

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
        assertCampaignInfluence(nowMilliseconds, influencedData, "10");

        influencedData = SwrvePushSupport.getInfluenceDataJson();
        assertEquals("[]", influencedData);
    }

    protected void assertNotification(NotificationCompat.Builder builder, String title, String text, int icon, int color, String sound)  {
        Notification notification = builder.build();
        ShadowNotification shadowNotification = shadowOf(notification);
        assertEquals("Notification title is wrong", title, shadowNotification.getContentTitle());
        assertEquals("Notification content text is wrong", text, shadowNotification.getContentText());
        assertEquals("Notification icon is wrong", icon, notification.icon);
        assertEquals("Notification colorHex is wrong", color, builder.getColor());
        assertEquals("Notification sound is wrong",  sound, notification.sound == null ? null : notification.sound.toString());
    }

    protected void assertCampaignInfluence(long nowMilliseconds, String influencedData, String trackingId) throws Exception {
        JSONArray influenceArray = new JSONArray(influencedData);
        assertEquals(1, influenceArray.length());
        JSONObject influenceObject = influenceArray.getJSONObject(0);
        assertEquals(trackingId, influenceObject.getString("trackingId"));
        long maxInfluencedMillis = influenceObject.getLong("maxInfluencedMillis");
        assertTrue(maxInfluencedMillis >= nowMilliseconds);
    }
}
