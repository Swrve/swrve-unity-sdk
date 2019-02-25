package com.swrve.unity;

import android.app.Notification;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.support.v4.app.NotificationCompat;

import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrveLogger;
import com.swrve.sdk.SwrveNotificationBuilder;
import com.swrve.sdk.SwrveNotificationConstants;
import com.swrve.sdk.SwrveUnitySDK;
import com.swrve.sdk.SwrveUnityCommonHelper;
import com.unity3d.player.UnityPlayer;

import java.util.Date;

import static com.swrve.sdk.SwrveNotificationConstants.SWRVE_NESTED_JSON_PAYLOAD_KEY;

public class SwrveUnityPushServiceManager {
    private final Context context;
    private SwrveNotificationBuilder notificationBuilder;

    public SwrveUnityPushServiceManager(Context context) {
        this.context = context;
    }

    public void processRemoteNotification(Bundle msg) {
        if (!SwrveHelper.isSwrvePush(msg)) {
            SwrveLogger.i( "Swrve received a push notification: but not processing as it doesn't contain:" + SwrveNotificationConstants.SWRVE_TRACKING_KEY);
            return;
        }
        if (!SwrveUnityCommonHelper.isTargetUser(msg)) {
            SwrveLogger.w("Swrve cannot process push because its intended for different user.");
            return;
        }

        try {
            if (SwrveHelper.isSwrvePush(msg)) {
                final SharedPreferences prefs = SwrvePushServiceManagerCommon.getPreferences(context);
                String activityClassName = SwrvePushSupport.getActivityClassName(context, prefs);

                String silentId = SwrveHelper.getSilentPushId(msg);

                if (SwrveHelper.isNullOrEmpty(silentId)) {
                    // Only call this listener if there is an activity running
                    if (UnityPlayer.currentActivity != null) {
                        // Call Unity SDK MonoBehaviour container
                        SwrveUnityNotification swrveNotification = SwrveUnityNotification.Builder.build(msg);
                        SwrvePushSupport.newReceivedNotification(SwrvePushServiceManagerCommon.getGameObject(UnityPlayer.currentActivity), swrveNotification);
                    }
                    SwrvePushSupport.saveCampaignInfluence(msg, context, SwrveHelper.getRemotePushId(msg));
                    // Process notification
                    processNotification(msg, activityClassName);
                } else {
                    // Silent push notification
                    SwrvePushSupport.saveCampaignInfluence(msg, context, silentId);
                    Intent intent = SwrvePushSupport.getSilentPushIntent(msg);
                    context.sendBroadcast(intent);
                }
            }
        } catch (Exception ex) {
            SwrveLogger.e("Error processing push notification", ex);
        }
    }

    private void processNotification(final Bundle msg, String activityClassName) {
        // Put the message into a notification and post it.
        final NotificationManager mNotificationManager = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
        final PendingIntent contentIntent = createPendingIntent(msg, activityClassName);
        final Notification notification = createNotification(msg, contentIntent);
        if (notification != null) {
            //Check if is an authenticated push so we save it to remove from Notification Manager later. (when switch user.)
            if (SwrveUnityCommonHelper.isAuthenticatedNotification(msg)) {
                SwrveUnityCommonHelper.saveNotificationWithId(notificationBuilder.getNotificationId());
            }
            showNotification(mNotificationManager, notification);
        }
    }

    private int showNotification(NotificationManager notificationManager, Notification notification) {
        int notificationId = notificationBuilder.getNotificationId();
        notificationManager.notify(notificationId, notification);
        return notificationId;
    }

    private Notification createNotification(Bundle msg, PendingIntent contentIntent) {
        String msgText = msg.getString(SwrveNotificationConstants.TEXT_KEY);
        if (SwrveHelper.isNotNullOrEmpty(msgText)) {
            createSwrveNotificationBuilder();
            NotificationCompat.Builder mBuilder = notificationBuilder.build(msgText, msg, SwrveUnityCommonHelper.getGenericEventCampaignTypePush(), null);
            mBuilder.setContentIntent(contentIntent);

            if (SwrveUnitySDK.getNotificationCustomFilter() != null) {
                String jsonPayload = msg.getString(SWRVE_NESTED_JSON_PAYLOAD_KEY);
                return SwrveUnitySDK.getNotificationCustomFilter().filterNotification(mBuilder, notificationBuilder.getNotificationId(), jsonPayload);
            } else {
                return mBuilder.build();
            }
        }

        return null;
    }

    private PendingIntent createPendingIntent(Bundle msg, String activityClassName) {
        // Add notification to bundle
        Intent intent = SwrvePushSupport.createIntent(context, msg, activityClassName);
        int id = (int)(new Date().getTime() % Integer.MAX_VALUE);
        return PendingIntent.getActivity(context, id, intent, PendingIntent.FLAG_UPDATE_CURRENT);
    }

    private SwrveNotificationBuilder createSwrveNotificationBuilder() {
        final SharedPreferences prefs = SwrvePushServiceManagerCommon.getPreferences(context);
        notificationBuilder = SwrvePushSupport.createSwrveNotificationBuilder(context, prefs);
        return notificationBuilder;
    }
}
