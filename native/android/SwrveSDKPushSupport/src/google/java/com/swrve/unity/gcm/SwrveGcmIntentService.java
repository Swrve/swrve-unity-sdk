package com.swrve.unity.gcm;

import android.app.Notification;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.support.v4.app.NotificationCompat;
import android.util.Log;

import com.google.android.gms.gcm.GcmListenerService;
import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrvePushConstants;
import com.swrve.sdk.SwrvePushSDK;
import com.swrve.unity.SwrveNotification;
import com.swrve.unity.SwrvePushSupport;
import com.unity3d.player.UnityPlayer;

import java.util.Date;

public class SwrveGcmIntentService extends GcmListenerService {
	private static final String LOG_TAG = "SwrveGcmIntentService";
	private Integer notificationId;

	@Override
	public void onMessageReceived(String from, Bundle data) {
		processRemoteNotification(data);
	}

	private void processRemoteNotification(Bundle msg) {
		try {
			if (SwrvePushSDK.isSwrveRemoteNotification(msg)) {
				final Context context = getApplicationContext();
				final SharedPreferences prefs = SwrveGcmPushSupport.getGCMPreferences(context);
				final String activityClassName = SwrvePushSupport.getActivityClassName(context, prefs);

				String silentId = SwrvePushSDK.getSilentPushId(msg);
				if (SwrveHelper.isNullOrEmpty(silentId)) {
					// Visible push notification
					// Only call this listener if there is an activity running
					if (UnityPlayer.currentActivity != null) {
						// Call Unity SDK MonoBehaviour container
						SwrveNotification swrveNotification = SwrveNotification.Builder.build(msg);
						SwrveGcmPushSupport.newReceivedNotification(SwrveGcmPushSupport.getGameObject(UnityPlayer.currentActivity), SwrveGcmPushSupport.ON_NOTIFICATION_RECEIVED_METHOD, swrveNotification);
					}

					// Save influenced data
					if (msg.containsKey(SwrvePushConstants.SWRVE_INFLUENCED_WINDOW_MINS_KEY)) {
						// Save the date and push id for tracking influenced users
                        String normalId = SwrvePushSDK.getPushId(msg);
						SwrvePushSDK.saveInfluencedCampaign(context, normalId, msg.getString(SwrvePushConstants.SWRVE_INFLUENCED_WINDOW_MINS_KEY), new Date());
					}

					// Process notification
					processNotification(msg, activityClassName);
				} else {
					// Silent push notification
					if (msg.containsKey(SwrvePushConstants.SWRVE_INFLUENCED_WINDOW_MINS_KEY)) {
						// Save the date and push id for tracking influenced users
						SwrvePushSDK.saveInfluencedCampaign(context, silentId, msg.getString(SwrvePushConstants.SWRVE_INFLUENCED_WINDOW_MINS_KEY), new Date());
					}

					// Obtain and pass around the silent push object
					String payloadJson = msg.getString(SwrvePushConstants.SILENT_PAYLOAD_KEY);

					// Trigger the silent push broadcast
					Bundle silentBundle = new Bundle();
					silentBundle.putString(SwrvePushConstants.SILENT_PAYLOAD_KEY, payloadJson);
					Intent silentIntent = new Intent(SwrvePushSupport.SILENT_PUSH_BROADCAST_ACTION);
					silentIntent.putExtras(silentBundle);
					sendBroadcast(silentIntent);
				}
			}
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
		if (notificationId == null) {
			// Continue to work as pre 4.11
			int localNotificationId = generateNotificationId(notification);
			notificationManager.notify(localNotificationId, notification);
			return localNotificationId;
		} else {
			// Notification Id generated in createNotification
			notificationManager.notify(notificationId, notification);
			return notificationId;
		}
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
		Context context = getApplicationContext();
		SharedPreferences prefs = SwrveGcmPushSupport.getGCMPreferences(context);
		return SwrvePushSupport.createNotificationBuilder(context, prefs, msgText, msg, notificationId);
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
			notificationId = SwrvePushSupport.createNotificationId(msg);
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
		return SwrvePushSupport.createIntent(this, msg, activityClassName);
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
					Bundle msg = extras.getBundle(SwrvePushSupport.NOTIFICATION_PAYLOAD_KEY);
					if (msg != null) {
						SwrveNotification notification = SwrveNotification.Builder.build(msg);
						// Remove influenced data before letting Unity know
						SwrvePushSDK.removeInfluenceCampaign(context, notification.getId());
						SwrveGcmPushSupport.newOpenedNotification(SwrveGcmPushSupport.getGameObject(UnityPlayer.currentActivity), SwrveGcmPushSupport.ON_OPENED_FROM_PUSH_NOTIFICATION_METHOD, notification);
					}
				}
			} catch(Exception ex) {
				Log.e(LOG_TAG, "Could not process push notification intent", ex);
			}
		}
	}
}
