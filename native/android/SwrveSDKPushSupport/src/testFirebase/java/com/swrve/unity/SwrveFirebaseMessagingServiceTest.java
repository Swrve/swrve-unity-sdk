package com.swrve.unity;

import android.app.Activity;
import android.app.Service;
import android.os.Bundle;

import androidx.test.core.app.ApplicationProvider;

import com.google.firebase.messaging.RemoteMessage;
import com.swrve.sdk.SwrveNotificationConstants;
import com.swrve.sdk.SwrveUnityCommon;
import com.swrve.unity.firebase.MainActivity;
import com.swrve.unity.firebase.SwrveFirebaseMessagingService;

import org.junit.Before;
import org.junit.Test;
import org.mockito.ArgumentCaptor;
import org.mockito.Mockito;
import org.robolectric.Robolectric;
import org.robolectric.Shadows;
import org.robolectric.shadows.ShadowActivity;

import java.lang.reflect.Constructor;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNotNull;
import static org.mockito.Mockito.atLeastOnce;
import static org.mockito.Mockito.doReturn;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.spy;

public class SwrveFirebaseMessagingServiceTest extends SwrveBaseTest {

    protected Activity mActivity;
    protected ShadowActivity mShadowActivity;
    protected Service service;
    protected SwrveUnityPushServiceManager pushServiceManagerMock;

    @Before
    public void setUp() throws Exception {
        super.setUp();
        mActivity = Robolectric.buildActivity(MainActivity.class).create().visible().get();
        mShadowActivity = Shadows.shadowOf(mActivity);
        pushServiceManagerMock = mock(SwrveUnityPushServiceManager.class);
        service = Robolectric.setupService(SwrveFirebaseMessagingService.class);
    }

    @Test
    public void testMessagingService() throws Exception {

        new SwrveUnityCommon(ApplicationProvider.getApplicationContext());
        String msgText = "hello";
        Bundle bundle = new Bundle();
        bundle.putString("_p", "10");
        bundle.putString(SwrveNotificationConstants.TEXT_KEY, msgText);
        bundle.putString("custom", "key");

        Constructor<RemoteMessage> constructor = RemoteMessage.class.getDeclaredConstructor(Bundle.class);
        constructor.setAccessible(true);
        RemoteMessage message = constructor.newInstance(bundle);
        SwrveFirebaseMessagingService serviceSpy = spy((SwrveFirebaseMessagingService) service);
        doReturn(pushServiceManagerMock).when(serviceSpy).getSwrveUnityPushServiceManager();
        serviceSpy.onMessageReceived(message);

        ArgumentCaptor<Bundle> bundleCaptor = ArgumentCaptor.forClass(Bundle.class);
        Mockito.verify(pushServiceManagerMock, atLeastOnce()).processRemoteNotification(bundleCaptor.capture());
        Bundle capturedBundle = bundleCaptor.getAllValues().get(0);
        assertNotNull(capturedBundle);
        assertEquals(capturedBundle.size(), 3);
        assertEquals("10", capturedBundle.getString("_p"));
        assertEquals(msgText, capturedBundle.getString(SwrveNotificationConstants.TEXT_KEY));
        assertEquals("key", capturedBundle.getString("custom"));
    }
}
