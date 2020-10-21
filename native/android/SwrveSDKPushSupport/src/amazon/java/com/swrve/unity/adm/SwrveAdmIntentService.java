package com.swrve.unity.adm;

import android.content.Intent;

import com.amazon.device.messaging.ADMMessageHandlerBase;
import com.swrve.sdk.SwrveLogger;

// Used for old devices
public class SwrveAdmIntentService extends ADMMessageHandlerBase {
    public SwrveAdmIntentService() {
        super(SwrveAdmIntentService.class.getName());
    }

    public SwrveAdmIntentService(final String className) {
        super(className);
    }

    @Override
    protected void onMessage(final Intent intent) {
        getPushBase().onMessage(getApplicationContext(), intent);
    }

    @Override
    protected void onRegistrationError(final String string) {
        SwrveLogger.e("ADM Registration Error. Error string %s", string);
    }

    @Override
    protected void onRegistered(final String registrationId) {
        SwrveLogger.i("ADM Registered. RegistrationId: %s", registrationId);
        getPushBase().onRegistered(getApplicationContext(), registrationId);
    }

    @Override
    protected void onUnregistered(final String registrationId) {
        SwrveLogger.i("ADM Unregistered. RegistrationId: %s", registrationId);
    }

    protected SwrveAdmPushBase getPushBase() {
        return new SwrveAdmPushBase();
    }
}
