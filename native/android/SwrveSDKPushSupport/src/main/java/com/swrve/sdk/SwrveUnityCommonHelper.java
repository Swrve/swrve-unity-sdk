package com.swrve.sdk;

import android.app.NotificationChannel;
import android.content.Context;
import android.os.Bundle;

import java.util.ArrayList;

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

    public static String getPushDeliveredBatchEvent(ArrayList<String> eventsList) throws Exception {
        return EventHelper.getPushDeliveredBatchEvent(eventsList);
    }

    public static ArrayList<String> getPushDeliveredEvent(Bundle extras, long time) throws Exception {
        return EventHelper.getPushDeliveredEvent(extras, time, true, "");
    }

    public void sendPushDeliveredEvent(Context context, Bundle extras) {
        try {
            SwrveCommon.checkInstanceCreated(); // We need be sure that we have a valid "SwrveCommon" instance to be able to send Push Delivery.
            ArrayList<String> eventsList = SwrveUnityCommonHelper.getPushDeliveredEvent(extras, getTime());
            if (eventsList != null && eventsList.size() > 0) {
                String eventBody = SwrveUnityCommonHelper.getPushDeliveredBatchEvent(eventsList);
                String endPoint = SwrveCommon.getInstance().getEventsServer() + "/1/batch";
                getCampaignDeliveryManager(context).sendCampaignDelivery(endPoint, eventBody);
            }
        } catch (Exception e) {
            SwrveLogger.e("Exception in sendPushDeliveredEvent", e);
        }
    }

    protected CampaignDeliveryManager getCampaignDeliveryManager(Context context) {
        return new CampaignDeliveryManager(context);
    }

    protected long getTime() {
        return System.currentTimeMillis();
    }
}
