package com.swrve.unity;

import android.content.Context;
import android.content.SharedPreferences;

import com.swrve.unity.SwrvePushSupport;

// Class with variant defined methods to be used by SwrveUnityPushServiceManager
public class SwrvePushServiceManagerCommon {

    public static SharedPreferences getPreferences(Context context) {
        return context.getSharedPreferences(context.getPackageName() + "_swrve_adm_push", Context.MODE_PRIVATE);
    }

    public static String getGameObject(Context context) {
        final SharedPreferences prefs = getPreferences(context);
        return prefs.getString(SwrvePushSupport.PROPERTY_GAME_OBJECT_NAME, "SwrveComponent");
    }
}
