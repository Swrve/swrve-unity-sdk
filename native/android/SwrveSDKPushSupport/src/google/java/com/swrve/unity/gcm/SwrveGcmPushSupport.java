package com.swrve.unity.gcm;

import android.app.Activity;
import android.content.Context;
import android.content.SharedPreferences;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager.NameNotFoundException;
import android.os.AsyncTask;
import android.util.Log;

import com.google.android.gms.common.ConnectionResult;
import com.google.android.gms.common.GoogleApiAvailability;
import com.google.android.gms.iid.InstanceID;
import com.google.android.gms.ads.identifier.AdvertisingIdClient;
import com.google.android.gms.ads.identifier.AdvertisingIdClient.Info;
import com.swrve.unity.SwrvePushSupport;
import com.unity3d.player.UnityPlayer;

public class SwrveGcmPushSupport extends SwrvePushSupport {
	private static final String LOG_TAG = "SwrveGcmRegistration";
	private static final int VERSION = 4;

    private static final String PROPERTY_REG_ID = "registration_id";
    private static final String PROPERTY_APP_VERSION = "appVersion";

	// Method names used when sending message from this plugin to Unity class "SwrveSDK/SwrveComponent.cs"
	private static final String ON_DEVICE_REGISTERED_METHOD = "OnDeviceRegistered";
	private static final String ON_NEW_ADVERTISING_ID_METHOD = "OnNewAdvertisingId";
	static final String ON_NOTIFICATION_RECEIVED_METHOD = "OnNotificationReceived";
	static final String ON_OPENED_FROM_PUSH_NOTIFICATION_METHOD = "OnOpenedFromPushNotification";

	private static String lastGameObjectRegistered;
	private static String lastSenderIdUsed;

	// Called by Unity
    public static int getVersion() {
    	return VERSION;
    }

	// Called by Unity
    public static boolean registerDevice(final String gameObject, final String senderId,
										 final String appTitle, final String iconId,
										 final String materialIconId, final String largeIconId,
										 final int accentColor) {
    	if (UnityPlayer.currentActivity != null) {
			lastGameObjectRegistered = gameObject;
			lastSenderIdUsed = senderId;
    		final Activity activity = UnityPlayer.currentActivity;
	    	// This code needs to be run from the UI thread. Do not trust Unity to run
	    	// the JNI invoked code from that thread.
    		activity.runOnUiThread(new Runnable() {
				@Override
				public void run() {
					try {
			    		saveConfig(gameObject, activity, appTitle, iconId, materialIconId, largeIconId, accentColor);
						String registrationId;
						if (isGooglePlayServicesAvailable(activity)) {
							Context context = activity.getApplicationContext();
				            registrationId = getRegistrationId(context);
				            if (isEmptyString(registrationId)) {
				                registerInBackground(gameObject, context, senderId);
				            } else {
				            	notifySDKOfRegistrationId(gameObject, registrationId);
				            }
					    }

						sdkIsReadyToReceivePushNotifications(gameObject, ON_NOTIFICATION_RECEIVED_METHOD, ON_OPENED_FROM_PUSH_NOTIFICATION_METHOD);
			    	} catch (Throwable ex) {
			            Log.e(LOG_TAG, "Couldn't obtain the GCM registration id for the device", ex);
			        }
				}
			});

    		return true;
    	} else {
    		Log.e(LOG_TAG, "UnityPlayer.currentActivity was null");
    	}

    	return false;
	}

	public static void onTokenRefreshed() {
		if (UnityPlayer.currentActivity != null && lastGameObjectRegistered != null && lastSenderIdUsed != null) {
			final Activity activity = UnityPlayer.currentActivity;
			// This code needs to be run from the UI thread. Do not trust Unity to run
			// the JNI invoked code from that thread.
			activity.runOnUiThread(new Runnable() {
				@Override
				public void run() {
					try {
						Context context = activity.getApplicationContext();
						registerInBackground(lastGameObjectRegistered, context, lastSenderIdUsed);
					} catch (Throwable ex) {
						Log.e(LOG_TAG, "Couldn't obtain the registration id for the device", ex);
					}
				}
			});
		} else {
			Log.e(LOG_TAG, "UnityPlayer.currentActivity was null or the plugin was not initialized");
		}
	}

    private static void saveConfig(String gameObject, Activity activity, String appTitle, String iconId,
								   String materialIconId, String largeIconId, int accentColor) {
    	Context context = activity.getApplicationContext();
    	final SharedPreferences prefs = getGCMPreferences(context);

    	SharedPreferences.Editor editor = prefs.edit();
		SwrvePushSupport.saveConfig(editor, gameObject, activity, appTitle, iconId, materialIconId, largeIconId, accentColor);
	    editor.apply();
    }

    /**
	 * Gets the current registration ID for application on GCM service.
	 *
	 * If result is empty, the app needs to register.
	 *
	 * @return registration ID, or empty string if there is no existing
	 *         registration ID.
	 */
	private static String getRegistrationId(Context context) {
		final SharedPreferences prefs = getGCMPreferences(context);
	    String registrationId = prefs.getString(PROPERTY_REG_ID, "");
	    if (isEmptyString(registrationId)) {
	        Log.i(LOG_TAG, "Registration not found.");
	        return "";
	    }
	    // Check if app was updated; if so, it must clear the registration ID
	    // since the existing regID is not guaranteed to work with the new
	    // app version.
	    int registeredVersion = prefs.getInt(PROPERTY_APP_VERSION, Integer.MIN_VALUE);
	    int currentVersion = getAppVersion(context);
	    if (registeredVersion != currentVersion) {
	        Log.i(LOG_TAG, "App version changed.");
	        return "";
	    }
	    return registrationId;
	}

