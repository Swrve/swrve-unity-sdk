package com.swrve.sdk;

import android.os.Bundle;

import androidx.test.core.app.ApplicationProvider;

import com.swrve.unity.SwrveBaseTest;

import org.junit.Before;
import org.junit.Test;
import org.mockito.Mockito;

import static org.mockito.Mockito.atLeastOnce;
import static org.mockito.Mockito.doReturn;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;

public class SwrveUnityCommonHelperTest extends SwrveBaseTest {

    @Before
    public void setUp() throws Exception {
        super.setUp();
        ISwrveCommon swrveCommonSpy = mock(ISwrveCommon.class);
        Mockito.doReturn("some_app_version").when(swrveCommonSpy).getAppVersion();
        Mockito.doReturn("some_device_id").when(swrveCommonSpy).getDeviceId();
        Mockito.doReturn("some_session_key").when(swrveCommonSpy).getSessionKey();
        Mockito.doReturn("some_endpoint").when(swrveCommonSpy).getEventsServer();
        Mockito.doReturn(1).when(swrveCommonSpy).getNextSequenceNumber();
        SwrveCommon.setSwrveCommon(swrveCommonSpy);
    }

    @Test
    public void testSendPushDeliveredEvent() {

        Bundle pushBundle = new Bundle();
        pushBundle.putString(SwrveNotificationConstants.SWRVE_TRACKING_KEY, "123");
        SwrveUnityCommonHelper commonHelperSpy = Mockito.spy(new SwrveUnityCommonHelper());
        CampaignDeliveryManager campaignDeliveryManagerSpy = mock(CampaignDeliveryManager.class);
        doReturn(9876l).when(commonHelperSpy).getTime();
        doReturn(campaignDeliveryManagerSpy).when(commonHelperSpy).getCampaignDeliveryManager(ApplicationProvider.getApplicationContext());

        commonHelperSpy.sendPushDeliveredEvent(ApplicationProvider.getApplicationContext(), pushBundle);

        // @formatter:off
        String expectedJson =
                "{" +
                    "\"session_token\":\"some_session_key\"," +
                    "\"version\":\"3\"," +
                    "\"app_version\":\"some_app_version\"," +
                    "\"unique_device_id\":\"some_device_id\"," +
                    "\"data\":" +
                     "[" +
                        "{" +
                            "\"type\":\"generic_campaign_event\"," +
                            "\"time\":9876," +
                            "\"seqnum\":1," +
                            "\"actionType\":\"delivered\"," +
                            "\"campaignType\":\"push\"," +
                            "\"id\":\"123\"," +
                            "\"payload\":{" +
                                "\"displayed\":\"true\"," +
                                "\"silent\":\"false\"" +
                            "}" +
                        "}" +
                    "]" +
                "}";
        // @formatter:on

        verify(campaignDeliveryManagerSpy, atLeastOnce()).sendCampaignDelivery("some_endpoint/1/batch", expectedJson);
    }
}
