package com.swrve.unity;

import android.app.Activity;
import android.app.NotificationChannel;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.content.res.Resources;
import android.os.Bundle;

import com.swrve.sdk.SwrveCampaignInfluence;
import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrveNotificationBuilder;
import com.swrve.sdk.SwrveNotificationConfig;
import com.swrve.sdk.SwrveNotificationConstants;
import com.swrve.sdk.SwrveUnityCommon;
import com.swrve.sdk.SwrveUnityCommonHelper;
import com.unity3d.player.UnityPlayer;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.Date;
import java.util.List;

public abstract class SwrvePushSupport {
    private static final String PROPERTY_ACTIVITY_NAME = "activity_name";
    static final String PROPERTY_GAME_OBJECT_NAME = "game_object_name";

    private static final String PROPERTY_ICON_ID = "icon_id";
    private static final String PROPERTY_MATERIAL_ICON_ID = "material_icon_id";
    private static final String PROPERTY_LARGE_ICON_ID = "large_icon_id";
    private static final String PROPERTY_ACCENT_COLOR_HEX = "accent_color_hex";

    protected static final String NOTIFICATION_PAYLOAD_KEY = "notification";

    private static final String SILENT_PUSH_BROADCAST_ACTION = "com.swrve.SILENT_PUSH_ACTION";

    private static final String ON_NOTIFICATION_RECEIVED_METHOD = "OnNotificationReceived";
    private static final String ON_OPENED_FROM_PUSH_NOTIFICATION_METHOD = "OnOpenedFromPushNotification";

    static void newReceivedNotification(String gameObject, SwrveUnityNotification notification) {
        if (notification != null) {
            String serializedNotification = notification.toJson();
            if (serializedNotification != null) {
                SwrveUnityCommon.UnitySendMessage(gameObject, ON_NOTIFICATION_RECEIVED_METHOD, serializedNotification);
            }
        }
    }

    public static void newOpenedNotification(String gameObject, SwrveUnityNotification notification) {
        if (notification != null) {
            String serializedNotification = notification.toJson();
            if (serializedNotification != null) {
                SwrveUnityCommon.UnitySendMessage(gameObject, ON_OPENED_FROM_PUSH_NOTIFICATION_METHOD, serializedNotification);
            }
        }
    }

    static SwrveNotificationBuilder createSwrveNotificationBuilder(Context context, SharedPreferences prefs) {
        Resources res = context.getResources();
        String iconResourceName = prefs.getString(SwrvePushSupport.PROPERTY_ICON_ID, null);
        String materialIconName = prefs.getString(SwrvePushSupport.PROPERTY_MATERIAL_ICON_ID, null);
        String largeIconName = prefs.getString(SwrvePushSupport.PROPERTY_LARGE_ICON_ID, null);
        String accentColorHex = prefs.getString(SwrvePushSupport.PROPERTY_ACCENT_COLOR_HEX, null);
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

        int largeIconId = -1;
        if (largeIconName != null) {
            largeIconId = res.getIdentifier(largeIconName, "drawable", packageName);
        }

        // Create the NotificationChannel (only on API 26+, channel might be null)
        NotificationChannel channel = SwrveUnityCommonHelper.getDefaultNotificationChannel();

        SwrveNotificationConfig.Builder builder = new SwrveNotificationConfig.Builder(iconId, materialIcon, channel).largeIconDrawableId(largeIconId);

        if (SwrveHelper.isNotNullOrEmpty(accentColorHex)) {
            builder.accentColorHex(accentColorHex);
        }

        return new UnitySwrveNotificationBuilder(context, builder.build());
    }

    static String getActivityClassName(Context ctx, SharedPreferences prefs) {
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

    static Intent createIntent(Context ctx, Bundle msg, String activityClassName) {
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
            SharedPreferences sharedPreferences = context.getSharedPreferences(SwrveCampaignInfluence.INFLUENCED_PREFS, Context.MODE_PRIVATE);
            // Transform into JSON for the C# layer
            List<SwrveCampaignInfluence.InfluenceData> influenceData = new SwrveCampaignInfluence().getSavedInfluencedData(sharedPreferences);
            for (SwrveCampaignInfluence.InfluenceData data : influenceData) {
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

    public static void saveConfig(SharedPreferences.Editor editor, String gameObject, Activity activity,
                                  String iconId, String materialIconId,
                                  String largeIconId, String accentColorHex) {
        editor.putString(PROPERTY_ACTIVITY_NAME, activity.getLocalClassName());
        editor.putString(PROPERTY_GAME_OBJECT_NAME, gameObject);
        editor.putString(PROPERTY_ICON_ID, iconId);
        editor.putString(PROPERTY_MATERIAL_ICON_ID, materialIconId);
        editor.putString(PROPERTY_LARGE_ICON_ID, largeIconId);
        editor.putString(PROPERTY_ACCENT_COLOR_HEX, accentColorHex);
    }

    private static class UnitySwrveNotificationBuilder extends SwrveNotificationBuilder {

        UnitySwrveNotificationBuilder(Context context, SwrveNotificationConfig config) {
            super(context, config);
        }

        @Override
        public Intent createButtonIntent(Context context, Bundle msg) {
            // Mark this push so that Unity does not send engagement nor process the deeplink in the C# layer
            msg.putBoolean("SWRVE_UNITY_DO_NOT_PROCESS", true);
            // Send this intent to the flavour Unity engage receiver which will properly notify the C# layer
            Intent intent = super.createButtonIntent(context, msg);
            intent.setComponent(new ComponentName(context, SwrveUnityNotificationEngageReceiver.class));
            return intent;
        }
    }

    static void saveCampaignInfluence(Bundle msg, Context context, String pushId) {
        // Attempt to save influence data for push
        SwrveCampaignInfluence campaignInfluence = new SwrveCampaignInfluence();
        campaignInfluence.saveInfluencedCampaign(context, pushId, msg, new Date());
    }

    public static void removeInfluenceCampaign(Context context, String pushId) {
        SwrveCampaignInfluence campaignInfluence = new SwrveCampaignInfluence();
        campaignInfluence.removeInfluenceCampaign(context, pushId);
    }

    static Intent getSilentPushIntent(Bundle msg) {
        // Obtain and pass around the silent push object
        String payloadJson = msg.getString(SwrveNotificationConstants.SILENT_PAYLOAD_KEY);

        // Create the silent push that will be used to broadcast
        Bundle silentBundle = new Bundle();
        silentBundle.putString(SwrveNotificationConstants.SILENT_PAYLOAD_KEY, payloadJson);
        Intent silentIntent = new Intent(SwrvePushSupport.SILENT_PUSH_BROADCAST_ACTION);
        silentIntent.putExtras(silentBundle);
        return silentIntent;
    }

}
