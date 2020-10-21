package com.swrve.sdk;

import android.app.IntentService;
import android.content.Intent;

public class SwrveUnityWakefulService extends IntentService {

    public SwrveUnityWakefulService() {
        super("SwrveUnityWakefulService");
    }

    @Override
    protected void onHandleIntent(Intent intent) {
        if (intent == null) {
            return;
        }
        try {
            SwrveUnityBackgroundEventSender sender = getBackgroundEventSender();
            sender.handleSendEvents(intent.getExtras());
        } catch (Exception e) {
            SwrveLogger.e("Unable to properly process Intent information", e);
        }
        finally {
            SwrveUnityWakefulReceiver.completeWakefulIntent(intent);
        }
    }

    protected SwrveUnityBackgroundEventSender getBackgroundEventSender() {
        return new SwrveUnityBackgroundEventSender();
    }
}
