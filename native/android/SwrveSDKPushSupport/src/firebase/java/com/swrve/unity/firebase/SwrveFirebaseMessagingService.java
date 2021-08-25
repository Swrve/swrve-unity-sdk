package com.swrve.unity.firebase;

import android.os.Bundle;

import com.google.firebase.messaging.FirebaseMessagingService;
import com.google.firebase.messaging.RemoteMessage;
import com.swrve.sdk.SwrveLogger;
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
				Bundle pushBundle = new Bundle(); // Convert from map to Bundle
				for (String key : remoteMessage.getData().keySet()) {
					pushBundle.putString(key, remoteMessage.getData().get(key));
				}
				getSwrveUnityPushServiceManager().processMessage(pushBundle);
			}
		} catch (Exception e) {
			SwrveLogger.e("SwrveFirebaseMessagingService.onMessageReceived Exception", e);
		}
	}

	public SwrvePushManagerUnityImp getSwrveUnityPushServiceManager() {
		return new SwrvePushManagerUnityImp(this);
	}
}
