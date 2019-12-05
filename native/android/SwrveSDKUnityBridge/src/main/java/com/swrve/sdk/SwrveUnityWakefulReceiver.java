package com.swrve.sdk;

import android.content.Context;
import android.content.Intent;
import androidx.legacy.content.WakefulBroadcastReceiver;

import java.util.ArrayList;

public class SwrveUnityWakefulReceiver extends WakefulBroadcastReceiver {

    @Override
    public void onReceive(Context context, Intent intent) {
        Intent service = new Intent(context, SwrveUnityWakefulService.class);
        if(intent.hasExtra(SwrveUnityBackgroundEventSender.EXTRA_EVENTS)) {
            ArrayList<String> events = intent.getExtras().getStringArrayList(SwrveUnityBackgroundEventSender.EXTRA_EVENTS);
            SwrveLogger.i("SwrveUnityWakefulReceiver. Events: %s", events);
            service.putExtras(intent);
        }
        startWakefulService(context, service);
    }
}
