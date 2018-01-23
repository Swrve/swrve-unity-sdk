package com.swrve.sdk;

import android.app.IntentService;
import android.content.Intent;

import com.swrve.sdk.rest.IRESTClient;
import com.swrve.sdk.rest.RESTClient;

import org.json.JSONException;

import java.util.ArrayList;
import java.util.LinkedHashMap;

public class SwrveUnityWakefulService extends IntentService {

    public SwrveUnityWakefulService() {
        super("SwrveUnityWakefulService");
    }

    @Override
    protected void onHandleIntent(Intent intent) {
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
