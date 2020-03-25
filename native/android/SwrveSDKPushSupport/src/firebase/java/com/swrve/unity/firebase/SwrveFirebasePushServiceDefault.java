package com.swrve.unity.firebase;

import android.content.Context;
import android.content.Intent;
import android.os.Build;
import android.os.Bundle;

import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrveLogger;
import com.swrve.swrvesdkcommon.R;

import java.util.Map;

public class SwrveFirebasePushServiceDefault {

    /**
     * Use this method to inform Swrve of a new token.
     * @param context A context
     * @param token The new token
     */
    public static void onNewToken(Context context, String token) {
        SwrveFirebasePushSupport.onNewToken(context, token);
    }
}
