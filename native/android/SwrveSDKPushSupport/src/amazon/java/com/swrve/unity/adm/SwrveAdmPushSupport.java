package com.swrve.unity.adm;

import android.app.Activity;
import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;

import com.amazon.device.messaging.ADM;
import com.swrve.unity.SwrvePushSupport;
import com.unity3d.player.UnityPlayer;

public class SwrveAdmPushSupport extends SwrvePushSupport {
    private static final String TAG = "SwrveAdmRegistration";
    private static final int VERSION = 1;

    private static final String PROPERTY_PUSH_CONFIG_VERSION = "push_config_version";
    private static final String PROPERTY_PUSH_CONFIG_VERSION_VAL = "1";

    // Method names used when sending message from this plugin to Unity class "SwrveSDK/SwrveComponent.cs"
    public static final String ON_DEVICE_REGISTERED_METHOD = "OnDeviceRegisteredADM";
    public static final String ON_NOTIFICATION_RECEIVED_METHOD = "OnNotificationReceivedADM";
    public static final String ON_OPENED_FROM_PUSH_NOTIFICATION_METHOD = "OnOpenedFromPushNotificationADM";

    // Called by Unity
    public static int getVersion() {
        return VERSION;
    }

    private static boolean isAdmAvailable() {
        boolean admAvailable = false;
        try {
            Class.forName("com.amazon.device.messaging.ADM");
            admAvailable = true;
        } catch (ClassNotFoundException e) {
            Log.i(TAG, "ADM message class not found.", e);
        }
        return admAvailable;
    }

    // Called by Unity
    public static boolean initialiseAdm(final String gameObject, final String appTitle, final String iconId, final String materialIconId, final String largeIconId, final int accentColor) {
        if (!isAdmAvailable()) {
            Log.e(TAG, "Won't initialise ADM. ADM class not found.");
            return false;
        }

        if (UnityPlayer.currentActivity == null) {
            Log.e(TAG, "UnityPlayer.currentActivity was null");
            return false;
        }

        final Activity activity = UnityPlayer.currentActivity;
        try {
            saveConfig(gameObject, activity, appTitle, iconId, materialIconId, largeIconId, accentColor);
            Context context = activity.getApplicationContext();

            final ADM adm = new ADM(context);
            String registrationId = adm.getRegistrationId();
            if (SwrveAdmHelper.isNullOrEmpty(registrationId)) {
                Log.i(TAG, "adm.getRegistrationId() returned null. Will call adm.startRegister().");
                adm.startRegister();
            } else {
                Log.i(TAG, "adm.getRegistrationId() returned: " + registrationId);
                notifySDKOfRegistrationId(gameObject, registrationId);
            }
            sdkIsReadyToReceivePushNotifications(getGameObject(context), ON_NOTIFICATION_RECEIVED_METHOD, ON_OPENED_FROM_PUSH_NOTIFICATION_METHOD);
        } catch (Exception ex) {
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
        editor.apply();
    }

    static SharedPreferences getAdmPreferences(Context context) {
        return context.getSharedPreferences(context.getPackageName() + "_swrve_adm_push", Context.MODE_PRIVATE);
    }

    static String getGameObject(Context context) {
        final SharedPreferences prefs = getAdmPreferences(context);
        return prefs.getString(PROPERTY_GAME_OBJECT_NAME, "SwrveComponent");
    }

    static void onPushTokenUpdated(Context context, String registrationId) {
        String gameObject = getGameObject(context);
        if (SwrveAdmHelper.isNullOrEmpty(gameObject)) {
            Log.e(TAG, "Token has been updated, but can't inform UnitySDK because gameObject is empty");
            return;
        }
        notifySDKOfRegistrationId(gameObject, registrationId);
    }

    private static void notifySDKOfRegistrationId(String gameObject, String registrationId) {
        // Call Unity SDK MonoBehaviour container
        UnityPlayer.UnitySendMessage(gameObject, ON_DEVICE_REGISTERED_METHOD, registrationId);
    }
}
