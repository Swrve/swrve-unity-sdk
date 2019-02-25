package com.swrve.unity.firebase;
import android.os.Bundle;

import com.google.firebase.messaging.FirebaseMessagingService;
import com.google.firebase.messaging.RemoteMessage;
import com.swrve.unity.SwrveUnityPushServiceManager;

public class SwrveFirebaseMessagingService extends FirebaseMessagingService {

	@Override
	public void onNewToken(String token) {
		super.onNewToken(token);
		SwrveFirebasePushSupport.onNewToken(getApplicationContext(), token);
	}

	@Override
	public void onMessageReceived(RemoteMessage remoteMessage) {
		super.onMessageReceived(remoteMessage);
		if (remoteMessage.getData() != null) {
			// Convert from map to Bundle
			Bundle pushBundle = new Bundle();
			for (String key : remoteMessage.getData().keySet()) {
				pushBundle.putString(key, remoteMessage.getData().get(key));
			}
			new SwrveUnityPushServiceManager(this).processRemoteNotification(pushBundle);
		}
	}
}
