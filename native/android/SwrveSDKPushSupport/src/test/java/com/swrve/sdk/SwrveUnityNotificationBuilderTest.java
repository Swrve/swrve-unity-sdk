package com.swrve.sdk;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertTrue;
import static org.mockito.Mockito.spy;
import static org.robolectric.Shadows.shadowOf;

import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;

import androidx.test.core.app.ApplicationProvider;

import org.junit.Test;
import org.junit.runner.RunWith;
import org.robolectric.RobolectricTestRunner;
import org.robolectric.shadows.ShadowPendingIntent;

@RunWith(RobolectricTestRunner.class)
public class SwrveUnityNotificationBuilderTest extends com.swrve.unity.SwrveBaseTest {

    private SwrveNotificationConfig notificationConfig = new SwrveNotificationConfig.Builder(0, 0, null)
            .build();
    private Context context = ApplicationProvider.getApplicationContext();

    @Test
    public void testGetPendingIntent() {
        SwrveUnityNotificationBuilder builder = new SwrveUnityNotificationBuilder(ApplicationProvider.getApplicationContext(), notificationConfig);

        PendingIntent pendingIntent;
        ShadowPendingIntent shadowPendingIntent;
        Intent shadowIntent;

        // api level 30 --> use SwrveNotificationEngageReceiver
        Intent intentApi30 = new Intent(context, builder.getIntentClass(30, false));
        pendingIntent = builder.getPendingIntent(30, intentApi30, PendingIntent.FLAG_CANCEL_CURRENT, false);
        shadowPendingIntent = shadowOf(pendingIntent);
        assertTrue(shadowPendingIntent.isBroadcastIntent());
        shadowIntent = shadowPendingIntent.getSavedIntents()[0];
        assertEquals("com.swrve.sdk.SwrveUnityNotificationEngageReceiver", shadowIntent.getComponent().getClassName());

        // api level 31 - dismiss action --> use SwrveNotificationEngageReceiver
        Intent intentApi31Dismiss = new Intent(context, builder.getIntentClass(31, true));
        pendingIntent = builder.getPendingIntent(31, intentApi31Dismiss, PendingIntent.FLAG_CANCEL_CURRENT, true);
        shadowPendingIntent = shadowOf(pendingIntent);
        assertTrue(shadowPendingIntent.isBroadcastIntent());
        shadowIntent = shadowPendingIntent.getSavedIntents()[0];
        assertEquals("com.swrve.sdk.SwrveUnityNotificationEngageReceiver", shadowIntent.getComponent().getClassName());

        // api level 31 -  --> use SwrveNotificationEngageActivity
        Intent intentApi31 = new Intent(context, builder.getIntentClass(31, false));
        pendingIntent = builder.getPendingIntent(31, intentApi31, PendingIntent.FLAG_CANCEL_CURRENT, false);
        shadowPendingIntent = shadowOf(pendingIntent);
        assertTrue(shadowPendingIntent.isActivityIntent());
        shadowIntent = shadowPendingIntent.getSavedIntents()[0];
        assertEquals("com.swrve.sdk.SwrveUnityNotificationEngageActivity", shadowIntent.getComponent().getClassName());
    }

    @Test
    public void testGetIntentClass() {
        SwrveUnityNotificationBuilder builder = new SwrveUnityNotificationBuilder(ApplicationProvider.getApplicationContext(), notificationConfig);
        assertEquals("com.swrve.sdk.SwrveUnityNotificationEngageReceiver", builder.getIntentClass(30, false).getName());
        assertEquals("com.swrve.sdk.SwrveUnityNotificationEngageReceiver", builder.getIntentClass(30, true).getName());

        assertEquals("com.swrve.sdk.SwrveUnityNotificationEngageReceiver", builder.getIntentClass(31, true).getName());
        assertEquals("com.swrve.sdk.SwrveUnityNotificationEngageActivity", builder.getIntentClass(31, false).getName());
    }
}
