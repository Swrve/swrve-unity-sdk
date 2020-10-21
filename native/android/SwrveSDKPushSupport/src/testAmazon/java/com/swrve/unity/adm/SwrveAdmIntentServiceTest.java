package com.swrve.unity.adm;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;

import com.swrve.unity.SwrveBaseTest;

import org.junit.Before;
import org.junit.Test;
import org.robolectric.Robolectric;

import static org.mockito.Mockito.doReturn;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.spy;
import static org.mockito.Mockito.verify;

public class SwrveAdmIntentServiceTest extends SwrveBaseTest {

    protected Activity mActivity;
    private SwrveAdmIntentService serviceSpy;

    @Before
    public void setUp() throws Exception {
        super.setUp();
        serviceSpy = spy(new SwrveAdmIntentService());
        mActivity = Robolectric.buildActivity(MainActivity.class).create().visible().get();
        doReturn(mActivity).when(serviceSpy).getApplicationContext();
    }

    @Test
    public void testOnMessage() {

        Bundle bundle = new Bundle();
        bundle.putString("_s.t", "1000");
        Intent intent = new Intent();
        intent.putExtras(bundle);

        SwrveAdmPushBase admPushBaseMock = mock(SwrveAdmPushBase.class);
        doReturn(admPushBaseMock).when(serviceSpy).getPushBase();

        serviceSpy.onMessage(intent);

        verify(admPushBaseMock).onMessage(mActivity, intent);
    }
}
