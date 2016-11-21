package com.swrve.unity.adm;

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
import java.util.LinkedList;

import com.amazon.device.messaging.ADMMessageHandlerBase;
import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.unity3d.player.UnityPlayer;

public class SwrveAdmIntentService extends ADMMessageHandlerBase {
    private final static String TAG = "SwrveAdm";
    private final static String AMAZON_RECENT_PUSH_IDS = "recent_push_ids";
    private final static String AMAZON_PREFERENCES = "swrve_amazon_unity_pref";
    private final static String UNITY_ACTIVITY_CLASS_NAME = "com.unity3d.player.UnityPlayerNativeActivity";

    protected final int DEFAULT_PUSH_ID_CACHE_SIZE = 16;

    public SwrveAdmIntentService() {
        super(SwrveAdmIntentService.class.getName());
    }

    public SwrveAdmIntentService(final String className) {
        super(className);
    }

    @Override
    protected void onMessage(final Intent intent) {
        if (intent == null) {
            Log.e(TAG, "Unexpected null intent");
            return;
        }

        final Bundle extras = intent.getExtras();
        if (extras != null && !extras.isEmpty()) {  // has effect of unparcelling Bundle
            Log.i(TAG, "Received ADM notification: " + extras.toString());
            processRemoteNotification(extras);
        }
    }

    @Override
    protected void onRegistrationError(final String string) {
        //This is considered fatal for ADM
        Log.e(TAG, "ADM Registration Error. Error string: " + string);
    }

    @Override
    protected void onRegistered(final String registrationId) {
        Log.i(TAG, "ADM Registered. RegistrationId: " + registrationId);
        Context context = getApplicationContext();
        SwrveAdmPushSupport.onPushTokenUpdated(context, registrationId);
    }

    @Override
    protected void onUnregistered(final String registrationId) {
        Log.i(TAG, "ADM Unregistered. RegistrationId: " + registrationId);
    }

    private static boolean isSwrveRemoteNotification(final Bundle msg) {
        Object rawId = msg.get(SwrveAdmHelper.SWRVE_TRACKING_KEY);
        String msgId = (rawId != null) ? rawId.toString() : null;
        return !SwrveAdmHelper.isNullOrEmpty(msgId);
    }

    private void processRemoteNotification(Bundle msg) {
        try {
            if (!isSwrveRemoteNotification(msg)) {
                Log.i(TAG, "ADM notification: but not processing as it doesn't contain:" + SwrveAdmHelper.SWRVE_TRACKING_KEY);
                return;
            }

            //Deduplicate notification
            //Get tracking key
            Object rawId = msg.get(SwrveAdmHelper.SWRVE_TRACKING_KEY);
            String msgId = (rawId != null) ? rawId.toString() : null;

            final String timestamp = msg.getString(SwrveAdmHelper.TIMESTAMP_KEY);
            if (SwrveAdmHelper.isNullOrEmpty(timestamp)) {
                Log.e(TAG, "ADM notification: but not processing as it's missing " + SwrveAdmHelper.TIMESTAMP_KEY);
                return;
            }

            //Check for duplicates. This is a necessary part of using ADM which might clone
            //a message as part of attempting to deliver it. We de-dupe by
            //checking against the tracking id and timestamp. (Multiple pushes with the same
            //tracking id are possible in some scenarios from Swrve).
            //Id is concatenation of tracking key and timestamp "$_p:$_s.t"
            String curId = msgId + ":" + timestamp;
            LinkedList<String> recentIds = getRecentNotificationIdCache();
            if (recentIds.contains(curId)) {
                //Found a duplicate
                Log.i(TAG, "ADM notification: but not processing because duplicate Id: " + curId);
                return;
            }

            //Try get de-dupe cache size
            int pushIdCacheSize = msg.getInt(SwrveAdmHelper.PUSH_ID_CACHE_SIZE_KEY, DEFAULT_PUSH_ID_CACHE_SIZE);

            //No duplicate found. Update the cache.
            updateRecentNotificationIdCache(recentIds, curId, pushIdCacheSize);

            final SharedPreferences prefs = SwrveAdmPushSupport.getAdmPreferences(getApplicationContext());
            String activityClassName = prefs.getString(SwrveAdmPushSupport.PROPERTY_ACTIVITY_NAME, null);
            if (SwrveAdmHelper.isNullOrEmpty(activityClassName)) {
                activityClassName = UNITY_ACTIVITY_CLASS_NAME;
            }

            // Process activity name (could be local or a class with a package name)
            if(!activityClassName.contains(".")) {
                activityClassName = getPackageName() + "." + activityClassName;
            }

            // Only call this listener if there is an activity running
            if (UnityPlayer.currentActivity != null) {
                // Call Unity SDK MonoBehaviour container
                SwrveNotification swrveNotification = SwrveNotification.Builder.build(msg);
                SwrveAdmPushSupport.newReceivedNotification(UnityPlayer.currentActivity, swrveNotification);
            }

            // Process notification
            processNotification(msg, activityClassName);
        } catch (Exception ex) {
            Log.e(TAG, "Error processing push notification", ex);
        }
    }

