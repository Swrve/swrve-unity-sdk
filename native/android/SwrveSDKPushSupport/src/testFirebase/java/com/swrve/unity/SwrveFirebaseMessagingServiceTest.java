package com.swrve.unity;

import static org.junit.Assert.assertEquals;
import static org.mockito.Mockito.never;

import android.os.Bundle;

import com.google.firebase.messaging.RemoteMessage;
import com.swrve.sdk.SwrveNotificationConstants;
import com.swrve.unity.firebase.SwrveFirebaseMessagingService;

import org.junit.Test;
import org.mockito.ArgumentCaptor;
import org.mockito.Mockito;
import org.robolectric.Robolectric;

public class SwrveFirebaseMessagingServiceTest extends SwrveBaseTest {

    @Test
    public void testMessagingService() {
        SwrveFirebaseMessagingService service = Robolectric.setupService(SwrveFirebaseMessagingService.class);
        SwrveFirebaseMessagingService serviceSpy = Mockito.spy(service);
        serviceSpy.onCreate();

        SwrvePushManagerUnityImp mockSwrvePushManagerImp = Mockito.mock(SwrvePushManagerUnityImp.class);
        Mockito.doNothing().when(mockSwrvePushManagerImp).processMessage(Mockito.any(Bundle.class));
        Mockito.doReturn(mockSwrvePushManagerImp).when(serviceSpy).getSwrveUnityPushServiceManager();

        // Invalid bundle
        RemoteMessage.Builder invalidPushBundle = new RemoteMessage.Builder("dest");
        invalidPushBundle.addData("text", "hello");
        invalidPushBundle.addData("key1", "value1");
        serviceSpy.onMessageReceived(invalidPushBundle.build());
        ArgumentCaptor<Bundle> bundleCaptor = ArgumentCaptor.forClass(Bundle.class);
        Mockito.verify(mockSwrvePushManagerImp, never()).processMessage(bundleCaptor.capture());

        // valid bundle but without a sid
        RemoteMessage.Builder validPushBundleWithoutSid = new RemoteMessage.Builder("dest");
        validPushBundleWithoutSid.addData(SwrveNotificationConstants.TEXT_KEY, "validBundle");
        validPushBundleWithoutSid.addData(SwrveNotificationConstants.SWRVE_TRACKING_KEY, "1");
        serviceSpy.onMessageReceived(validPushBundleWithoutSid.build());
        bundleCaptor = ArgumentCaptor.forClass(Bundle.class);
        Mockito.verify(mockSwrvePushManagerImp, Mockito.atLeastOnce()).processMessage(bundleCaptor.capture());
        Bundle actualBundle = bundleCaptor.getAllValues().get(0);
        assertEquals(4, actualBundle.size());
        assertEquals("validBundle", actualBundle.getString("text"));
        assertEquals("1", actualBundle.getString("_p"));

        // valid bundle but with a sid
        RemoteMessage.Builder pushBundleWithSid = new RemoteMessage.Builder("dest");
        pushBundleWithSid.addData(SwrveNotificationConstants.TEXT_KEY, "validBundle");
        pushBundleWithSid.addData(SwrveNotificationConstants.SWRVE_TRACKING_KEY, "2");
        pushBundleWithSid.addData(SwrveNotificationConstants.SWRVE_UNIQUE_MESSAGE_ID_KEY, "2");
        pushBundleWithSid.setMessageId("123");
        serviceSpy.onMessageReceived(pushBundleWithSid.build());
        bundleCaptor = ArgumentCaptor.forClass(Bundle.class);
        Mockito.verify(mockSwrvePushManagerImp, Mockito.atLeastOnce()).processMessage(bundleCaptor.capture());
        assertEquals(2, bundleCaptor.getAllValues().size()); // the size increases to 2
        actualBundle = bundleCaptor.getAllValues().get(1); // second index
        assertEquals(5, actualBundle.size());
        assertEquals("validBundle", actualBundle.getString("text"));
        assertEquals("2", actualBundle.getString("_p"));
        assertEquals("123", actualBundle.getString("provider.message_id"));
        assertEquals("0", actualBundle.getString("provider.sent_time"));

        // use same bundle as last with the same sid
        serviceSpy.onMessageReceived(pushBundleWithSid.build());
        bundleCaptor = ArgumentCaptor.forClass(Bundle.class);
        Mockito.verify(mockSwrvePushManagerImp, Mockito.atLeastOnce()).processMessage(bundleCaptor.capture());
        assertEquals(2, bundleCaptor.getAllValues().size()); // the size stays at 2 meaning the duplicate was caught
    }
}
