package com.swrve.unity.gcm;

import android.app.Notification;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.ContentResolver;
import android.content.Context;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.res.Resources;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.media.RingtoneManager;
import android.net.Uri;
import android.os.Bundle;
import android.support.v4.app.NotificationCompat;
import android.util.Log;
import java.util.Date;

import com.google.android.gms.gcm.GcmListenerService;
import com.unity3d.player.UnityPlayer;

public class SwrveGcmIntentService extends GcmListenerService {
	private static final String LOG_TAG = "SwrveGcmIntentService";

	@Override
	public void onMessageReceived(String from, Bundle data) {
		processRemoteNotification(data);
	}

	private static boolean isSwrveRemoteNotification(final Bundle msg) {
		Object rawId = msg.get("_p");
		String msgId = (rawId != null) ? rawId.toString() : null;
		return msgId != null && !msgId.equals("");
     }

	private void processRemoteNotification(Bundle msg) {
		try {
			if (isSwrveRemoteNotification(msg)) {
				final SharedPreferences prefs = SwrveGcmDeviceRegistration.getGCMPreferences(getApplicationContext());
				String activityClassName = prefs.getString(SwrveGcmDeviceRegistration.PROPERTY_ACTIVITY_NAME, null);
				if (isEmptyString(activityClassName)) {
					activityClassName = "com.unity3d.player.UnityPlayerNativeActivity";
				}
				
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
		Context context = getApplicationContext();
		SharedPreferences prefs = SwrveGcmDeviceRegistration.getGCMPreferences(context);
		Resources res = getResources();
		String pushTitle = prefs.getString(SwrveGcmDeviceRegistration.PROPERTY_APP_TITLE, null);
		String iconResourceName = prefs.getString(SwrveGcmDeviceRegistration.PROPERTY_ICON_ID, null);
		String materialIconName = prefs.getString(SwrveGcmDeviceRegistration.PROPERTY_MATERIAL_ICON_ID, null);
		String largeIconName = prefs.getString(SwrveGcmDeviceRegistration.PROPERTY_LARGE_ICON_ID, null);
		int accentColor = prefs.getInt(SwrveGcmDeviceRegistration.PROPERTY_ACCENT_COLOR, -1);

		PackageManager packageManager = context.getPackageManager();
		ApplicationInfo app = null;
		try {
			app = packageManager.getApplicationInfo(getPackageName(), 0);
		} catch (Exception exp) {
			exp.printStackTrace();
		}

		int iconId = 0;
		if (isEmptyString(iconResourceName)) {
			// Default to the application icon
			if (app != null) {
				iconId = app.icon;
			}
		} else {
			iconId = res.getIdentifier(iconResourceName, "drawable", getPackageName());
		}

		int finalIconId = iconId;
		boolean mustUseMaterialDesignIcon = (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.LOLLIPOP);
		if (isEmptyString(materialIconName)) {
			// No material (Android L+) icon configured
			Log.w(LOG_TAG, "No mateiral icon specified. We recommend setting a special material icon for Android L+");
		} else if(mustUseMaterialDesignIcon) {
			// Running on Android L+
			finalIconId = res.getIdentifier(materialIconName, "drawable", getPackageName());
		}

		if (isEmptyString(pushTitle)) {
			if (app != null) {
				// No configured push title
				CharSequence appTitle = app.loadLabel(packageManager);
				if (appTitle != null) {
					// Default to the application title
					pushTitle = appTitle.toString();
				}
			}
			if (isEmptyString(pushTitle)) {
				pushTitle = "Configure your app title";
			}
		}

		// Build notification
		NotificationCompat.Builder mBuilder = new NotificationCompat.Builder(this)
				.setSmallIcon(finalIconId)
				.setContentTitle(pushTitle)
				.setStyle(new NotificationCompat.BigTextStyle().bigText(msgText))
				.setContentText(msgText)
				.setTicker(msgText)
				.setAutoCancel(true);

		if (largeIconName != null) {
			int largeIconId = res.getIdentifier(largeIconName, "drawable", getPackageName());
			Bitmap largeIconBitmap = BitmapFactory.decodeResource(getResources(), largeIconId);
			mBuilder.setLargeIcon(largeIconBitmap);
		}

		if (accentColor >= 0) {
			mBuilder.setColor(accentColor);
		}

		String msgSound = msg.getString("sound");
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
