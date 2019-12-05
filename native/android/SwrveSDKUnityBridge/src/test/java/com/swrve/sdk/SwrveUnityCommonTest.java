package com.swrve.sdk;

import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.content.Context;
import android.content.SharedPreferences;
import android.os.Build;
import androidx.annotation.RequiresApi;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;
import org.junit.Test;
import org.robolectric.annotation.Config;

import androidx.test.core.app.ApplicationProvider;

import static org.junit.Assert.assertEquals;

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
        SharedPreferences prefs = ApplicationProvider.getApplicationContext().getSharedPreferences(SwrveUnityCommon.SHARED_PREFERENCE_FILENAME, Context.MODE_PRIVATE);
        SwrveUnityCommon swrveCommon = new SwrveUnityCommon(ApplicationProvider.getApplicationContext());

        // Save 2 Notifications
        swrveCommon.saveNotificationAuthenticated(0);
        swrveCommon.saveNotificationAuthenticated(1);

        // Check if they are on cache and its content.
        JSONArray jsonArray = new JSONArray(prefs.getString(SwrveUnityCommon.NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY, "[]"));
        for (int i = 0; i < jsonArray.length(); i++) {
            JSONObject notification = jsonArray.getJSONObject(i);
            assertEquals(i, notification.get("id"));
        }
        assertEquals(jsonArray.length(), 2);

        // Purge the notifications.
        swrveCommon.clearAllAuthenticatedNotifications();
        jsonArray = new JSONArray(prefs.getString(SwrveUnityCommon.NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY, "[]"));
        assertEquals(0, jsonArray.length());
    }

    @Test
    public void testMaxAuthenticatedCacheNotifications() throws JSONException {
        SharedPreferences prefs = ApplicationProvider.getApplicationContext().getSharedPreferences(SwrveUnityCommon.SHARED_PREFERENCE_FILENAME, Context.MODE_PRIVATE);
        SwrveUnityCommon swrveCommon = new SwrveUnityCommon(ApplicationProvider.getApplicationContext());

        int numberOfNotifications = SwrveUnityCommon.MAX_CACHED_AUTHENTICATED_NOTIFICATIONS + 5;
        // Try save more notifications than should be supported by our cache system.
        for (int i = 0; i < numberOfNotifications; i++) {
            swrveCommon.saveNotificationAuthenticated(i);
        }

        //Check the amount of notifications that are available on cache!
        JSONArray jsonArray = new JSONArray(prefs.getString(SwrveUnityCommon.NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY, "[]"));
        assertEquals(SwrveUnityCommon.MAX_CACHED_AUTHENTICATED_NOTIFICATIONS, jsonArray.length());
    }
}
