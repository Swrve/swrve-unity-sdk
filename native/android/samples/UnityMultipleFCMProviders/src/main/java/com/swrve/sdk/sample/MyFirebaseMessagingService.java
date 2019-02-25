package com.swrve.sdk.sample;

import com.google.firebase.messaging.FirebaseMessagingService;
import com.google.firebase.messaging.RemoteMessage;

/**
 * Class that receives the FCM messages
 */
public class MyFirebaseMessagingService extends FirebaseMessagingService {
    @Override
    public void onMessageReceived(RemoteMessage remoteMessage) {
        super.onMessageReceived(remoteMessage);
        // Use the Unity version under the com.swrve.unity package
        if (!com.swrve.unity.SwrvePushServiceDefault.handle(this, remoteMessage.getData())) {
            // execute code for other push provider
        }
    }
}