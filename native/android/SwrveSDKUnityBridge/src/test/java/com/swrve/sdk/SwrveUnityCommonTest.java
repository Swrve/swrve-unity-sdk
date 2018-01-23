package com.swrve.sdk;

import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.os.Build;
import android.support.annotation.RequiresApi;

import org.junit.Test;
import org.robolectric.annotation.Config;

import static junit.framework.Assert.assertEquals;

public class SwrveUnityCommonTest extends SwrveBaseTest {

    @Config(sdk = Build.VERSION_CODES.O)
    @RequiresApi(api = Build.VERSION_CODES.O)
    @Test
    public void testDefaultNotificationChannel() throws NoSuchFieldException, IllegalAccessException {
        String channelId = "default-channel-id";
        String channelName = "default-channel-name";
        String channelImportance = "low";

        // Emulate call to register from the C# layer
        SwrveUnityCommon swrveCommon = new SwrveUnityCommon(shadowApplication.getApplicationContext());
        swrveCommon.setDefaultNotificationChannel(channelId, channelName, channelImportance);
        NotificationChannel notificationChannel = swrveCommon.getDefaultNotificationChannel();
        assertEquals(channelId, notificationChannel.getId());
        assertEquals(channelName, notificationChannel.getName());
        assertEquals(NotificationManager.IMPORTANCE_LOW, notificationChannel.getImportance());
    }
}