    private LinkedList<String> getRecentNotificationIdCache() {
        Context context = getApplicationContext();
        SharedPreferences sharedPreferences = context.getSharedPreferences(AMAZON_PREFERENCES, Context.MODE_PRIVATE);
        String jsonString = sharedPreferences.getString(AMAZON_RECENT_PUSH_IDS, "");
        Gson gson = new Gson();
        LinkedList<String> recentIds = gson.fromJson(jsonString, new TypeToken<LinkedList<String>>() {}.getType());
        recentIds = recentIds == null ? new LinkedList<String>() : recentIds;
        return recentIds;
    }

    private void updateRecentNotificationIdCache(LinkedList<String> recentIds, String newId, int maxCacheSize) {
        //Update queue
        recentIds.add(newId);

        //This must be at least zero;
        maxCacheSize = Math.max(0, maxCacheSize);

        //Maintain cache size limit
        while (recentIds.size() > maxCacheSize) {
            recentIds.remove();
        }

        //Store latest queue to shared preferences
        Context context = getApplicationContext();
        Gson gson = new Gson();
        String recentNotificationsJson = gson.toJson(recentIds);
        SharedPreferences sharedPreferences = context.getSharedPreferences(AMAZON_PREFERENCES, Context.MODE_PRIVATE);
        sharedPreferences.edit().putString(AMAZON_RECENT_PUSH_IDS, recentNotificationsJson).apply();
    }

    private void processNotification(final Bundle msg, String activityClassName) {
        try {
            // Put the message into a notification and post it.
            final NotificationManager mNotificationManager = (NotificationManager) this.getSystemService(Context.NOTIFICATION_SERVICE);
            
            final PendingIntent contentIntent = createPendingIntent(msg, activityClassName);
            if (contentIntent == null) {
                Log.e(TAG, "Error processing ADM push notification. Unable to create intent");
                return;
            }

            final Notification notification = createNotification(msg, contentIntent);
            if (notification == null) {
                Log.e(TAG, "Error processing ADM push notification. Unable to create notification.");
                return;
            }

            //Time to show notification
            showNotification(mNotificationManager, notification);
        } catch (Exception ex) {
            Log.e(TAG, "Error processing ADM push notification:", ex);
        }
    }

    private int showNotification(NotificationManager notificationManager, Notification notification) {
        int id = generateTimestampId();
        notificationManager.notify(id, notification);
        return id;
    }

    private int generateTimestampId() {
        return (int)(new Date().getTime() % Integer.MAX_VALUE);
    }

    private Notification createNotification(Bundle msg, PendingIntent contentIntent) {
        String msgText = msg.getString("text");
        if (!SwrveAdmHelper.isNullOrEmpty(msgText)) {
            // Build notification
            NotificationCompat.Builder builder = createNotificationBuilder(msgText, msg);
            builder.setContentIntent(contentIntent);
            return builder.build();
        }
        return null;
    }

