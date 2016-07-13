package com.swrve.sdk;

import android.content.Context;
import android.content.Intent;
import android.support.v4.content.WakefulBroadcastReceiver;

import java.util.ArrayList;

public class SwrveUnityWakefulReceiver extends WakefulBroadcastReceiver {

    private static final String LOG_TAG = "SwrveWakeful";

    @Override
    public void onReceive(Context context, Intent intent) {
        Intent service = new Intent(context, SwrveUnityWakefulService.class);
        if(intent.hasExtra(SwrveUnityWakefulService.EXTRA_EVENTS)) {
            ArrayList<String> events = intent.getExtras().getStringArrayList(SwrveUnityWakefulService.EXTRA_EVENTS);
            SwrveLogger.i(LOG_TAG, "SwrveUnityWakefulReceiver. Events: " + events);
            service.putExtras(intent);
        }
        startWakefulService(context, service);
    }
}
