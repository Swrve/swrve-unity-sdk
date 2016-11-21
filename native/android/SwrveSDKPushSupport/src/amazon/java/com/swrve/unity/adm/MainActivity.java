package com.swrve.unity.adm;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import com.unity3d.player.UnityPlayerActivity;

public class MainActivity extends UnityPlayerActivity {

    private static Intent lastIntent;

    @Override
    protected void onCreate(Bundle arg0) {
        super.onCreate(arg0);
        processIntent(getApplicationContext(), getIntent());
    }

    @Override
    protected void onResume() {
        super.onResume();
        processIntent(getApplicationContext(), getIntent());
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        processIntent(getApplicationContext(), intent);
    }

    public static void processIntent(Context context, Intent intent) {
        if (lastIntent != intent) {
            // Process intent that launched resumed activity
            SwrveAdmPushSupport.processIntent(context, intent);
            lastIntent = intent;
        }
    }
}

