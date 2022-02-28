package com.swrve.unity;

import android.annotation.TargetApi;
import android.os.Build;
import android.util.Log;

import com.swrve.sdk.SwrveLogger;

import org.junit.After;
import org.junit.Before;
import org.junit.runner.RunWith;
import org.robolectric.RobolectricTestRunner;
import org.robolectric.RuntimeEnvironment;
import org.robolectric.Shadows;
import org.robolectric.annotation.Config;
import org.robolectric.shadows.ShadowApplication;
import org.robolectric.shadows.ShadowLog;

@RunWith(RobolectricTestRunner.class)
@Config(sdk = Build.VERSION_CODES.R)
@TargetApi(Build.VERSION_CODES.R)
public abstract class SwrveBaseTest {

    protected ShadowApplication shadowApplication;

    @Before
    public void setUp() throws Exception {
        SwrveLogger.setLogLevel(Log.VERBOSE);
        ShadowLog.stream = System.out;
        shadowApplication = Shadows.shadowOf(RuntimeEnvironment.application);
    }

    @After
    public void tearDown() throws Exception {
        // empty
    }
}
