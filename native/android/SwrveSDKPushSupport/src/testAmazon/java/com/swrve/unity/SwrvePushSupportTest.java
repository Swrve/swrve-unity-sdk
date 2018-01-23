package com.swrve.unity;

import android.content.Intent;
import android.os.Bundle;

import com.swrve.sdk.SwrvePushSDK;
import com.swrve.unity.adm.MainActivity;

import org.junit.After;
import org.junit.Before;
import org.robolectric.Robolectric;
import org.robolectric.Shadows;

public class SwrvePushSupportTest extends SwrveBasePushSupportTest {

    @Before
    public void setUp() throws Exception {
        super.setUp();

        mActivity = Robolectric.buildActivity(MainActivity.class).create().visible().get();
        mShadowActivity = Shadows.shadowOf(mActivity);
        service = Robolectric.setupService(TestSwrveAdmIntentService.class);
        SwrvePushSDK.createInstance(mActivity);
    }

    @After
    public void tearDown() throws Exception {
        super.tearDown();
    }

    @Override
    public void serviceOnMessageReceived(Bundle bundle) {
        Intent intent = new Intent();
        bundle.putString("_s.t", "1000");
        intent.putExtras(bundle);
        ((TestSwrveAdmIntentService)service).onMessage(intent);
    }

}