    private NotificationCompat.Builder createNotificationBuilder(String msgText, Bundle msg) {
        Context context = getApplicationContext();
        SharedPreferences prefs = SwrveAdmPushSupport.getAdmPreferences(context);
        Resources res = getResources();
        String pushTitle = prefs.getString(SwrveAdmPushSupport.PROPERTY_APP_TITLE, null);
        String iconResourceName = prefs.getString(SwrveAdmPushSupport.PROPERTY_ICON_ID, null);
        String materialIconName = prefs.getString(SwrveAdmPushSupport.PROPERTY_MATERIAL_ICON_ID, null);
        String largeIconName = prefs.getString(SwrveAdmPushSupport.PROPERTY_LARGE_ICON_ID, null);
        int accentColor = prefs.getInt(SwrveAdmPushSupport.PROPERTY_ACCENT_COLOR, -1);

        PackageManager packageManager = context.getPackageManager();
        ApplicationInfo app = null;
        try {
            app = packageManager.getApplicationInfo(getPackageName(), 0);
        } catch (Exception exp) {
            exp.printStackTrace();
        }

        int iconId = 0;
        if (SwrveAdmHelper.isNullOrEmpty(iconResourceName)) {
            // Default to the application icon
            if (app != null) {
                iconId = app.icon;
            }
        } else {
            iconId = res.getIdentifier(iconResourceName, "drawable", getPackageName());
        }

        int finalIconId = iconId;
        boolean mustUseMaterialDesignIcon = (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.LOLLIPOP);
        if (SwrveAdmHelper.isNullOrEmpty(materialIconName)) {
            // No material (Android L+) icon configured
            Log.w(TAG, "No material icon specified. We recommend setting a special material icon for Android L+");
        } else if(mustUseMaterialDesignIcon) {
            // Running on Android L+
            finalIconId = res.getIdentifier(materialIconName, "drawable", getPackageName());
        }

        if (SwrveAdmHelper.isNullOrEmpty(pushTitle)) {
            if (app != null) {
                // No configured push title
                CharSequence appTitle = app.loadLabel(packageManager);
                if (appTitle != null) {
                    // Default to the application title
                    pushTitle = appTitle.toString();
                }
            }
            if (SwrveAdmHelper.isNullOrEmpty(pushTitle)) {
                pushTitle = "Configure your app title";
            }
        }

        // Build notification
        NotificationCompat.Builder builder = new NotificationCompat.Builder(this)
                .setSmallIcon(finalIconId)
                .setContentTitle(pushTitle)
                .setStyle(new NotificationCompat.BigTextStyle().bigText(msgText))
                .setContentText(msgText)
                .setTicker(msgText)
                .setAutoCancel(true);

        if (largeIconName != null) {
            int largeIconId = res.getIdentifier(largeIconName, "drawable", getPackageName());
            Bitmap largeIconBitmap = BitmapFactory.decodeResource(getResources(), largeIconId);
            builder.setLargeIcon(largeIconBitmap);
        }

        if (accentColor >= 0) {
            builder.setColor(accentColor);
        }

        String msgSound = msg.getString("sound");
        if (!SwrveAdmHelper.isNullOrEmpty(msgSound)) {
            Uri soundUri;
            if (msgSound.equalsIgnoreCase("default")) {
                soundUri = RingtoneManager.getDefaultUri(RingtoneManager.TYPE_NOTIFICATION);
            } else {
                String packageName = getApplicationContext().getPackageName();
                soundUri = Uri.parse(ContentResolver.SCHEME_ANDROID_RESOURCE + "://" + packageName + "/raw/" + msgSound);
            }
            builder.setSound(soundUri);
        }
        return builder;
    }

    private PendingIntent createPendingIntent(Bundle msg, String activityClassName) {
        // Add notification to bundle
        Intent intent = createIntent(msg, activityClassName);
        if (intent == null) {
            return null;
        }
        return PendingIntent.getActivity(this, generateTimestampId(), intent, PendingIntent.FLAG_UPDATE_CURRENT);
    }

    private Intent createIntent(Bundle msg, String activityClassName) {
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
}

