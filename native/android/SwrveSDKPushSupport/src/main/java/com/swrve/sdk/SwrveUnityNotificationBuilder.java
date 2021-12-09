package com.swrve.sdk;

import android.content.Context;
import android.content.Intent;
import android.os.Build;
import android.os.Bundle;

import com.swrve.sdk.notifications.model.SwrveNotificationButton;

public class SwrveUnityNotificationBuilder extends SwrveNotificationBuilder {

    public SwrveUnityNotificationBuilder(Context context, SwrveNotificationConfig config) {
        super(context, config);
    }

    @Override
    public Intent createButtonIntent(Context context, Bundle msg, SwrveNotificationButton.ActionType actionType, boolean isDismissAction) {
        // Mark this push so that Unity does not send engagement nor process the deeplink in the C# layer
        msg.putBoolean("SWRVE_UNITY_DO_NOT_PROCESS", true);
        // Send this intent to the Unity SwrveNotificationEngage which will properly notify the C# layer
        Intent intent = new Intent(context, getIntentClass(Build.VERSION.SDK_INT, isDismissAction));
        intent.putExtra(SwrveNotificationConstants.PUSH_BUNDLE, msg);
        intent.putExtra(SwrveNotificationConstants.PUSH_NOTIFICATION_ID, getNotificationId());
        intent.putExtra(SwrveNotificationConstants.CAMPAIGN_TYPE, campaignType);
        return intent;
    }

    @Override
    public Class getIntentClass(int sdkVersion, boolean isDismissAction) {
        Class clazz;
        // A dismiss action should dismiss the notification without opening the app
//            if (sdkVersion >= Build.VERSION_CODES.S && !isDismissAction) {
        if (sdkVersion >= 31 && !isDismissAction) { // Unity compile level is 30 has does not contain Build.VERSION_CODES.S constant
            clazz = SwrveUnityNotificationEngageActivity.class;
        } else {
            clazz = SwrveUnityNotificationEngageReceiver.class;
        }
        return clazz;
    }
}
