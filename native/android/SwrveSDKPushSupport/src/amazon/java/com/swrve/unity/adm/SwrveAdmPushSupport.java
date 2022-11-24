package com.swrve.unity.adm;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.util.Log;

import com.amazon.device.messaging.ADM;
import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrveLogger;
import com.swrve.sdk.SwrveUnityCommon;
import com.swrve.unity.SwrvePushServiceManagerCommon;
import com.swrve.unity.SwrvePushSupport;
import com.swrve.unity.SwrveUnityNotification;
import com.unity3d.player.UnityPlayer;

public class SwrveAdmPushSupport extends SwrvePushSupport {
    private static final String TAG = "SwrveAdmRegistration";
    private static final int VERSION = 1;

    private static final String PROPERTY_PUSH_CONFIG_VERSION = "push_config_version";
    private static final String PROPERTY_PUSH_CONFIG_VERSION_VAL = "1";

    // Method names used when sending message from this plugin to Unity class "SwrveSDK/SwrveComponent.cs"
    private static final String ON_DEVICE_REGISTERED_METHOD = "OnDeviceRegisteredADM";

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
    public static boolean initialiseAdm(final String gameObject,
                                        final String iconId, final String materialIconId,
                                        final String largeIconId, final String accentColorHex) {
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
            saveConfig(gameObject, activity, iconId, materialIconId, largeIconId, accentColorHex);
            Context context = activity.getApplicationContext();

            final ADM adm = new ADM(context);
            String registrationId = adm.getRegistrationId();
            if (SwrveHelper.isNullOrEmpty(registrationId)) {
                Log.i(TAG, "adm.getRegistrationId() returned null. Will call adm.startRegister().");
                adm.startRegister();
            } else {
                Log.i(TAG, "adm.getRegistrationId() returned: " + registrationId);
                notifySDKOfRegistrationId(gameObject, registrationId);
            }
        } catch (Exception ex) {
            Log.e(TAG, "Couldn't obtain the ADM registration id for the device", ex);
            return false;
        }
        return true;
    }

    private static void saveConfig(String gameObject, Activity activity, String iconId,
                                   String materialIconId, String largeIconId, String accentColorHex) {
        Context context = activity.getApplicationContext();
        final SharedPreferences prefs = SwrvePushServiceManagerCommon.getPreferences(context);

        SharedPreferences.Editor editor = prefs.edit();
        editor.putString(PROPERTY_PUSH_CONFIG_VERSION, PROPERTY_PUSH_CONFIG_VERSION_VAL);
        SwrvePushSupport.saveConfig(editor, gameObject, iconId, materialIconId, largeIconId, accentColorHex);
        editor.apply();
    }

    static void onPushTokenUpdated(Context context, String registrationId) {
        String gameObject = SwrvePushServiceManagerCommon.getGameObject(context);
        if (SwrveHelper.isNullOrEmpty(gameObject)) {
            Log.e(TAG, "Token has been updated, but can't inform UnitySDK because gameObject is empty");
            return;
        }
        notifySDKOfRegistrationId(gameObject, registrationId);
    }

    private static void notifySDKOfRegistrationId(String gameObject, String registrationId) {
        // Call Unity SDK MonoBehaviour container
        SwrveUnityCommon.UnitySendMessage(gameObject, ON_DEVICE_REGISTERED_METHOD, registrationId);
    }

    static void processIntent(Context context, Intent intent) {
        if (intent == null) {
            return;
        }
        try {
            Bundle extras = intent.getExtras();
            if (extras != null && !extras.isEmpty()) {
                Bundle msg = extras.getBundle(SwrvePushSupport.NOTIFICATION_PAYLOAD_KEY);
                if (msg != null) {
                    SwrveUnityNotification notificationUnity = SwrveUnityNotification.Builder.build(msg);
                    // Remove influenced data before letting Unity know
                    SwrvePushSupport.removeInfluenceCampaign(context, notificationUnity.getId());
                    SwrveAdmPushSupport.newOpenedNotification(UnityPlayer.currentActivity, intent);
                }
            }
        } catch(Exception ex) {
            SwrveLogger.e("Could not process push notification intent", ex);
        }
    }
}
