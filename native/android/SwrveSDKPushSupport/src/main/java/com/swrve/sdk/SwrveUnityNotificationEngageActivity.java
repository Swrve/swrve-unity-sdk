package com.swrve.sdk;

import android.app.Activity;
import android.os.Bundle;

import com.swrve.unity.SwrvePushSupport;

public class SwrveUnityNotificationEngageActivity extends Activity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        try {
            super.onCreate(savedInstanceState);
            new SwrveNotificationEngage(getApplicationContext()).processIntent(getIntent());
            SwrvePushSupport.newOpenedNotification(getApplicationContext(), getIntent());
            finish();
        } catch (Exception e) {
            SwrveLogger.e("SwrveUnityNotificationEngageActivity engage.processIntent", e);
        }
    }
}
