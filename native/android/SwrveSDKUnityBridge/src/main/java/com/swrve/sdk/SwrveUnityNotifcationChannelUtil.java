package com.swrve.sdk;

import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.content.Context;
import android.content.SharedPreferences;
import android.os.Build;
import androidx.annotation.RequiresApi;

import java.lang.ref.WeakReference;

public class SwrveUnityNotifcationChannelUtil {

    protected static final String PREF_NAME = "swrve_default_notification_channel";
    protected static final String PREF_CHANNEL_ID = "channel_id";
    protected static final String PREF_CHANNEL_NAME = "channel_name";
    protected static final String PREF_CHANNEL_IMPORTANCE = "channel_importance";
    private final WeakReference<Context> context;

    public SwrveUnityNotifcationChannelUtil(WeakReference<Context> context) {
        this.context = context;
    }

    public void setDefaultNotificationChannel(String id, String name, String importance) {
        if (context.get() == null) {
            return;
        }

        SwrveLogger.v("Setting default Notification Channel details with [id:%s], [name:%s], [importance:%s]", id, name, importance);
        SharedPreferences preferences = context.get().getSharedPreferences(PREF_NAME, Context.MODE_PRIVATE);
        SharedPreferences.Editor editor = preferences.edit();
        editor.putString(PREF_CHANNEL_ID, id);
        editor.putString(PREF_CHANNEL_NAME, name);
        editor.putString(PREF_CHANNEL_IMPORTANCE, importance);
        editor.apply();
    }

    @RequiresApi(api = Build.VERSION_CODES.O)
    public NotificationChannel getDefaultNotificationChannel() {
        if (context.get() == null) {
            return null;
        }

        NotificationChannel channel = null;
        // Create the NotificationChannel (only on API 26+, channel might be null)
        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.O) {
            SharedPreferences preferences = context.get().getSharedPreferences(PREF_NAME, Context.MODE_PRIVATE);
            String id = preferences.getString(PREF_CHANNEL_ID, "swrve_default");
            String name = preferences.getString(PREF_CHANNEL_NAME, "Default");
            int importance = parseImportance(preferences.getString(PREF_CHANNEL_IMPORTANCE, "default"));
            SwrveLogger.v("Creating default Notification Channel with [id:%s], [name:%s], [importance:%s]", id, name, importance);
            channel = new NotificationChannel(id, name, importance);
        }

        return channel;
    }

    private int parseImportance(String notificationImportance) {
        int importance;
        switch (notificationImportance) {
            case "default":
                importance = NotificationManager.IMPORTANCE_DEFAULT;
                break;
            case "high":
                importance = NotificationManager.IMPORTANCE_HIGH;
                break;
            case "low":
                importance = NotificationManager.IMPORTANCE_LOW;
                break;
            case "max":
                importance = NotificationManager.IMPORTANCE_MAX;
                break;
            case "min":
                importance = NotificationManager.IMPORTANCE_MIN;
                break;
            case "none":
                importance = NotificationManager.IMPORTANCE_NONE;
                break;
            default:
                importance = NotificationManager.IMPORTANCE_DEFAULT;
                break;
        }
        return importance;
    }

}
