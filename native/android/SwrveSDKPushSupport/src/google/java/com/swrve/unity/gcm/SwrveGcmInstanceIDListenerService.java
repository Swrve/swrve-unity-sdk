package com.swrve.unity.gcm;

import com.google.android.gms.iid.InstanceIDListenerService;

public class SwrveGcmInstanceIDListenerService extends InstanceIDListenerService {

    @Override
    public void onTokenRefresh() {
        SwrveGcmDeviceRegistration.onTokenRefreshed();
    }
}