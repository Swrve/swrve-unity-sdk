package com.swrve.sdk;

import android.app.NotificationChannel;

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

    public static void saveNotificationWithId(int notificationId) {
        SwrveCommon.checkInstanceCreated();
        ISwrveCommon swrveCommon = SwrveCommon.getInstance();
        swrveCommon.saveNotificationAuthenticated(notificationId);
    }
}
