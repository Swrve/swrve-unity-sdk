package com.swrve.sdk;

import androidx.work.Data;
import androidx.work.NetworkType;
import androidx.work.OneTimeWorkRequest;
import androidx.work.impl.model.WorkSpec;

import com.google.common.collect.Lists;
import com.swrve.sdk.rest.IRESTClient;

import org.junit.Before;
import org.junit.Test;

import java.util.ArrayList;

import static com.swrve.sdk.SwrveUnityBackgroundEventSender.DATA_KEY_EVENTS;
import static com.swrve.sdk.SwrveUnityBackgroundEventSender.DATA_KEY_USER_ID;
import static org.junit.Assert.assertArrayEquals;
import static org.junit.Assert.assertEquals;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.atLeastOnce;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.doReturn;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.spy;
import static org.mockito.Mockito.verify;

public class SwrveUnityBackgroundEventSenderTest extends SwrveBaseTest {

    @Before
    public void setUp() {
        super.setUp();
        ISwrveCommon swrveCommonSpy = mock(ISwrveCommon.class);
        doReturn("some_app_version").when(swrveCommonSpy).getAppVersion();
        doReturn("some_device_id").when(swrveCommonSpy).getDeviceId();
        doReturn("some_session_key").when(swrveCommonSpy).getSessionKey();
        SwrveCommon.setSwrveCommon(swrveCommonSpy);
    }

    @Test
    public void testGetOneTimeWorkRequest() {

        SwrveUnityBackgroundEventSender backgroundEventSenderSpy = spy(new SwrveUnityBackgroundEventSender(mActivity));
        ArrayList<String> events = Lists.newArrayList("event1", "event2");
        OneTimeWorkRequest workRequest = backgroundEventSenderSpy.getOneTimeWorkRequest("userId", events);

        WorkSpec workSpec = workRequest.getWorkSpec();
        assertEquals(NetworkType.CONNECTED, workSpec.constraints.getRequiredNetworkType());
        assertEquals("userId", workSpec.input.getString(DATA_KEY_USER_ID));
        assertArrayEquals(new String[]{"event1", "event2"}, workSpec.input.getStringArray(DATA_KEY_EVENTS));
    }

    @Test
    public void testSend() {

        SwrveUnityBackgroundEventSender backgroundEventSenderSpy = spy(new SwrveUnityBackgroundEventSender(mActivity));
        doNothing().when(backgroundEventSenderSpy).enqueueWorkRequest(any(OneTimeWorkRequest.class));

        ArrayList<String> events = Lists.newArrayList("event1", "event2");
        backgroundEventSenderSpy.send("userId", events);

        verify(backgroundEventSenderSpy, atLeastOnce()).getOneTimeWorkRequest("userId", events);
        verify(backgroundEventSenderSpy, atLeastOnce()).enqueueWorkRequest(any(OneTimeWorkRequest.class));
    }

    @Test
    public void testWithInvalidData() throws Exception {
        SwrveUnityBackgroundEventSender sender = new SwrveUnityBackgroundEventSender(mActivity);

        Data data = new Data.Builder()
                .putString(DATA_KEY_USER_ID, "userId")
                .putStringArray(DATA_KEY_EVENTS, null) // null array
                .build();
        int eventsSent = sender.handleSendEvents(data);
        assertEquals(0, eventsSent);

        data = new Data.Builder()
                .putString(DATA_KEY_USER_ID, "userId")
                .putStringArray(DATA_KEY_EVENTS, new String[]{}) // empty array
                .build();
        eventsSent = sender.handleSendEvents(data);
        assertEquals(0, eventsSent);
    }

    @Test
    public void testWithValidData() throws Exception {
        SwrveUnityBackgroundEventSender backgroundEventSenderSpy = spy(new SwrveUnityBackgroundEventSender(mActivity));
        IRESTClient mockRestClient = mock(IRESTClient.class);
        doReturn(mockRestClient).when(backgroundEventSenderSpy).getRestClient();

        ArrayList<String> events = new ArrayList<>();
        events.add("{\"my_awesome_event1\":\"1\"}");
        events.add("{\"my_awesome_event2\":\"2\"}");
        events.add("{\"my_awesome_event3\":\"3\"}");
        Data data = new Data.Builder()
                .putString(DATA_KEY_USER_ID, "userId")
                .putStringArray(DATA_KEY_EVENTS, events.toArray(new String[events.size()]))
                .build();
        int eventsSent = backgroundEventSenderSpy.handleSendEvents(data);
        assertEquals(3, eventsSent);
    }
}
