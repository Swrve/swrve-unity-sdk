package com.swrve.sdk;

import android.app.NotificationChannel;
import android.os.Bundle;

public class SwrveUnityCommonHelper {

    public static String getGenericEventCampaignTypePush() {
        return ISwrveCommon.GENERIC_EVENT_CAMPAIGN_TYPE_PUSH;
    }

    public static NotificationChannel getDefaultNotificationChannel() {
        NotificationChannel channel = null;
        SwrveCommon.checkInstanceCreated();
        ISwrveCommon swrveCommon = SwrveCommon.getInstance();
        if (swrveCommon != null) {
            channel = swrveCommon.getDefaultNotificationChannel();
        }
        return channel;
    }

    public static boolean isAuthenticatedNotification(Bundle msg) {
        String targetUser = msg.getString(SwrveNotificationConstants.SWRVE_AUTH_USER_KEY);
        return SwrveHelper.isNotNullOrEmpty(targetUser);
    }

    public static boolean isTargetUser(Bundle msg) {
        SwrveCommon.checkInstanceCreated();
        ISwrveCommon swrveCommon = SwrveCommon.getInstance();
        String authenticatedUserId = msg.getString(SwrveNotificationConstants.SWRVE_AUTH_USER_KEY);
        if (authenticatedUserId != null) {
            if (swrveCommon != null && !swrveCommon.getUserId().equals(authenticatedUserId)) {
                return false;
            }
        }
        return true;
    }

    public static void saveNotificationWithId(int notificationId) {
        SwrveCommon.checkInstanceCreated();
        ISwrveCommon swrveCommon = SwrveCommon.getInstance();
        swrveCommon.saveNotificationAuthenticated(notificationId);
    }

    public static void checkInstance() {
        SwrveCommon.checkInstanceCreated();
    }
}
