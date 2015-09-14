package com.swrve.unity.gcm;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

import android.app.Activity;
import android.content.Context;
import android.content.SharedPreferences;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager.NameNotFoundException;
import android.os.AsyncTask;
import android.util.Log;

import com.google.android.gms.common.ConnectionResult;
import com.google.android.gms.common.GooglePlayServicesUtil;
import com.google.android.gms.gcm.GoogleCloudMessaging;
import com.google.android.gms.iid.InstanceID;
import com.unity3d.player.UnityPlayer;

public class SwrveGcmDeviceRegistration {
	private static final String LOG_TAG = "SwrveGcmRegistration";
	private static final int VERSION = 3;

    public static final String PROPERTY_REG_ID = "registration_id";
    public static final String PROPERTY_APP_VERSION = "appVersion";
    public static final String PROPERTY_ACTIVITY_NAME = "activity_name";
    public static final String PROPERTY_GAME_OBJECT_NAME = "game_object_name";
    public static final String PROPERTY_APP_TITLE = "app_title";

	public static String lastGameObjectRegistered;
	public static String lastSenderIdUsed;
    public static List<SwrveNotification> receivedNotifications = new ArrayList<SwrveNotification>();
    public static List<SwrveNotification> openedNotifications = new ArrayList<SwrveNotification>();
    
    public static int getVersion() {
    	return VERSION;
    }
    
    public static boolean registerDevice(final String gameObject, final String senderId, final String appTitle) {
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
			    		saveConfig(gameObject, activity, appTitle);
						String registrationId;
						if (checkPlayServices(activity)) {
							Context context = activity.getApplicationContext();
				            registrationId = getRegistrationId(context);
				            if (isEmptyString(registrationId)) {
				                registerInBackground(gameObject, context, senderId);
				            } else {
				            	notifySDKOfRegistrationId(gameObject, registrationId);
				            }
					    }
						
						sdkIsReadyToReceivePushNotifications(activity);
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
		if (UnityPlayer.currentActivity != null  && lastGameObjectRegistered != null && lastSenderIdUsed != null) {
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
    
    private static void saveConfig(String gameObject, Activity activity, String appTitle) {
    	Context context = activity.getApplicationContext();
    	final SharedPreferences prefs = getGCMPreferences(context);
    	
    	SharedPreferences.Editor editor = prefs.edit();
	    editor.putString(PROPERTY_ACTIVITY_NAME, activity.getLocalClassName());
	    editor.putString(PROPERTY_GAME_OBJECT_NAME, gameObject);
	    editor.putString(PROPERTY_APP_TITLE, appTitle);
	    editor.commit();
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
	private static boolean checkPlayServices(Activity context) {
	    int resultCode = GooglePlayServicesUtil.isGooglePlayServicesAvailable(context);
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
	    editor.commit();
	}
	
	/**
	 * @return Application's {@code SharedPreferences}.
	 */
	public static SharedPreferences getGCMPreferences(Context context) {
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
    	UnityPlayer.UnitySendMessage(gameObject, "OnDeviceRegistered", registrationId);
	}
    
	private static String getGameObject(Context context) {
		final SharedPreferences prefs = SwrveGcmDeviceRegistration.getGCMPreferences(context);
		return prefs.getString(SwrveGcmDeviceRegistration.PROPERTY_GAME_OBJECT_NAME, "SwrveComponent");
	}

	public static void sdkIsReadyToReceivePushNotifications(final Context context) {
		synchronized(receivedNotifications) {
			// Send pending received notifications to SDK instance
			for(SwrveNotification notification : receivedNotifications) {
				notifySDKOfReceivedNotification(context, notification);
			}
			// Remove right away as SDK is initialized
			receivedNotifications.clear();
		}


		synchronized(openedNotifications) {
			// Send pending opened notifications to SDK instance
			for(SwrveNotification notification : openedNotifications) {
				notifySDKOfOpenedNotification(context, notification);
			}
			// Remove right away as SDK is initialized
			openedNotifications.clear();
		}
	}
	
	public static void newReceivedNotification(Context context, SwrveNotification notification) {
		if (notification != null) {
			synchronized(receivedNotifications) {
				receivedNotifications.add(notification);
				notifySDKOfReceivedNotification(context, notification);
			}
		}
	}
	
	public static void newOpenedNotification(Context context, SwrveNotification notification) {
		if (notification != null) {
			synchronized(openedNotifications) {
				openedNotifications.add(notification);
				notifySDKOfOpenedNotification(context, notification);
			}
		}
	}

	private static void notifySDKOfReceivedNotification(Context context, SwrveNotification notification) {
		String gameObject = getGameObject(context);
		String serializedNotification = notification.toJson();
		if (serializedNotification != null) {
			UnityPlayer.UnitySendMessage(gameObject, "OnNotificationReceived", serializedNotification.toString());
		}
	}
	
	private static void notifySDKOfOpenedNotification(Context context, SwrveNotification notification) {
		String gameObject = getGameObject(context);
		String serializedNotification = notification.toJson();
		if (serializedNotification != null) {
			UnityPlayer.UnitySendMessage(gameObject, "OnOpenedFromPushNotification", serializedNotification.toString());
		}
	}
	
	public static void sdkAcknowledgeReceivedNotification(String id) {
		removeFromCollection(id, receivedNotifications);
	}
	
	public static void sdkAcknowledgeOpenedNotification(String id) {
		removeFromCollection(id, receivedNotifications);
	}
	
	private static void removeFromCollection(String id, List<SwrveNotification> collection) {
		synchronized(collection) {
			// Remove acknowledge notification
			Iterator<SwrveNotification> it = collection.iterator();
			while(it.hasNext()) {
				SwrveNotification notification = it.next();
				if (notification.getId().equals(id)) {
					it.remove();
				}
			}
		}
	}
}