	/**
	 * Check the device to make sure it has the Google Play Services APK. If
	 * it doesn't, display a dialog that allows users to download the APK from
	 * the Google Play Store or enable it in the device's system settings.
	 */
	private static boolean isGooglePlayServicesAvailable(Activity context) {
	    GoogleApiAvailability googleAPI = GoogleApiAvailability.getInstance();
        int resultCode = googleAPI.isGooglePlayServicesAvailable(context);
	    return resultCode == ConnectionResult.SUCCESS;
	}

	/**
	 * Registers the application with GCM servers asynchronously.
	 */
	private static void registerInBackground(final String gameObject, final Context context, final String senderId) {
	    new AsyncTask<Void, Integer, Void>() {
	        @Override
	        protected Void doInBackground(Void... params) {
	        	String gcmRegistrationId = null;

	        	// Try to obtain the GCM registration id from Google Play
	            try {
					InstanceID instanceID = InstanceID.getInstance(context);
					gcmRegistrationId = instanceID.getToken(senderId, null);
	            } catch (Exception ex) {
	                Log.e(LOG_TAG, "Couldn't obtain the registration id for the device", ex);
	            }

  	            if (!isEmptyString(gcmRegistrationId)) {
  	            	try {
			            // Save registration id and app version
						storeRegistrationId(context, gcmRegistrationId);
						// Notify the sdk of the new registration id
						notifySDKOfRegistrationId(gameObject, gcmRegistrationId);
					} catch (Exception ex) {
 						Log.e(LOG_TAG, "Couldn't save the registration id for the device", ex);
 					}
				}

				return null;
	        }

			@Override
	        protected void onPostExecute(Void v) {
	        }
	    }.execute(null, null, null);
	}

	/**
	 * Stores the registration ID and app versionCode in the application's
	 * {@code SharedPreferences}.
	 *
	 * @param context application's context.
	 * @param regId registration ID
	 */
	private static void storeRegistrationId(Context context, String regId) {
	    final SharedPreferences prefs = getGCMPreferences(context);
	    int appVersion = getAppVersion(context);
	    Log.i(LOG_TAG, "Saving regId on app version " + appVersion);
	    SharedPreferences.Editor editor = prefs.edit();
	    editor.putString(PROPERTY_REG_ID, regId);
	    editor.putInt(PROPERTY_APP_VERSION, appVersion);
	    editor.apply();
	}

	/**
	 * @return Application's {@code SharedPreferences}.
	 */
	static SharedPreferences getGCMPreferences(Context context) {
	    return context.getSharedPreferences(context.getPackageName() + "_swrve_push", Context.MODE_PRIVATE);
	}

	/**
	 * @return Application's version code from the {@code PackageManager}.
	 */
	private static int getAppVersion(Context context) {
	    try {
	        PackageInfo packageInfo = context.getPackageManager()
	                .getPackageInfo(context.getPackageName(), 0);
	        return packageInfo.versionCode;
	    } catch (NameNotFoundException e) {
	        // should never happen
	        throw new RuntimeException("Could not get package name: " + e);
	    }
	}

	private static boolean isEmptyString(String str) {
		return (str == null || str.equals(""));
	}

    private static void notifySDKOfRegistrationId(String gameObject, String registrationId) {
    	// Call Unity SDK MonoBehaviour container
    	UnityPlayer.UnitySendMessage(gameObject, ON_DEVICE_REGISTERED_METHOD, registrationId);
	}

	static String getGameObject(Context context) {
		final SharedPreferences prefs = SwrveGcmPushSupport.getGCMPreferences(context);
		return prefs.getString(PROPERTY_GAME_OBJECT_NAME, "SwrveComponent");
	}

	// Called by Unity
	public static boolean requestAdvertisingId(final String gameObject) {
    	if (UnityPlayer.currentActivity != null) {
    		final Activity activity = UnityPlayer.currentActivity;
	    	new AsyncTask<Void, Integer, Void>() {
                @Override
                protected Void doInBackground(Void... params) {
                    try {
                    	if (isGooglePlayServicesAvailable(activity)) {
	                        Info adInfo = AdvertisingIdClient.getAdvertisingIdInfo(activity);
	                        // Notify the SDK of the new advertising ID
	                        notifySDKOfAdvertisingId(gameObject, adInfo.getId());
	                    }
                    } catch (Exception ex) {
                        Log.e(LOG_TAG, "Couldn't obtain Advertising Id", ex);
                    }
                    return null;
                }

                @Override
                protected void onPostExecute(Void v) {
                }
            }.execute(null, null, null);
    		return true;
    	} else {
    		Log.e(LOG_TAG, "UnityPlayer.currentActivity was null");
    	}

    	return false;
	}

	private static void notifySDKOfAdvertisingId(String gameObject, String advertisingId) {
		UnityPlayer.UnitySendMessage(gameObject, ON_NEW_ADVERTISING_ID_METHOD, advertisingId);
	}
}
