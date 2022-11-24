package com.swrve.unity.firebase;

import static com.swrve.sdk.SwrveNotificationConstants.SWRVE_SILENT_TRACKING_KEY;
import static com.swrve.sdk.SwrveNotificationInternalPayloadConstants.SWRVE_TRACKING_KEY;

import android.os.Bundle;

import com.google.firebase.messaging.FirebaseMessagingService;
import com.google.firebase.messaging.RemoteMessage;
import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrveLogger;
import com.swrve.sdk.SwrveUnityCommonHelper;
import com.swrve.unity.SwrvePushManagerUnityImp;

public class SwrveFirebaseMessagingService extends FirebaseMessagingService {

    @Override
    public void onNewToken(String token) {
        super.onNewToken(token);
        SwrveFirebasePushSupport.onNewToken(getApplicationContext(), token);
    }

    @Override
    public void onMessageReceived(RemoteMessage remoteMessage) {
        super.onMessageReceived(remoteMessage);
        try {
            if (remoteMessage.getData() != null) {
                Bundle extras = new Bundle(); // Convert from map to Bundle
                for (String key : remoteMessage.getData().keySet()) {
                    extras.putString(key, remoteMessage.getData().get(key));
                }
                extras.putString("provider.message_id", remoteMessage.getMessageId());
                extras.putString("provider.sent_time", String.valueOf(remoteMessage.getSentTime()));

                if (!SwrveHelper.isSwrvePush(extras)) {
                    SwrveLogger.i("SwrveSDK Received Push: but not processing as it doesn't contain: %s or %s", SWRVE_TRACKING_KEY, SWRVE_SILENT_TRACKING_KEY);
                    return;
                } else if (SwrveUnityCommonHelper.isPushSidDupe(this, remoteMessage.getData())) {
                    SwrveLogger.i("SwrveSDK Received Push: but not processing as _sid has been processed before.");
                    return;
                }

                getSwrveUnityPushServiceManager().processMessage(extras);
            }
        } catch (Exception e) {
            SwrveLogger.e("SwrveFirebaseMessagingService.onMessageReceived Exception", e);
        }
    }

    public SwrvePushManagerUnityImp getSwrveUnityPushServiceManager() {
        return new SwrvePushManagerUnityImp(this);
    }
}
