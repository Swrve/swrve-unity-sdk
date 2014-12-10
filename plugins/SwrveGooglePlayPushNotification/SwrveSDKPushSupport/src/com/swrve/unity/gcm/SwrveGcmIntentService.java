package com.swrve.unity.gcm;

import android.app.IntentService;
import android.app.Notification;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.ContentResolver;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.res.Resources;
import android.media.RingtoneManager;
import android.net.Uri;
import android.os.Bundle;
import android.support.v4.app.NotificationCompat;
import android.util.Log;
import android.preference.PreferenceManager;

import com.google.android.gms.gcm.GoogleCloudMessaging;
import com.unity3d.player.UnityPlayer;

public class SwrveGcmIntentService extends IntentService {
	private static final String LOG_TAG = "SwrveGcmIntentService";

	public SwrveGcmIntentService() {
		super("SwrveGcmIntentService");
	}

	@Override
	protected void onHandleIntent(Intent intent) {
		Bundle extras = intent.getExtras();
		GoogleCloudMessaging gcm = GoogleCloudMessaging.getInstance(this);
		// The getMessageType() intent parameter must be the intent you received
		// in your BroadcastReceiver.
		String messageType = gcm.getMessageType(intent);

		if (!extras.isEmpty()) { // has effect of unparcelling Bundle
			/*
			 * Filter messages based on message type. Since it is likely that
			 * GCM will be extended in the future with new message types, just
			 * ignore any message types you're not interested in, or that you
			 * don't recognize.
			 */
			if (GoogleCloudMessaging.MESSAGE_TYPE_SEND_ERROR.equals(messageType)) {
				Log.e(LOG_TAG, "Send error: " + extras.toString());
			} else if (GoogleCloudMessaging.MESSAGE_TYPE_DELETED.equals(messageType)) {
				Log.e(LOG_TAG, "Deleted messages on server: " + extras.toString());
				// If it's a regular GCM message, do some work.
			} else if (GoogleCloudMessaging.MESSAGE_TYPE_MESSAGE.equals(messageType)) {
				// Process notification.
				processRemoteNotification(extras);
				Log.i(LOG_TAG, "Received notification: " + extras.toString());
			}
		}
		// Release the wake lock provided by the WakefulBroadcastReceiver.
		SwrveGcmBroadcastReceiver.completeWakefulIntent(intent);
	}

	private void processRemoteNotification(Bundle msg) {
		try {
			final SharedPreferences prefs = SwrveGcmDeviceRegistration.getGCMPreferences(getApplicationContext());
			String activityClassName = prefs.getString(SwrveGcmDeviceRegistration.PROPERTY_ACTIVITY_NAME, "com.unity3d.player.UnityPlayerNativeActivity");
			
			// Process activity name (could be local or a class with a package name)
			if(!activityClassName.contains(".")) {
				activityClassName = getPackageName() + "." + activityClassName;
			}
			
			// Only call this listener if there is an activity running
			if (UnityPlayer.currentActivity != null) {
				// Call Unity SDK MonoBehaviour container
				SwrveNotification swrveNotification = SwrveNotification.Builder.build(msg);
				SwrveGcmDeviceRegistration.newReceivedNotification(UnityPlayer.currentActivity, swrveNotification);
	    	}
	
			// Process notification
			processNotification(msg, activityClassName);
		} catch (Exception ex) {
			Log.e(LOG_TAG, "Error processing push notification", ex);
		}
	}

	/**
	 * Override this function to process notifications in a different way.
	 * 
	 * @param msg
	 * @param activityClassName
	 * 			game activity
	 */
	public void processNotification(final Bundle msg, String activityClassName) {
		// Put the message into a notification and post it.
		final NotificationManager mNotificationManager = (NotificationManager) this.getSystemService(Context.NOTIFICATION_SERVICE);
		final PendingIntent contentIntent = createPendingIntent(msg, activityClassName);
		final Notification notification = createNotification(msg, contentIntent);
		if (notification != null) {
			showNotification(mNotificationManager, notification);
		}
	}

	/**
	 * Override this function to change the way a notification is shown.
	 * 
	 * @param notificationManager
	 * @param notification
	 * @return the notification id so that it can be dismissed by other UI
	 *         elements
	 */
	public int showNotification(NotificationManager notificationManager, Notification notification) {
		int notificationId = generateNotificationId(notification);
		notificationManager.notify(notificationId, notification);
		return notificationId;
	}

	/**
	 * Generate the id for the new notification.
	 *
	 * Defaults to the current milliseconds to have unique notifications.
	 * 
	 * @param notification notification data
	 * @return id for the notification to be displayed
	 */
	public int generateNotificationId(Notification notification) {
		return (int)(new Date().getTime() % Integer.MAX_VALUE);
	}

