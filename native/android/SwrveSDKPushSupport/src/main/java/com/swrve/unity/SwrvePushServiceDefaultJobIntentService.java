package com.swrve.unity;

import android.content.Intent;
import androidx.core.app.SwrveJobIntentService;

import com.swrve.sdk.SwrveLogger;

public class SwrvePushServiceDefaultJobIntentService extends SwrveJobIntentService {

    @Override
    protected void onHandleWork(Intent intent) {
        try {
            new SwrveUnityPushServiceManager(this).processRemoteNotification(intent.getExtras());
        } catch (Exception ex) {
            SwrveLogger.e("SwrvePushServiceDefaultJobIntentService exception (intent: %s): ", ex, intent);
        }
    }
}
