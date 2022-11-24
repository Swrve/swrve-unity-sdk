package com.swrve.sdk.sample;

import com.google.firebase.messaging.FirebaseMessagingService;
import com.google.firebase.messaging.RemoteMessage;

/**
 * Class that receives the FCM messages
 */
public class MyFirebaseMessagingService extends FirebaseMessagingService {
    @Override
    public void onNewToken(String token) {
        super.onNewToken(token);
        com.swrve.unity.firebase.SwrveFirebasePushServiceDefault.onNewToken(getApplicationContext(), token);
    }

    @Override
    public void onMessageReceived(RemoteMessage remoteMessage) {
        super.onMessageReceived(remoteMessage);
        // If the push is not a Swrve push and has to be processed by our other provider...
        if (!com.swrve.unity.SwrvePushServiceDefault.handle(this, remoteMessage.getData(), remoteMessage.getMessageId(), remoteMessage.getSentTime())) {
            // Execute code for other push provider
        }
    }
}
