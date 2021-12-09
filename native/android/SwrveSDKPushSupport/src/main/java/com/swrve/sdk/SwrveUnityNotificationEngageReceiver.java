package com.swrve.sdk;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;

import com.swrve.unity.SwrvePushSupport;

public class SwrveUnityNotificationEngageReceiver extends BroadcastReceiver {

    @Override
    public void onReceive(Context context, Intent intent) {
        try {
            new SwrveNotificationEngage(context).processIntent(intent);
            SwrvePushSupport.newOpenedNotification(context, intent);
        } catch (Exception ex) {
            SwrveLogger.e("SwrveUnityNotificationEngageReceiver. Error processing intent. Intent: %s", ex, intent.toString());
        }
    }
}
