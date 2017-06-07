package com.swrve.unity;

import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.content.res.Resources;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.os.Bundle;
import android.support.v4.app.NotificationCompat;

import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrvePushNotificationConfig;
import com.swrve.sdk.SwrvePushSDK;
import com.unity3d.player.UnityPlayer;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

public abstract class SwrvePushSupport {
    protected static final String PROPERTY_ACTIVITY_NAME = "activity_name";
    protected static final String PROPERTY_GAME_OBJECT_NAME = "game_object_name";

    protected static final String PROPERTY_APP_TITLE = "app_title";
    protected static final String PROPERTY_ICON_ID = "icon_id";
    protected static final String PROPERTY_MATERIAL_ICON_ID = "material_icon_id";
    protected static final String PROPERTY_LARGE_ICON_ID = "large_icon_id";
    protected static final String PROPERTY_ACCENT_COLOR = "accent_color";

    public static final String NOTIFICATION_PAYLOAD_KEY = "notification";

    public static final String SILENT_PUSH_BROADCAST_ACTION = "com.swrve.SILENT_PUSH_ACTION";

    private static final List<SwrveNotification> receivedNotifications = new ArrayList<SwrveNotification>();
    private static final List<SwrveNotification> openedNotifications = new ArrayList<SwrveNotification>();

    protected static void sdkIsReadyToReceivePushNotifications(String gameObject, String receivedUnityCall, String openedUnityCall) {
        synchronized(receivedNotifications) {
            // Send pending received notifications to SDK instance
            for(SwrveNotification notification : receivedNotifications) {
                notifySDKOfReceivedNotification(gameObject, receivedUnityCall, notification);
            }
            // Remove right away as SDK is initialized
            receivedNotifications.clear();
        }

        synchronized(openedNotifications) {
            // Send pending opened notifications to SDK instance
            for(SwrveNotification notification : openedNotifications) {
                notifySDKOfOpenedNotification(gameObject, openedUnityCall, notification);
            }
            // Remove right away as SDK is initialized
            openedNotifications.clear();
        }
    }

    public static void newReceivedNotification(String gameObject, String receivedUnityCall, SwrveNotification notification) {
        if (notification != null) {
            synchronized(receivedNotifications) {
                receivedNotifications.add(notification);
                notifySDKOfReceivedNotification(gameObject, receivedUnityCall, notification);
            }
        }
    }

    public static void newOpenedNotification(String gameObject, String openedUnityCall, SwrveNotification notification) {
        if (notification != null) {
            synchronized(openedNotifications) {
                openedNotifications.add(notification);
                notifySDKOfOpenedNotification(gameObject, openedUnityCall, notification);
            }
        }
    }

    private static void notifySDKOfReceivedNotification(String gameObject, String receivedUnityCall, SwrveNotification notification) {
        String serializedNotification = notification.toJson();
        if (serializedNotification != null) {
            UnityPlayer.UnitySendMessage(gameObject, receivedUnityCall, serializedNotification);
        }
    }

    private static void notifySDKOfOpenedNotification(String gameObject, String openedUnityCall, SwrveNotification notification) {
        String serializedNotification = notification.toJson();
        if (serializedNotification != null) {
            UnityPlayer.UnitySendMessage(gameObject, openedUnityCall, serializedNotification);
        }
    }

    // Called by Unity
    public static void sdkAcknowledgeReceivedNotification(String id) {
        removeFromCollection(id, receivedNotifications);
    }

