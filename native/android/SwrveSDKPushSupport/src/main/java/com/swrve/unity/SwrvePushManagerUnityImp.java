package com.swrve.unity;

import android.app.Notification;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;

import androidx.core.app.NotificationCompat;

import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrveLogger;
import com.swrve.sdk.SwrveNotificationBuilder;
import com.swrve.sdk.SwrveNotificationConstants;
import com.swrve.sdk.SwrveNotificationFilter;
import com.swrve.sdk.SwrvePushManager;
import com.swrve.sdk.SwrvePushManagerImpBase;
import com.swrve.sdk.SwrveUnityCommonHelper;
import com.swrve.sdk.SwrveUnitySDK;
import com.unity3d.player.UnityPlayer;

import java.util.Date;

import static com.swrve.sdk.SwrveUnityCommonHelper.getGenericEventCampaignTypePush;
import static com.swrve.unity.SwrvePushServiceManagerCommon.getGameObject;

public class SwrvePushManagerUnityImp extends SwrvePushManagerImpBase implements SwrvePushManager {

    private SwrveNotificationBuilder notificationBuilder;

    public SwrvePushManagerUnityImp(Context context) {
        super(context);
    }

    @Override
    public void processMessage(Bundle msg) {
        process(msg);
    }

    @Override
    public void processSilent(final Bundle msg, final String silentId) {
        Intent intent = SwrvePushSupport.getSilentPushIntent(msg);
        context.sendBroadcast(intent);
    }

    @Override
    public void processNotification(final Bundle msg, String pushId) {

        executeUnityPushNotificationListener(msg);

        saveCampaignInfluence(msg, pushId);

        // Put the message into a notification and post it.
        final NotificationManager mNotificationManager = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
        final Notification notification = createNotification(msg);
        if (notification != null) {
            // Check if is an authenticated push so we save it to remove from NotificationManager later. (when switch user.)
            if (isAuthenticatedNotification(msg)) {
                SwrveUnityCommonHelper.saveNotificationWithId(notificationBuilder.getNotificationId());
            }
            showNotification(mNotificationManager, notification);
        }
    }

    // Try to execute in csharp unity listener configured SwrveConfig.PushNotificationListener.OnNotificationReceived
    private void executeUnityPushNotificationListener(final Bundle msg) {
        // Only call this if there is a unity activity running
        if (UnityPlayer.currentActivity != null) {
            SwrveUnityNotification swrveNotification = SwrveUnityNotification.Builder.build(msg);
            SwrvePushSupport.newReceivedNotification(getGameObject(UnityPlayer.currentActivity), swrveNotification);
        }
    }

    private int showNotification(NotificationManager notificationManager, Notification notification) {
        int notificationId = notificationBuilder.getNotificationId();
        notificationManager.notify(notificationId, notification);
        return notificationId;
    }

    private Notification createNotification(Bundle msg) {
        Notification notification = null;
        String msgText = msg.getString(SwrveNotificationConstants.TEXT_KEY);
        if (SwrveHelper.isNotNullOrEmpty(msgText)) {
            createSwrveNotificationBuilder();
            NotificationCompat.Builder mBuilder = notificationBuilder.build(msgText, msg, getGenericEventCampaignTypePush(), null);
            final PendingIntent contentIntent = notificationBuilder.createPendingIntent(msg, getGenericEventCampaignTypePush(), null);
            mBuilder.setContentIntent(contentIntent);

            int notificationId = notificationBuilder.getNotificationId();
            notification = applyCustomFilter((mBuilder), notificationId, msg, notificationBuilder.getNotificationDetails());
            if (notification == null) {
                SwrveLogger.d("SwrveUnityPushServiceManager: notification suppressed via custom filter. notificationId: %s", notificationId);
            }
        }
        return notification;
    }

    @Override
    public SwrveNotificationFilter getNotificationFilter() {
        return SwrveUnitySDK.getNotificationFilter();
    }

    private SwrveNotificationBuilder createSwrveNotificationBuilder() {
        final SharedPreferences prefs = SwrvePushServiceManagerCommon.getPreferences(context);
        notificationBuilder = SwrvePushSupport.createSwrveNotificationBuilder(context, prefs);
        return notificationBuilder;
    }

}