	/**
	 * Override this function to change the attributes of a notification.
	 * 
	 * @param msgText
	 * @param msg
	 * @return
	 */
	public NotificationCompat.Builder createNotificationBuilder(String msgText, Bundle msg) {
		final SharedPreferences prefs = SwrveGcmDeviceRegistration.getGCMPreferences(getApplicationContext());
		String appTitle = prefs.getString(SwrveGcmDeviceRegistration.PROPERTY_APP_TITLE, "Configure your app title");
		
		String msgSound = msg.getString("sound");
		Resources res = getResources();
		int iconDrawableId = res.getIdentifier("app_icon", "drawable", getPackageName());
		// Build notification
		NotificationCompat.Builder mBuilder = new NotificationCompat.Builder(this)
				.setSmallIcon(iconDrawableId)
				.setContentTitle(appTitle)
				.setStyle(new NotificationCompat.BigTextStyle().bigText(msgText))
				.setContentText(msgText)
				.setAutoCancel(true);

		if (!isEmptyString(msgSound)) {
			Uri soundUri;
			if (msgSound.equalsIgnoreCase("default")) {
				soundUri = RingtoneManager.getDefaultUri(RingtoneManager.TYPE_NOTIFICATION);
			} else {
				String packageName = getApplicationContext().getPackageName();
				soundUri = Uri.parse(ContentResolver.SCHEME_ANDROID_RESOURCE + "://" + packageName + "/raw/" + msgSound);
			}
			mBuilder.setSound(soundUri);
		}
		return mBuilder;
	}

	private static boolean isEmptyString(String str) {
		return (str == null || str.equals(""));
	}

	/**
	 * Override this function to change the way the notifications are created.
	 * 
	 * @param msg
	 * @param contentIntent
	 * @return
	 */
	public Notification createNotification(Bundle msg, PendingIntent contentIntent) {
		String msgText = msg.getString("text");

		if (!isEmptyString(msgText)) { // Build notification
			NotificationCompat.Builder mBuilder = createNotificationBuilder(msgText, msg);
			mBuilder.setContentIntent(contentIntent);
			return mBuilder.build();
		}

		return null;
	}

	/**
	 * Override this function to change what the notification will do once
	 * clicked by the user.
	 * 
	 * Note: sending the Bundle in an extra parameter "notification" is
	 * essential so that the Swrve SDK can be notified that the app was opened
	 * from the notification.
	 * 
	 * @param msg
	 * @param activityClassName
	 * 			game activity
	 * @return
	 */
	public PendingIntent createPendingIntent(Bundle msg, String activityClassName) {
		// Add notification to bundle
		Intent intent = createIntent(msg, activityClassName);
		return PendingIntent.getActivity(this, generatePendingIntentId(msg), intent, PendingIntent.FLAG_UPDATE_CURRENT);
	}

	/**
	 * Generate the id for the pending intent associated with
	 * the given push payload.
	 *
	 * Defaults to the current milliseconds to have unique notifications.
	 * 
	 * @param msg push message payload
	 * @return id for the notification to be displayed
	 */
	public int generatePendingIntentId(Bundle msg) {
		return (int)(new Date().getTime() % Integer.MAX_VALUE);
	}

	/**
	 * Override this function to change what the notification will do once
	 * clicked by the user.
	 * 
	 * Note: sending the Bundle in an extra parameter "notification" is
	 * essential so that the Swrve SDK can be notified that the app was opened
	 * from the notification.
	 * 
	 * @param msg
	 * @param activityClassName
	 * 			game activity
	 * @return
	 */
	public Intent createIntent(Bundle msg, String activityClassName) {
		try {
			Intent intent = new Intent(this, Class.forName(activityClassName));
			intent.putExtra("notification", msg);
			intent.setAction("openActivity");
			return intent;
		} catch (ClassNotFoundException e) {
			e.printStackTrace();
		}
		return null;
	}
	
	/**
	 * Process the push notification received from GCM
	 * that opened the app.
	 * 
	 * @param context
	 * @param intent
	 * 			The intent that opened the activity
	 */
	public static void processIntent(Context context, Intent intent) {
		if (intent != null) {
			try {
				Bundle extras = intent.getExtras();
				if (extras != null && !extras.isEmpty()) {
					Bundle msg = extras.getBundle("notification");
					if (msg != null) {
						SwrveNotification notification = SwrveNotification.Builder.build(msg);
						SwrveGcmDeviceRegistration.newOpenedNotification(context, notification);
					}
				}
			} catch(Exception ex) {
				Log.e(LOG_TAG, "Could not process push notification intent", ex);
			}
		}
	}
}
