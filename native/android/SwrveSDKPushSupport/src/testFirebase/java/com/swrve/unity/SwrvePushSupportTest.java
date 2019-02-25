package com.swrve.unity;

import android.os.Bundle;

import com.google.firebase.messaging.RemoteMessage;
import com.swrve.unity.firebase.MainActivity;
import com.swrve.unity.firebase.SwrveFirebaseMessagingService;

import org.junit.Before;
import org.robolectric.Robolectric;
import org.robolectric.Shadows;

import java.lang.reflect.Constructor;

import static junit.framework.Assert.fail;

public class SwrvePushSupportTest extends SwrveBasePushSupportTest {

    @Before
    public void setUp() throws Exception {
        super.setUp();
        mActivity = Robolectric.buildActivity(MainActivity.class).create().visible().get();
        mShadowActivity = Shadows.shadowOf(mActivity);
        service = Robolectric.setupService(SwrveFirebaseMessagingService.class);
    }

    @Override
    public void serviceOnMessageReceived(Bundle bundle) {
        try {
            Constructor<RemoteMessage> constructor = RemoteMessage.class.getDeclaredConstructor(Bundle.class);
            constructor.setAccessible(true);
            RemoteMessage message = constructor.newInstance(bundle);
            ((SwrveFirebaseMessagingService) service).onMessageReceived(message);
        } catch(Exception exp) {
            System.err.println(exp.toString());
            fail();
        }
    }
}
