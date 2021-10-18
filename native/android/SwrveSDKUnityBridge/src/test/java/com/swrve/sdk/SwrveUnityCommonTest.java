package com.swrve.sdk;

import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.content.Context;
import android.content.SharedPreferences;
import android.os.Build;

import androidx.annotation.RequiresApi;
import androidx.test.core.app.ApplicationProvider;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;
import org.junit.Test;
import org.robolectric.annotation.Config;

import static com.swrve.sdk.SwrveUnityCommon.SHARED_PREFERENCE_FILENAME;
import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;

public class SwrveUnityCommonTest extends SwrveBaseTest {

    @Config(sdk = Build.VERSION_CODES.O)
    @RequiresApi(api = Build.VERSION_CODES.O)
    @Test
    public void testDefaultNotificationChannel() {
        String channelId = "default-channel-id";
        String channelName = "default-channel-name";
        String channelImportance = "low";

        // Emulate call to register from the C# layer
        SwrveUnityCommon swrveCommon = new SwrveUnityCommon(ApplicationProvider.getApplicationContext());
        swrveCommon.setDefaultNotificationChannel(channelId, channelName, channelImportance);
        NotificationChannel notificationChannel = swrveCommon.getDefaultNotificationChannel();
        assertEquals(channelId, notificationChannel.getId());
        assertEquals(channelName, notificationChannel.getName());
        assertEquals(NotificationManager.IMPORTANCE_LOW, notificationChannel.getImportance());
    }

    @Test
    public void testSaveAndClearNotificationsId() throws JSONException {
        // Test setup.
        SharedPreferences prefs = ApplicationProvider.getApplicationContext().getSharedPreferences(SHARED_PREFERENCE_FILENAME, Context.MODE_PRIVATE);
        SwrveUnityCommon swrveCommon = new SwrveUnityCommon(ApplicationProvider.getApplicationContext());

        // Save 2 Notifications
        swrveCommon.saveNotificationAuthenticated(0);
        swrveCommon.saveNotificationAuthenticated(1);

        // Check if they are on cache and its content.
        JSONArray jsonArray = new JSONArray(prefs.getString(SwrveUnityCommon.PREF_NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY, "[]"));
        for (int i = 0; i < jsonArray.length(); i++) {
            JSONObject notification = jsonArray.getJSONObject(i);
            assertEquals(i, notification.get("id"));
        }
        assertEquals(jsonArray.length(), 2);

        // Purge the notifications.
        swrveCommon.clearAllAuthenticatedNotifications();
        jsonArray = new JSONArray(prefs.getString(SwrveUnityCommon.PREF_NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY, "[]"));
        assertEquals(0, jsonArray.length());
    }

    @Test
    public void testMaxAuthenticatedCacheNotifications() throws JSONException {
        SharedPreferences prefs = ApplicationProvider.getApplicationContext().getSharedPreferences(SHARED_PREFERENCE_FILENAME, Context.MODE_PRIVATE);
        SwrveUnityCommon swrveCommon = new SwrveUnityCommon(ApplicationProvider.getApplicationContext());

        int numberOfNotifications = SwrveUnityCommon.MAX_CACHED_AUTHENTICATED_NOTIFICATIONS + 5;
        // Try save more notifications than should be supported by our cache system.
        for (int i = 0; i < numberOfNotifications; i++) {
            swrveCommon.saveNotificationAuthenticated(i);
        }

        //Check the amount of notifications that are available on cache!
        JSONArray jsonArray = new JSONArray(prefs.getString(SwrveUnityCommon.PREF_NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY, "[]"));
        assertEquals(SwrveUnityCommon.MAX_CACHED_AUTHENTICATED_NOTIFICATIONS, jsonArray.length());
    }

