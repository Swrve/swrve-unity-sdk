package com.swrve.sdk;

import android.Manifest;
import android.app.Activity;
import android.content.pm.PackageManager;
import android.support.v4.app.ActivityCompat;

import com.swrve.sdk.SwrvePlot;
import com.unity3d.player.UnityPlayer;

public class SwrveLocationUnityBridge
{
    static final int PERMISSIONS_RESULT = 1269;

    public static void StartPlot(Activity activity) {
        SwrvePlot.onCreate(activity);
    }

    public static void StartPlot() {
        StartPlot(UnityPlayer.currentActivity);
    }

    public static boolean StartPlotAfterPermissions(final Activity activity) {
        final String[] permissions = new String[]{Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION};

        if (ActivityCompat.checkSelfPermission(activity, permissions[0]) != PackageManager.PERMISSION_GRANTED ||
            ActivityCompat.checkSelfPermission(activity, permissions[1]) != PackageManager.PERMISSION_GRANTED) {
            activity.runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    ActivityCompat.requestPermissions(activity, permissions, PERMISSIONS_RESULT);
                }
            });
            return false;
        }
        else {
            StartPlot(activity);
            return true;
        }
    }

    public static boolean StartPlotAfterPermissions() {
        return StartPlotAfterPermissions(UnityPlayer.currentActivity);
    }
}
