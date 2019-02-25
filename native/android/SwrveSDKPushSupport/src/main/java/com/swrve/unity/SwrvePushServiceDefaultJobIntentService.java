package com.swrve.unity;

import android.content.Intent;
import android.support.v4.app.JobIntentService;

import com.swrve.sdk.SwrveLogger;

public class SwrvePushServiceDefaultJobIntentService extends JobIntentService {

    @Override
    protected void onHandleWork(Intent intent) {
        try {
            new SwrveUnityPushServiceManager(this).processRemoteNotification(intent.getExtras());
        } catch (Exception ex) {
            SwrveLogger.e("SwrvePushServiceDefaultJobIntentService exception (intent: %s): ", ex, intent);
        }
    }
}
