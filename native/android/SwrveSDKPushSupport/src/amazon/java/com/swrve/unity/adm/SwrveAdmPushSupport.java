package com.swrve.unity.adm;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

import android.app.Activity;
import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;

import com.amazon.device.messaging.ADM;
import com.unity3d.player.UnityPlayer;

public class SwrveAdmPushSupport {
    private static final String TAG = "SwrveAdmRegistration";
    private static final int VERSION = 1;

    public static final String PROPERTY_ACTIVITY_NAME = "activity_name";
    public static final String PROPERTY_GAME_OBJECT_NAME = "game_object_name";

    public static final String PROPERTY_APP_TITLE = "app_title";
    public static final String PROPERTY_ICON_ID = "icon_id";
    public static final String PROPERTY_MATERIAL_ICON_ID = "material_icon_id";
    public static final String PROPERTY_LARGE_ICON_ID = "large_icon_id";
    public static final String PROPERTY_ACCENT_COLOR = "accent_color";
    public static final String PROPERTY_PUSH_CONFIG_VERSION = "push_config_version";
    public static final String PROPERTY_PUSH_CONFIG_VERSION_VAL = "1";

    public static String lastGameObjectRegistered;
    public static List<SwrveNotification> receivedNotifications = new ArrayList<SwrveNotification>();
    public static List<SwrveNotification> openedNotifications = new ArrayList<SwrveNotification>();

    public static int getVersion() {
        return VERSION;
    }

    private static boolean IsAdmAvailable() {
        boolean admAvailable = false;
        try {
            Class.forName("com.amazon.device.messaging.ADM");
            admAvailable = true;
        } catch (ClassNotFoundException e) {
            Log.i(TAG, "ADM message class not found.", e);
        }
        return admAvailable;
    }

    public static boolean initialiseAdm(final String gameObject, final String appTitle, final String iconId, final String materialIconId, final String largeIconId, final int accentColor) {
        if (!IsAdmAvailable()) {
            Log.e(TAG, "Won't initialise ADM. ADM class not found.");
            return false;
        }

        if (UnityPlayer.currentActivity == null) {
            Log.e(TAG, "UnityPlayer.currentActivity was null");
            return false;
        }

        lastGameObjectRegistered = gameObject;
        final Activity activity = UnityPlayer.currentActivity;
        try {
            saveConfig(gameObject, activity, appTitle, iconId, materialIconId, largeIconId, accentColor);
            Context context = activity.getApplicationContext();

            try {
                final ADM adm = new ADM(context);
                String registrationId = adm.getRegistrationId();
                if (SwrveAdmHelper.isNullOrEmpty(registrationId)) {
                    Log.i(TAG, "adm.getRegistrationId() returned null. Will call adm.startRegister().");
                    adm.startRegister();
                } else {
                    Log.i(TAG, "adm.getRegistrationId() returned: " + registrationId);
                    notifySDKOfRegistrationId(gameObject, registrationId);
                }
                sdkIsReadyToReceivePushNotifications(activity);
            } catch (Throwable exp) {
                // Don't trust Amazon and all the moving parts to work as expected
                Log.e(TAG, "Couldn't obtain the registration key for the device.", exp);
                return false;
            }
        } catch (Throwable ex) {
            Log.e(TAG, "Couldn't obtain the ADM registration id for the device", ex);
            return false;
        }
        return true;
    }

    private static void saveConfig(String gameObject, Activity activity, String appTitle, String iconId, String materialIconId, String largeIconId, int accentColor) {
        Context context = activity.getApplicationContext();
        final SharedPreferences prefs = getAdmPreferences(context);

        SharedPreferences.Editor editor = prefs.edit();
        editor.putString(PROPERTY_PUSH_CONFIG_VERSION, PROPERTY_PUSH_CONFIG_VERSION_VAL);
        editor.putString(PROPERTY_ACTIVITY_NAME, activity.getLocalClassName());
        editor.putString(PROPERTY_GAME_OBJECT_NAME, gameObject);
        editor.putString(PROPERTY_APP_TITLE, appTitle);
        editor.putString(PROPERTY_ICON_ID, iconId);
        editor.putString(PROPERTY_MATERIAL_ICON_ID, materialIconId);
        editor.putString(PROPERTY_LARGE_ICON_ID, largeIconId);
        editor.putInt(PROPERTY_ACCENT_COLOR, accentColor);
        editor.commit();
    }

    public static SharedPreferences getAdmPreferences(Context context) {
        return context.getSharedPreferences(context.getPackageName() + "_swrve_adm_push", Context.MODE_PRIVATE);
    }

    private static String getGameObject(Context context) {
        final SharedPreferences prefs = getAdmPreferences(context);
        return prefs.getString(PROPERTY_GAME_OBJECT_NAME, "SwrveComponent");
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

    public static void onPushTokenUpdated(Context context, String registrationId) {
        String gameObject = getGameObject(context);
        if (SwrveAdmHelper.isNullOrEmpty(gameObject)) {
            Log.e(TAG, "Token has been updated, but can't inform UnitySDK because gameObject is empty");
            return;
        }
        notifySDKOfRegistrationId(gameObject, registrationId);
    }

    private static void notifySDKOfRegistrationId(String gameObject, String registrationId) {
        // Call Unity SDK MonoBehaviour container
        UnityPlayer.UnitySendMessage(gameObject, "ADMOnDeviceRegistered", registrationId);
    }

    private static void notifySDKOfReceivedNotification(Context context, SwrveNotification notification) {
        String gameObject = getGameObject(context);
        String serializedNotification = notification.toJson();
        if (serializedNotification != null) {
            UnityPlayer.UnitySendMessage(gameObject, "ADMOnNotificationReceived", serializedNotification.toString());
        }
    }

    private static void notifySDKOfOpenedNotification(Context context, SwrveNotification notification) {
        String gameObject = getGameObject(context);
        String serializedNotification = notification.toJson();
        if (serializedNotification != null) {
            UnityPlayer.UnitySendMessage(gameObject, "ADMOnOpenedFromPushNotification", serializedNotification.toString());
        }
    }

    public static void sdkAcknowledgeReceivedNotification(String id) {
        removeFromCollection(id, receivedNotifications);
    }

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
}
