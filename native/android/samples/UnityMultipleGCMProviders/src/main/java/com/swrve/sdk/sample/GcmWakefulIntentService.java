package com.swrve.sdk.sample;

import android.app.IntentService;
import android.content.Intent;

import com.swrve.sdk.SwrveLogger;
import com.swrve.unity.gcm.SwrveGcmIntentService;

public class GcmWakefulIntentService extends IntentService {

    public GcmWakefulIntentService() {
        super("GcmWakefulIntentService");
    }

    @Override
    protected void onHandleIntent(Intent intent) {
        try {
            new SwrveGcmIntentService().onMessageReceived(null, intent.getExtras());
        } catch (Exception ex) {
            SwrveLogger.e("SwrveGcmIntentService exception (intent: %s): ", ex, intent);
        } finally {
            GcmWakefulReceiver.completeWakefulIntent(intent);
        }
    }
}
