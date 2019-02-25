package com.swrve.unity.adm;

import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;

import com.amazon.device.messaging.ADMMessageHandlerBase;
import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrveLogger;
import com.swrve.sdk.SwrveNotificationConstants;
import com.swrve.unity.SwrveUnityPushServiceManager;

import java.util.LinkedList;

public class SwrveAdmIntentService extends ADMMessageHandlerBase {
    private final static String AMAZON_RECENT_PUSH_IDS = "recent_push_ids";
    private final static String AMAZON_PREFERENCES = "swrve_amazon_unity_pref";
    protected final int DEFAULT_PUSH_ID_CACHE_SIZE = 16;

    public SwrveAdmIntentService() {
        super(SwrveAdmIntentService.class.getName());
    }

    public SwrveAdmIntentService(final String className) {
        super(className);
    }

    @Override
    protected void onMessage(final Intent intent) {
        if (intent == null) {
            SwrveLogger.e("Unexpected null intent");
            return;
        }

        final Bundle pushBundle = intent.getExtras();
        if (pushBundle != null && !pushBundle.isEmpty()) {  // has effect of unparcelling Bundle
            SwrveLogger.i("Received ADM notification: %s", pushBundle.toString());

            // Deduplicate notification
            final String timestamp = pushBundle.getString(SwrveNotificationConstants.TIMESTAMP_KEY);
            if (SwrveHelper.isNullOrEmpty(timestamp)) {
                SwrveLogger.e("ADM notification: but not processing as it's missing %s", SwrveNotificationConstants.TIMESTAMP_KEY);
                return;
            }

            // Get tracking key
            Object rawId = pushBundle.get(SwrveNotificationConstants.SWRVE_TRACKING_KEY);
            String silentId = SwrveHelper.getSilentPushId(pushBundle);
            if (rawId == null) {
                rawId = silentId;
            }
            String msgId = (rawId != null) ? rawId.toString() : null;

            // Check for duplicates. This is a necessary part of using ADM which might clone
            // a message as part of attempting to deliver it. We de-dupe by
            // checking against the tracking id and timestamp. (Multiple pushes with the same
            // tracking id are possible in some scenarios from Swrve).
            // Id is concatenation of tracking key and timestamp "$_p:$_s.t"
            String curId = msgId + ":" + timestamp;
            LinkedList<String> recentIds = getRecentNotificationIdCache();
            if (recentIds.contains(curId)) {
                SwrveLogger.i("ADM notification: but not processing because duplicate Id: %s", curId);
                return;
            }

            // Try get de-dupe cache size
            int pushIdCacheSize = pushBundle.getInt(SwrveNotificationConstants.PUSH_ID_CACHE_SIZE_KEY, DEFAULT_PUSH_ID_CACHE_SIZE);

            // No duplicate found. Update the cache.
            updateRecentNotificationIdCache(recentIds, curId, pushIdCacheSize);

            new SwrveUnityPushServiceManager(this).processRemoteNotification(pushBundle);
        }
    }

    @Override
    protected void onRegistrationError(final String string) {
        SwrveLogger.e("ADM Registration Error. Error string %s", string);
    }

    @Override
    protected void onRegistered(final String registrationId) {
        SwrveLogger.i("ADM Registered. RegistrationId: %s", registrationId);
        Context context = getApplicationContext();
        SwrveAdmPushSupport.onPushTokenUpdated(context, registrationId);
    }

    @Override
    protected void onUnregistered(final String registrationId) {
        SwrveLogger.i("ADM Unregistered. RegistrationId: %s", registrationId);
    }

    private LinkedList<String> getRecentNotificationIdCache() {
        Context context = getApplicationContext();
        SharedPreferences sharedPreferences = context.getSharedPreferences(AMAZON_PREFERENCES, Context.MODE_PRIVATE);
        String jsonString = sharedPreferences.getString(AMAZON_RECENT_PUSH_IDS, "");
        Gson gson = new Gson();
        LinkedList<String> recentIds = gson.fromJson(jsonString, new TypeToken<LinkedList<String>>() {}.getType());
        recentIds = recentIds == null ? new LinkedList<String>() : recentIds;
        return recentIds;
    }

    private void updateRecentNotificationIdCache(LinkedList<String> recentIds, String newId, int maxCacheSize) {
        // Update queue
        recentIds.add(newId);

        // This must be at least zero
        maxCacheSize = Math.max(0, maxCacheSize);

        // Maintain cache size limit
        while (recentIds.size() > maxCacheSize) {
            recentIds.remove();
        }

        // Store latest queue to shared preferences
        Context context = getApplicationContext();
        Gson gson = new Gson();
        String recentNotificationsJson = gson.toJson(recentIds);
        SharedPreferences sharedPreferences = context.getSharedPreferences(AMAZON_PREFERENCES, Context.MODE_PRIVATE);
        sharedPreferences.edit().putString(AMAZON_RECENT_PUSH_IDS, recentNotificationsJson).apply();
    }
}
