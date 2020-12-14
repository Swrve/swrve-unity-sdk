package com.swrve.unity.firebase;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.AsyncTask;
import android.os.Bundle;
import androidx.annotation.NonNull;

import com.google.android.gms.ads.identifier.AdvertisingIdClient;
import com.google.android.gms.ads.identifier.AdvertisingIdClient.Info;
import com.google.android.gms.tasks.OnFailureListener;
import com.google.android.gms.tasks.OnSuccessListener;
import com.google.android.gms.tasks.Task;
import com.google.firebase.FirebaseApp;
import com.google.firebase.iid.FirebaseInstanceId;
import com.google.firebase.iid.InstanceIdResult;
import com.google.firebase.messaging.FirebaseMessaging;
import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrveLogger;
import com.swrve.sdk.SwrveUnityCommon;
import com.swrve.unity.SwrveUnityNotification;
import com.swrve.unity.SwrvePushServiceManagerCommon;
import com.swrve.unity.SwrvePushSupport;
import com.unity3d.player.UnityPlayer;

import java.util.List;

public class SwrveFirebasePushSupport extends SwrvePushSupport {
	private static final int VERSION = 4;
	private static final String PROPERTY_REG_ID = "registration_id";
	// Method names used when sending message from this plugin to Unity class "SwrveSDK/SwrveComponent.cs"
	private static final String ON_DEVICE_REGISTERED_METHOD = "OnDeviceRegistered";
	private static final String ON_NEW_ADVERTISING_ID_METHOD = "OnNewAdvertisingId";

	private static String lastGameObjectRegistered;

	// Called by Unity
	public static int getVersion() {
		return VERSION;
	}

	// Called by Unity
	public static boolean registerDevice(final String gameObject,
										 final String iconId, final String materialIconId,
										 final String largeIconId, final String accentColorHex,
										 final String currentRegIdInUnity) {
		if (UnityPlayer.currentActivity != null) {
			lastGameObjectRegistered = gameObject;
			final Activity activity = UnityPlayer.currentActivity;
			// This code needs to be run from the UI thread. Do not trust Unity to run
			// the JNI invoked code from that thread.
			activity.runOnUiThread(new Runnable() {
				@Override
				public void run() {
					try {
						Context context = activity.getApplicationContext();
						String registrationId = getRegistrationId(context);

						saveConfig(gameObject, activity, iconId, materialIconId, largeIconId, accentColorHex);
						// Start the Firebase SDK if not already started
						List<FirebaseApp> apps = FirebaseApp.getApps(activity);
						if (apps == null || apps.isEmpty()) {
							FirebaseApp.initializeApp(activity);
						}

						if (isEmptyString(registrationId) || isEmptyString(currentRegIdInUnity)) {
							registerInBackground(gameObject, context);
						}

					} catch (Throwable ex) {
						SwrveLogger.e("Couldn't obtain the SwrveFirebase registration id for the device", ex);
					}
				}
			});

			return true;
		} else {
			SwrveLogger.e("SwrveFirebase: UnityPlayer.currentActivity was null");
		}

		return false;
	}

	private static void saveConfig(String gameObject, Activity activity, String iconId,
								   String materialIconId, String largeIconId, String accentColorHex) {
		Context context = activity.getApplicationContext();
		final SharedPreferences prefs = SwrvePushServiceManagerCommon.getPreferences(context);

		SharedPreferences.Editor editor = prefs.edit();
		SwrvePushSupport.saveConfig(editor, gameObject, activity, iconId, materialIconId, largeIconId, accentColorHex);
		editor.apply();
	}

	/**
	 * Registers the application with Firebase servers asynchronously.
	 */
	private static void registerInBackground(final String gameObject, final Context context) {
		FirebaseMessaging firebaseMessaging = getFirebaseMessagingInstance();
		if (firebaseMessaging != null) {
			// Try to obtain the Firebase registration id
			Task<String> task = firebaseMessaging.getToken();
			task.addOnSuccessListener(new OnSuccessListener<String>() {
				@Override
				public void onSuccess(String  newRegistrationId) {
					try {
						if (!SwrveHelper.isNullOrEmpty(newRegistrationId)) {
							// Save registration id and app version
							storeRegistrationId(context, newRegistrationId);
							// Notify the sdk of the new registration id
							notifySDKOfRegistrationId(gameObject, newRegistrationId);
						}
					} catch (Exception ex) {
						SwrveLogger.e("Couldn't obtain the Firebase registration id for the device", ex);
					}
				}
			}).addOnFailureListener(new OnFailureListener() {
				@Override
				public void onFailure(@NonNull Exception e) {
					SwrveLogger.e("Couldn't obtain the Firebase registration id for the device", e);
				}
			});
		}
	}

