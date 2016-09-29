package com.swrve.sdk;

import android.app.Activity;
import android.content.Context;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

public class SwrveUnityPush implements ISwrvePushSDKListener {

    private static final String LOG = "SwrveUnityPush";

    private static String pushToken; //Do we prefer singletons?
    private static boolean swrvePushSdkAvailable = false;
    private static String lastGameObjectRegistered = null;

    public static List<SwrveUnityPushNotification> receivedNotifications = new ArrayList<>();
    public static List<SwrveUnityPushNotification> openedNotifications = new ArrayList<>();

    //Start the push SDK instance. This could be called from
    //Application onCreate or from Unity at a later point.
    public SwrveUnityPush(final Context context, final String senderId, boolean invokedFromUnity) {
        if (context instanceof Activity) {
            SwrveLogger.e(LOG, "Context must be application context");
            return;
        }

        //Check if the push sdk class is available
        try {
            Class.forName( "com.swrve.sdk.SwrvePushSDK" );
            swrvePushSdkAvailable = true;
        } catch (ClassNotFoundException e) {
            SwrveLogger.i(LOG, "SwrvePushSdk not found. Push disabled in android plugin." + e);
        }

        if (swrvePushSdkAvailable) {
            //Create push sdk instance.
            final SwrvePushSDK pushSdk = SwrvePushSDK.createInstance();
            if (pushSdk == null) {
                SwrveLogger.e(LOG, "Swrve Unity Push bridge unable to create SwrvePushSDK.");
                return;
            }
            final ISwrvePushSDKListener listener = this;

            if (!invokedFromUnity) {
                Log.i(LOG, "Initialising SwrvePushSdk. Invoked from SwrveUnityApplication.onCreate().");
                //Store pushToken - note it may not be null.
                pushToken = pushSdk.initialisePushSDK(context, listener, senderId);
            } else {
                final Activity activity = UnityPlayer.currentActivity;
                if (activity == null) {
                    Log.i(LOG, "UnityPlayer.currentActivity null, will not initialise SwrvePushSDK. This is unexpected, it was assumed SwrveUnityPush was being created from Unity SDK");
                    return;
                }
                Log.i(LOG, "Initialising SwrvePushSdk. Invoked from Unity SDK.");
                // This code needs to be run from the UI thread for internal Gcm AsyncTask registerInBackground operation.
                // Do not trust Unity to run this JNI invoked code from that thread.
                activity.runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        try {
                            //Store pushToken - note it may not be null.
                            pushToken = pushSdk.initialisePushSDK(context, listener, senderId);
                        } catch (Throwable ex) {
                            Log.e(LOG, "Couldn't initialise the Swrve Push SDK", ex);
                        }
                    }
                });
            }
        }
    }

    //This means the Unity SDK has started and has called init JNI on the push sdk.
    public static boolean unityRegisterForPush(final String gameObject) {
        if (!swrvePushSdkAvailable) {
            SwrveLogger.i(LOG, "swrvePushSDK is not available. Will not register Unity for Push.");
            return false;
        }

        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            SwrveLogger.e(LOG, "UnityPlayer.currentActivity was null");
            return false;
        }

        if (SwrveHelper.isNullOrEmpty(gameObject)) {
            SwrveLogger.e(LOG, "gameObject is null in unityRegisterforPush.");
            return false;
        }

        lastGameObjectRegistered = gameObject;

        //This pushToken may have been setup during construction of this object or
        //it may still be now.
        //TODO synchronize on pushToken?
        if (!SwrveHelper.isNullOrEmpty(pushToken)) {
            notifySDKOfPushToken(lastGameObjectRegistered, pushToken);
        }

        sdkIsReadyToReceivePushNotifications(activity);
        return true;
    }

    public static void sdkIsReadyToReceivePushNotifications(final Activity activity) {
        synchronized(receivedNotifications) {
            // Send pending received notifications to SDK instance
            for(SwrveUnityPushNotification notification : receivedNotifications) {
                notifySDKOfReceivedNotification(activity, notification);
            }
            // Remove right away as SDK is initialized
            receivedNotifications.clear();
        }

        synchronized(openedNotifications) {
            // Send pending opened notifications to SDK instance
            for(SwrveUnityPushNotification notification : openedNotifications) {
                notifySDKOfOpenedNotification(activity, notification);
            }
            // Remove right away as SDK is initialized
            openedNotifications.clear();
        }
    }

    private static SharedPreferences getUnityPushPreferences(Context context) {
        return context.getSharedPreferences(context.getPackageName() + "_swrve_push", Context.MODE_PRIVATE);
    }

    private static String getGameObject(Context context) {
        final SharedPreferences prefs = getUnityPushPreferences(context);
        return prefs.getString("game_object_name", "SwrveComponent");
    }

    private static void notifySDKOfNotification(Context context, String method, SwrveUnityPushNotification notification) {
        String gameObject = getGameObject(context);
        String serializedNotification = notification.toJson();
        if (serializedNotification != null) {
            UnityPlayer.UnitySendMessage(gameObject, method, serializedNotification.toString());
        }
    }

    private static void notifySDKOfReceivedNotification(Context context, SwrveUnityPushNotification notification) {
        notifySDKOfNotification(context, "OnNotificationReceived", notification);
    }

    private static void notifySDKOfOpenedNotification(Context context, SwrveUnityPushNotification notification) {
        notifySDKOfNotification(context, "OnOpenedFromPushNotification", notification);
    }

    //Called from Unity SDK via JNI
    public static void sdkAcknowledgeReceivedNotification(String id) {
        removeFromCollection(id, receivedNotifications);
    }

    //Called from Unity SDK via JNI
    public static void sdkAcknowledgeOpenedNotification(String id) {
        removeFromCollection(id, openedNotifications);
    }

    private static void removeFromCollection(String id, List<SwrveUnityPushNotification> collection) {
        synchronized(collection) {
            // Remove acknowledge notification
            Iterator<SwrveUnityPushNotification> it = collection.iterator();
            while(it.hasNext()) {
                SwrveUnityPushNotification notification = it.next();
                if (notification.getId().equals(id)) {
                    it.remove();
                }
            }
        }
    }

    //Just to be aware call can come from pushSdk amazon intent service, pushSdk google
    //service, or from unityRegisterForPush
    private static void notifySDKOfPushToken(String gameObject, String pushToken) {
        if (!swrvePushSdkAvailable) {
            SwrveLogger.e(LOG, "swrvePushSdk is not available. This function shouldn't be called.");
            return;
        }

        SwrvePushSDK pushSdk = SwrvePushSDK.getInstance();
        if (pushSdk == null) {
            SwrveLogger.e(LOG, "pushSdk has not been created.");
            return;
        }

        if (UnityPlayer.currentActivity == null) {
            SwrveLogger.e(LOG, "UnityPlayer.currentActivity was null.");
            return;
        }

        if (SwrveHelper.isNullOrEmpty(lastGameObjectRegistered)) {
            SwrveLogger.e(LOG, "lastGameObjectRegistered has not been set.");
            return;
        }

        try {
            // Call Unity SDK MonoBehaviour container with json
            JSONObject pushTokenInfo = new JSONObject("{}");
            pushTokenInfo.put("pushToken", pushToken);
            pushTokenInfo.put("pushTokenUserPropertyName", pushSdk.getPushTokenUserPropertyName());
            UnityPlayer.UnitySendMessage(gameObject, "OnPushTokenUpdated", pushTokenInfo.toString());
        } catch (JSONException ex) {
            SwrveLogger.e(LOG, "Error while creating device info json object", ex);
        }
    }

    @Override
    //From push sdk.
    //Adm uses an intent service (background thread) to generate call.
    //Gcm uses a service (main thread) to generate call.
    public void onPushTokenUpdated(String pushToken) {
        if (lastGameObjectRegistered == null) {
            Log.i(LOG, "lastGameObjectRegister was null (Unity has not registered for push yet). Will ignore push token update.");
            return;
        }
        this.pushToken = pushToken;
        notifySDKOfPushToken(lastGameObjectRegistered, pushToken);
    }

    @Override
    //From push sdk. Both via intent services (background thread).
    public void onMessageReceived(String msgId, Bundle msg) {
        SwrveLogger.i(LOG, "Unity Bridge Received push notification: " + msg.toString());
    }

    @Override
    //From a broadcast receiver (typically ui thread)
    public void onNotificationEngaged(Bundle msg) {
        SwrveLogger.i(LOG, "Unity Bridge Received notification engaged callback: " + msg.toString());
    }
}
