package com.swrve.unity.adm;

import android.app.Notification;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.support.v4.app.NotificationCompat;
import android.util.Log;
import java.util.Date;
import java.util.LinkedList;

import com.amazon.device.messaging.ADMMessageHandlerBase;
import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrvePushConstants;
import com.swrve.sdk.SwrvePushSDK;
import com.swrve.unity.SwrveNotification;
import com.swrve.unity.SwrvePushSupport;
import com.unity3d.player.UnityPlayer;

public class SwrveAdmIntentService extends ADMMessageHandlerBase {
    private final static String TAG = "SwrveAdm";
    private final static String AMAZON_RECENT_PUSH_IDS = "recent_push_ids";
    private final static String AMAZON_PREFERENCES = "swrve_amazon_unity_pref";
    protected final int DEFAULT_PUSH_ID_CACHE_SIZE = 16;

    private Integer notificationId;

    public SwrveAdmIntentService() {
        super(SwrveAdmIntentService.class.getName());
    }

    public SwrveAdmIntentService(final String className) {
        super(className);
    }

    @Override
    protected void onMessage(final Intent intent) {
        if (intent == null) {
            Log.e(TAG, "Unexpected null intent");
            return;
        }

        final Bundle extras = intent.getExtras();
        if (extras != null && !extras.isEmpty()) {  // has effect of unparcelling Bundle
            Log.i(TAG, "Received ADM notification: " + extras.toString());
            processRemoteNotification(extras);
        }
    }

    @Override
    protected void onRegistrationError(final String string) {
        // This is considered fatal for ADM
        Log.e(TAG, "ADM Registration Error. Error string: " + string);
    }

    @Override
    protected void onRegistered(final String registrationId) {
        Log.i(TAG, "ADM Registered. RegistrationId: " + registrationId);
        Context context = getApplicationContext();
        SwrveAdmPushSupport.onPushTokenUpdated(context, registrationId);
    }

    @Override
    protected void onUnregistered(final String registrationId) {
        Log.i(TAG, "ADM Unregistered. RegistrationId: " + registrationId);
    }

    private void processRemoteNotification(Bundle msg) {
        try {
            if (!SwrvePushSDK.isSwrveRemoteNotification(msg)) {
                Log.i(TAG, "ADM notification: but not processing as it doesn't contain:" + SwrvePushConstants.SWRVE_TRACKING_KEY);
                return;
            }

            // Deduplicate notification
            // Get tracking key
            Object rawId = msg.get(SwrvePushConstants.SWRVE_TRACKING_KEY);
            String silentId = SwrvePushSDK.getSilentPushId(msg);
            if (rawId == null) {
                rawId = silentId;
            }
            String msgId = (rawId != null) ? rawId.toString() : null;

            final String timestamp = msg.getString(SwrvePushConstants.TIMESTAMP_KEY);
            if (SwrveAdmHelper.isNullOrEmpty(timestamp)) {
                Log.e(TAG, "ADM notification: but not processing as it's missing " + SwrvePushConstants.TIMESTAMP_KEY);
                return;
            }

            // Check for duplicates. This is a necessary part of using ADM which might clone
            // a message as part of attempting to deliver it. We de-dupe by
            // checking against the tracking id and timestamp. (Multiple pushes with the same
            // tracking id are possible in some scenarios from Swrve).
            // Id is concatenation of tracking key and timestamp "$_p:$_s.t"
            String curId = msgId + ":" + timestamp;
            LinkedList<String> recentIds = getRecentNotificationIdCache();
            if (recentIds.contains(curId)) {
                //Found a duplicate
                Log.i(TAG, "ADM notification: but not processing because duplicate Id: " + curId);
                return;
            }

            // Try get de-dupe cache size
            int pushIdCacheSize = msg.getInt(SwrvePushConstants.PUSH_ID_CACHE_SIZE_KEY, DEFAULT_PUSH_ID_CACHE_SIZE);

            // No duplicate found. Update the cache.
            updateRecentNotificationIdCache(recentIds, curId, pushIdCacheSize);

            final Context context = getApplicationContext();
            if (SwrveHelper.isNullOrEmpty(silentId)) {
                // Visible push notification
                final SharedPreferences prefs = SwrveAdmPushSupport.getAdmPreferences(context);
                String activityClassName = SwrvePushSupport.getActivityClassName(context, prefs);

                // Only call this listener if there is an activity running
                if (UnityPlayer.currentActivity != null) {
                    // Call Unity SDK MonoBehaviour container
                    SwrveNotification swrveNotification = SwrveNotification.Builder.build(msg);
                    SwrveAdmPushSupport.newReceivedNotification(SwrveAdmPushSupport.getGameObject(UnityPlayer.currentActivity), SwrveAdmPushSupport.ON_NOTIFICATION_RECEIVED_METHOD, swrveNotification);
                }

                SwrvePushSDK pushSDK = SwrvePushSDK.createInstance(this);
                if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.O) {
                    pushSDK.setDefaultNotificationChannel((android.app.NotificationChannel)SwrvePushSupport.getDefaultAndroidChannel(prefs));
                }

                // Save influenced data
                if (msg.containsKey(SwrvePushConstants.SWRVE_INFLUENCED_WINDOW_MINS_KEY)) {
                    // Save the date and push id for tracking influenced users
                    String normalId = SwrvePushSDK.getPushId(msg);
                    SwrvePushSDK.saveInfluencedCampaign(context, normalId, msg.getString(SwrvePushConstants.SWRVE_INFLUENCED_WINDOW_MINS_KEY), new Date());
                }

                // Process notification
                processNotification(msg, activityClassName);
            } else {
                // Silent push notification
                if (msg.containsKey(SwrvePushConstants.SWRVE_INFLUENCED_WINDOW_MINS_KEY)) {
                    // Save the date and push id for tracking influenced users
                    SwrvePushSDK.saveInfluencedCampaign(context, silentId, msg.getString(SwrvePushConstants.SWRVE_INFLUENCED_WINDOW_MINS_KEY), new Date());
                }

                // Obtain and pass around the silent push object
                String payloadJson = msg.getString(SwrvePushConstants.SILENT_PAYLOAD_KEY);

                // Trigger the silent push broadcast
                Bundle silentBundle = new Bundle();
                silentBundle.putString(SwrvePushConstants.SILENT_PAYLOAD_KEY, payloadJson);
                Intent silentIntent = new Intent(SwrvePushSupport.SILENT_PUSH_BROADCAST_ACTION);
                silentIntent.putExtras(silentBundle);
                sendBroadcast(silentIntent);
            }
        } catch (Exception ex) {
            Log.e(TAG, "Error processing push notification", ex);
        }
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