    @Test
    public void testIsTrackingStateStopped() {

        SharedPreferences sharedPreferences = ApplicationProvider.getApplicationContext().getSharedPreferences(SHARED_PREFERENCE_FILENAME, Context.MODE_PRIVATE);
        String detailsTrue = getDummyDetails(true, "myuserid");
        String detailsFalse = getDummyDetails(false, "myuserid");

        // Save default values to cache with isTrackingStateStopped true
        sharedPreferences.edit().putString("UnitySwrveCommon", detailsTrue).apply();
        SwrveUnityCommon swrveCommon = new SwrveUnityCommon(ApplicationProvider.getApplicationContext());
        assertTrue(swrveCommon.isTrackingStateStopped());

        // Save default values to cache with isTrackingStateStopped false
        sharedPreferences.edit().putString("UnitySwrveCommon", detailsFalse).apply();
        swrveCommon = new SwrveUnityCommon(ApplicationProvider.getApplicationContext());
        assertFalse(swrveCommon.isTrackingStateStopped());

        // Construct SwrveUnityCommon with isTrackingStateStopped true
        swrveCommon = new SwrveUnityCommon(detailsTrue);
        assertTrue(swrveCommon.isTrackingStateStopped());

        // Construct SwrveUnityCommon with isTrackingStateStopped false
        swrveCommon = new SwrveUnityCommon(detailsFalse);
        assertFalse(swrveCommon.isTrackingStateStopped());

        // setTrackingStateStopped true
        swrveCommon.setTrackingStateStopped(true);
        assertTrue(swrveCommon.isTrackingStateStopped());
        swrveCommon = new SwrveUnityCommon(ApplicationProvider.getApplicationContext());
        assertTrue(swrveCommon.isTrackingStateStopped());

        // setTrackingStateStopped false
        swrveCommon.setTrackingStateStopped(false);
        assertFalse(swrveCommon.isTrackingStateStopped());
        swrveCommon = new SwrveUnityCommon(ApplicationProvider.getApplicationContext());
        assertFalse(swrveCommon.isTrackingStateStopped());
    }

    @Test
    public void testSetUserId() {

        // create SwrveUnityCommon from unity engine with json details
        String details = getDummyDetails(false, "myuserid");
        SwrveUnityCommon swrveCommon = new SwrveUnityCommon(details);
        assertEquals("myuserid", swrveCommon.getUserId());

        // set new userId
        swrveCommon.setUserId("newUserId");
        assertEquals("newUserId", swrveCommon.getUserId());

        // create SwrveUnityCommon from background thread such as receiving a push
        swrveCommon = new SwrveUnityCommon(ApplicationProvider.getApplicationContext());
        assertEquals("newUserId", swrveCommon.getUserId());
    }

    private String getDummyDetails(boolean isTrackingStateStopped, String aUserId) {
// @formatter:off
        String details = "{" +
                "\"sdkVersion\":\"8.0.0\"," +
                "\"apiKey\":\"fake_key\"," +
                "\"appId\":1234," +
                "\"userId\":\"" + aUserId + "\"," +
                "\"isTrackingStateStopped\":" + isTrackingStateStopped + "," +
                "\"deviceId\":\"dkbsdvbsdkjvnsdfkjnvskj\"," +
                "\"appVersion\":\"8.1.1-APP_BUILD_INFO\"," +
                "\"uniqueKey\":\"xfvdfvdfbdbdsfgbdsbsdb\"," +
                "\"deviceInfo\":{" +
                    "\"swrve.device_name\":\"Google Pixel 5\"," +
                    "\"swrve.os\":\"android\"," +
                    "\"swrve.device_width\":\"0\"," +
                    "\"swrve.device_height\":\"0\"," +
                    "\"swrve.device_dpi\":\"440\"," +
                    "\"swrve.language\":\"en-IE\"," +
                    "\"swrve.os_version\":\"Android OS 11 / API-30 (RQ3A.210705.001/7380771)\"," +
                    "\"swrve.app_store\":\"google\"," +
                    "\"swrve.sdk_version\":\"Unity 8.0.0\"," +
                    "\"swrve.unity_version\":\"2020.3.1f1\"," +
                    "\"swrve.install_date\":\"20210804\"," +
                    "\"swrve.tracking_state\":\"STOPPED\"," +
                    "\"swrve.device_type\":\"mobile\"," +
                    "\"swrve.utc_offset_seconds\":\"3600\"," +
                    "\"swrve.can_receive_authenticated_push\":\"true\"," +
                    "\"swrve.timezone_name\":\"Europe/Dublin\"," +
                    "\"swrve.device_region\":\"IE\"," +
                    "\"swrve.android_id\":\"0ab5fedeb26f6900\"," +
                    "\"swrve.permission.notifications_enabled\":\"True\"," +
                    "\"swrve.sdk_init_mode\":\"auto_auto\"" +
                "}," +
                "\"batchUrl\":\"/1/batch\"," +
                "\"eventsServer\":\"https://1234.api.swrve.com\"," +
                "\"contentServer\":\"https://1234.content.swrve.com\"," +
                "\"httpTimeout\":60000," +
                "\"maxEventsPerFlush\":50," +
                "\"swrvePath\":\"/storage/emulated/0/Android/data/com.swrve.unity.firebase/files\"," +
                "\"prefabName\":\"SDKContainer\"," +
                "\"swrveTemporaryPath\":\"/storage/emulated/0/Android/data/com.swrve.unity.firebase/cache\"," +
                "\"sigSuffix\":\"_SGT\"" +
            "}";
// @formatter:on
        return details;
    }
}
