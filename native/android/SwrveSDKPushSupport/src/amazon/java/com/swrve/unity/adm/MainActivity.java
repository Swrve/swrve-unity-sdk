package com.swrve.unity.adm;

import android.content.Intent;
import android.os.Bundle;

import com.unity3d.player.UnityPlayerActivity;

public class MainActivity extends UnityPlayerActivity {

    @Override
    protected void onCreate(Bundle arg0) {
        super.onCreate(arg0);
        processIntent(getIntent());
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        processIntent(intent);
    }

    public static void processIntent(Intent intent) {
        // Process intent that launched resumed activity
        SwrveAdmIntentService.processIntent(intent);
    }
}