        // This must be at least zero;
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

    private void processNotification(final Bundle msg, String activityClassName) {
        try {
            // Put the message into a notification and post it.
            final NotificationManager mNotificationManager = (NotificationManager) this.getSystemService(Context.NOTIFICATION_SERVICE);

            final PendingIntent contentIntent = createPendingIntent(msg, activityClassName);
            if (contentIntent == null) {
                Log.e(TAG, "Error processing ADM push notification. Unable to create intent");
                return;
            }

            final Notification notification = createNotification(msg, contentIntent);
            if (notification == null) {
                Log.e(TAG, "Error processing ADM push notification. Unable to create notification.");
                return;
            }

            //Time to show notification
            showNotification(mNotificationManager, notification);
        } catch (Exception ex) {
            Log.e(TAG, "Error processing ADM push notification:", ex);
        }
    }

    private int showNotification(NotificationManager notificationManager, Notification notification) {
        if (notificationId == null) {
            // Continue to work as pre 4.11
            int localNotificationId = generateTimestampId();
            notificationManager.notify(localNotificationId, notification);
            return localNotificationId;
        } else {
            // Notification Id generated in createNotification
            notificationManager.notify(notificationId, notification);
            return notificationId;
        }
    }

    private int generateTimestampId() {
        return (int)(new Date().getTime() % Integer.MAX_VALUE);
    }

    private Notification createNotification(Bundle msg, PendingIntent contentIntent) {
        String msgText = msg.getString("text");
        if (!SwrveAdmHelper.isNullOrEmpty(msgText)) {
            // Build notification
            notificationId = SwrvePushSupport.createNotificationId(msg);
            NotificationCompat.Builder builder = createNotificationBuilder(msgText, msg);
            builder.setContentIntent(contentIntent);
            return builder.build();
        }
        return null;
    }

    private NotificationCompat.Builder createNotificationBuilder(String msgText, Bundle msg) {
        Context context = getApplicationContext();
        SharedPreferences prefs = SwrveAdmPushSupport.getAdmPreferences(context);
        return SwrvePushSupport.createNotificationBuilder(context, prefs, msgText, msg, notificationId);
    }

    private PendingIntent createPendingIntent(Bundle msg, String activityClassName) {
        // Add notification to bundle
        Intent intent = createIntent(msg, activityClassName);
        if (intent == null) {
            return null;
        }
        return PendingIntent.getActivity(this, generateTimestampId(), intent, PendingIntent.FLAG_UPDATE_CURRENT);
    }

    private Intent createIntent(Bundle msg, String activityClassName) {
        return SwrvePushSupport.createIntent(this, msg, activityClassName);
    }

    protected static void processIntent(Context context, Intent intent) {
        if (intent == null) {
            return;
        }
        try {
            Bundle extras = intent.getExtras();
            if (extras != null && !extras.isEmpty()) {
                Bundle msg = extras.getBundle(SwrvePushSupport.NOTIFICATION_PAYLOAD_KEY);
                if (msg != null) {
                    SwrveNotification notification = SwrveNotification.Builder.build(msg);
                    // Remove influenced data before letting Unity know
                    SwrvePushSDK.removeInfluenceCampaign(context, notification.getId());
                    SwrveAdmPushSupport.newOpenedNotification(SwrveAdmPushSupport.getGameObject(UnityPlayer.currentActivity), SwrveAdmPushSupport.ON_OPENED_FROM_PUSH_NOTIFICATION_METHOD, notification);
                }
            }
        } catch(Exception ex) {
            Log.e(TAG, "Could not process push notification intent", ex);
        }
    }
}
