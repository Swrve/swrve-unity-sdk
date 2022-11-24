package com.swrve.sdk;

import android.app.NotificationChannel;
import android.content.Context;

import java.util.Map;

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

    public static boolean isPushSidDupe(Context context, Map<String, String> data) {
        return SwrvePushSidDeDuper.isDupe(context, data);
    }
}
