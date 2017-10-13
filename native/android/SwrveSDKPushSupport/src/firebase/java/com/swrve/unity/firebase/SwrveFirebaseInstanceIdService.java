package com.swrve.unity.firebase;

import com.google.firebase.iid.FirebaseInstanceIdService;

public class SwrveFirebaseInstanceIdService extends FirebaseInstanceIdService {

    @Override
    public void onTokenRefresh() {
        SwrveFirebaseDeviceRegistration.onTokenRefresh();
    }
}