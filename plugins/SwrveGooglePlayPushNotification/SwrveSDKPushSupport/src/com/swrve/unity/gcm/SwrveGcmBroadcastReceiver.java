package com.swrve.unity.gcm;

import android.support.v4.content.WakefulBroadcastReceiver;
import android.app.Activity;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;

public class SwrveGcmBroadcastReceiver extends WakefulBroadcastReceiver {
	private static String workaroundRegistrationId;

    @Override
    public void onReceive(Context context, Intent intent) {
    	if (intent != null) {
    		final String registrationId = intent.getStringExtra("registration_id");
    		if (!isEmptyString(registrationId)) {
    			// We got a registration id!
    			workaroundRegistrationId = registrationId;
    		}

	        ComponentName comp = new ComponentName(context.getPackageName(), SwrveGcmIntentService.class.getName());
	        // Start the service, keeping the device awake while it is launching.
	        startWakefulService(context, (intent.setComponent(comp)));
	    }

        setResultCode(Activity.RESULT_OK);
    }

    private static boolean isEmptyString(String str) {
		return (str == null || str.equals(""));
	}

    public static String getWorkaroundRegistrationId() {
    	return workaroundRegistrationId;
    }

    public static void clearWorkaroundRegistrationId() {
        workaroundRegistrationId = null;
    }
}
