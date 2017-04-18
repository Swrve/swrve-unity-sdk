package com.swrve.unity;

import android.app.Notification;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.preference.PreferenceManager;
import android.support.v4.app.NotificationCompat;

import com.swrve.unity.swrvesdkpushsupport.R;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;
import org.robolectric.shadows.ShadowNotification;

import static junit.framework.Assert.assertEquals;
import static org.robolectric.Shadows.shadowOf;

public class SwrvePushSupportTest extends SwrveBaseTest {

    @Before
    public void setUp() throws Exception {
        super.setUp();
    }

    @After
    public void tearDown() throws Exception {
        super.tearDown();
    }

    @Test
    public void testCreateNotificationBuilderWithPrefs() {

        String msgTitle = "My Awesome App Title";
        String msgText = "hello";
        String icon = "common_google_signin_btn_icon_dark";
        String materialIcon = "common_full_open_on_phone";
        int materialIconId = R.drawable.common_full_open_on_phone;
        int colorId = R.color.common_google_signin_btn_text_dark;

        SharedPreferences prefs = PreferenceManager.getDefaultSharedPreferences(mActivity);
        prefs.edit().putString(SwrvePushSupport.PROPERTY_APP_TITLE, msgTitle).commit();
        prefs.edit().putString(SwrvePushSupport.PROPERTY_ICON_ID, icon).commit();
        prefs.edit().putString(SwrvePushSupport.PROPERTY_MATERIAL_ICON_ID, materialIcon).commit();
        prefs.edit().putInt(SwrvePushSupport.PROPERTY_ACCENT_COLOR, colorId).commit();

        Bundle extras = new Bundle();
        extras.putString("sound", "default");
        NotificationCompat.Builder builder = SwrvePushSupport.createNotificationBuilder(mActivity, prefs, msgText, extras);
        assertNotification(builder, msgTitle, msgText, materialIconId, colorId, "content://settings/system/notification_sound");
    }

    @Test
    public void testCreateNotificationBuilderWithDefaults() {

        String msgTitle = "com.swrve.unity.swrvesdkpushsupport";
        String msgText = "hello";
        int iconId = 0;

        SharedPreferences prefs = PreferenceManager.getDefaultSharedPreferences(mActivity);
        prefs.edit().clear().commit();

        NotificationCompat.Builder builder = SwrvePushSupport.createNotificationBuilder(mActivity, prefs, msgText, new Bundle());
        assertNotification(builder, msgTitle, msgText, iconId, 0, null);
    }

    private void assertNotification(NotificationCompat.Builder builder, String title, String text, int icon, int colorId, String sound)  {
        Notification notification = builder.build();
        ShadowNotification shadowNotification = shadowOf(notification);
        assertEquals("Notification title is wrong", title, shadowNotification.getContentTitle());
        assertEquals("Notification content text is wrong", text, shadowNotification.getContentText());
        assertEquals("Notification icon is wrong", icon, notification.icon);
        assertEquals("Notification icon is wrong", colorId, builder.getColor());
        assertEquals("Notification sound is wrong",  sound, notification.sound == null ? null : notification.sound.toString());
    }
}