	private static void storeRegistrationId(Context context, String regId) {
		final SharedPreferences prefs = SwrvePushServiceManagerCommon.getPreferences(context);
		SharedPreferences.Editor editor = prefs.edit();
		editor.putString(PROPERTY_REG_ID, regId);
		editor.apply();
	}

	/**
	 * Get notified of the registration ID and store it in the application's {@code SharedPreferences}.
	 *
	 * @param context application's context.
	 * @param regId   registration ID
	 */
	static void onNewToken(Context context, String regId) {
		storeRegistrationId(context, regId);
		if (lastGameObjectRegistered != null) {
			// Notify the sdk of the new registration id
			notifySDKOfRegistrationId(lastGameObjectRegistered, regId);
		}
	}

	/**
	 * Gets the current registration ID.
	 * <p>
	 * If result is empty, the app needs to register.
	 *
	 * @return registration ID, or empty string if there is no existing
	 * registration ID.
	 */
	private static String getRegistrationId(Context context) {
		final SharedPreferences prefs = SwrvePushServiceManagerCommon.getPreferences(context);
		String registrationId = prefs.getString(PROPERTY_REG_ID, "");
		if (isEmptyString(registrationId)) {
			SwrveLogger.i("SwrveFirebase: Registration not found.");
			return "";
		}
		return registrationId;
	}

	private static boolean isEmptyString(String str) {
		return (str == null || str.equals(""));
	}

	private static void notifySDKOfRegistrationId(String gameObject, String registrationId) {
		// Call Unity SDK MonoBehaviour container
        SwrveUnityCommon.UnitySendMessage(gameObject, ON_DEVICE_REGISTERED_METHOD, registrationId);
	}

	// Called by Unity
	public static boolean requestAdvertisingId(final String gameObject) {
		if (UnityPlayer.currentActivity != null) {
			final Activity activity = UnityPlayer.currentActivity;
			new AsyncTask<Void, Integer, Void>() {
				@Override
				protected Void doInBackground(Void... params) {
					try {
						Info adInfo = AdvertisingIdClient.getAdvertisingIdInfo(activity);
						// Notify the SDK of the new advertising ID
						notifySDKOfAdvertisingId(gameObject, adInfo.getId());
					} catch (Exception ex) {
						SwrveLogger.e("Couldn't obtain the Firebase Advertising Id", ex);
					}
					return null;
				}

				@Override
				protected void onPostExecute(Void v) {
				}
			}.execute(null, null, null);
			return true;
		} else {
			SwrveLogger.e("UnityPlayer.currentActivity was null");
		}

		return false;
	}

	private static FirebaseMessaging getFirebaseMessagingInstance() {
		FirebaseMessaging firebaseInstanceId = null;
		try {
			firebaseInstanceId = FirebaseMessaging.getInstance();
		} catch (IllegalStateException e) {
			SwrveLogger.e("Swrve cannot get instance of FirebaseMessaging and therefore cannot get token registration id.", e);
		}
		return firebaseInstanceId;
	}

	private static void notifySDKOfAdvertisingId(String gameObject, String advertisingId) {
        SwrveUnityCommon.UnitySendMessage(gameObject, ON_NEW_ADVERTISING_ID_METHOD, advertisingId);
	}


	static void processIntent(Context context, Intent intent) {
		if (intent != null) {
			try {
				Bundle extras = intent.getExtras();
				if (extras != null && !extras.isEmpty()) {
					Bundle msg = extras.getBundle(SwrvePushSupport.NOTIFICATION_PAYLOAD_KEY);
					if (msg != null) {
						SwrveUnityNotification notification = SwrveUnityNotification.Builder.build(msg);
						// Remove influenced data before letting Unity know
						SwrvePushSupport.removeInfluenceCampaign(context, notification.getId());
						SwrvePushSupport.newOpenedNotification(SwrvePushServiceManagerCommon.getGameObject(UnityPlayer.currentActivity), notification);
					}
				}
			} catch(Exception ex) {
				SwrveLogger.e("Could not process push notification intent", ex);
			}
		}
	}
}