    // Called by Unity
    public static void sdkAcknowledgeOpenedNotification(String id) {
        removeFromCollection(id, openedNotifications);
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

    public static NotificationCompat.Builder createNotificationBuilder(Context context, SharedPreferences prefs, String msgText, Bundle msg) {
        Resources res = context.getResources();
        String pushTitle = prefs.getString(SwrvePushSupport.PROPERTY_APP_TITLE, null);
        String iconResourceName = prefs.getString(SwrvePushSupport.PROPERTY_ICON_ID, null);
        String materialIconName = prefs.getString(SwrvePushSupport.PROPERTY_MATERIAL_ICON_ID, null);
        String largeIconName = prefs.getString(SwrvePushSupport.PROPERTY_LARGE_ICON_ID, null);
        Integer accentColorObject = null;
        int accentColor = prefs.getInt(SwrvePushSupport.PROPERTY_ACCENT_COLOR, -1);
        if (accentColor != -1) {
            accentColorObject = accentColor;
        }
        String packageName = context.getPackageName();

        PackageManager packageManager = context.getPackageManager();
        ApplicationInfo app = null;
        try {
            app = packageManager.getApplicationInfo(packageName, 0);
        } catch (Exception exp) {
            exp.printStackTrace();
        }

        int iconId = -1;
        if (SwrveHelper.isNullOrEmpty(iconResourceName)) {
            // Default to the application icon
            if (app != null) {
                iconId = app.icon;
            }
        } else {
            iconId = res.getIdentifier(iconResourceName, "drawable", packageName);
        }

        int materialIcon = -1;
        if (SwrveHelper.isNotNullOrEmpty(materialIconName)) {
            materialIcon = res.getIdentifier(materialIconName, "drawable", packageName);
        }

        Bitmap largeIconBitmap = null;
        if (largeIconName != null) {
            int largeIconId = res.getIdentifier(largeIconName, "drawable", packageName);
            largeIconBitmap = BitmapFactory.decodeResource(context.getResources(), largeIconId);
        }

        if (SwrveHelper.isNullOrEmpty(pushTitle)) {
            if (app != null) {
                // No configured push title
                CharSequence appTitle = app.loadLabel(packageManager);
                if (appTitle != null) {
                    // Default to the application title
                    pushTitle = appTitle.toString();
                }
            }
            if (SwrveHelper.isNullOrEmpty(pushTitle)) {
                pushTitle = "Configure your app title";
            }
        }

        // Build notification
        SwrvePushNotificationConfig notification = new SwrvePushNotificationConfig(null, iconId, materialIcon, largeIconBitmap, accentColorObject, pushTitle);
        return notification.createNotificationBuilder(context, msgText, msg);
    }

    public static String getActivityClassName(Context ctx, SharedPreferences prefs) {
        String activityClassName = prefs.getString(PROPERTY_ACTIVITY_NAME, null);
        if (SwrveHelper.isNullOrEmpty(activityClassName)) {
            activityClassName = "com.unity3d.player.UnityPlayerNativeActivity";
        }

        // Process activity name (could be local or a class with a package name)
        if(!activityClassName.contains(".")) {
            activityClassName = ctx.getPackageName() + "." + activityClassName;
        }
        return activityClassName;
    }

    public static Intent createIntent(Context ctx, Bundle msg, String activityClassName) {
        try {
            Intent intent = new Intent(ctx, Class.forName(activityClassName));
            intent.putExtra(NOTIFICATION_PAYLOAD_KEY, msg);
            intent.setAction("openActivity");
            return intent;
        } catch (ClassNotFoundException e) {
            e.printStackTrace();
        }
        return null;
    }

    // Called by Unity
    public static String getInfluenceDataJson() {
        JSONArray influenceArray = new JSONArray();

        final Context context = UnityPlayer.currentActivity;
        if (context != null) {
            SharedPreferences sharedPreferences = context.getSharedPreferences(SwrvePushSDK.INFLUENCED_PREFS, Context.MODE_PRIVATE);
            // Transform into JSON for the C# layer
            List<SwrvePushSDK.InfluenceData> influenceData = SwrvePushSDK.readSavedInfluencedData(sharedPreferences);
            for (SwrvePushSDK.InfluenceData data : influenceData) {
                JSONObject influenceDataJson = data.toJson();
                if (influenceDataJson != null) {
                    influenceArray.put(influenceDataJson);
                }
            }

            // Remove the influence data
            sharedPreferences.edit().clear().commit();
        }

        return influenceArray.toString();
    }
}
