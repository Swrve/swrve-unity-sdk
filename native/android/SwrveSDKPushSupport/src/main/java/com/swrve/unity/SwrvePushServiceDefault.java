package com.swrve.unity;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;

import com.swrve.sdk.SwrvePushWorkerHelper;

import java.util.Map;

public class SwrvePushServiceDefault {

    /**
     * Use this method to override Swrve's implementation of another push provider. See samples
     * directory in the public repository on how to use it.
     * @param context A context
     * @param data A map containing swrve properties used for silent push or rich notifications.
     * @return true if successfully scheduled, false otherwise
     */
    public static boolean handle(Context context, Map<String, String> data) {
        boolean handled = false;
        if (data != null) {
            SwrvePushWorkerHelper workerHelper = new SwrvePushWorkerHelper(context, SwrvePushManagerWorkerUnity.class, data);
            handled = workerHelper.handle();
        }
        return handled;
    }

    /**
     * Use this method to override Swrve's implementation of another push provider. See samples
     * directory in the public repository on how to use it.
     * @param context A context
     * @param intent An intent containing a bundle of swrve properties used for silent push or rich notifications.
     * @return true if successfully scheduled, false otherwise
     */
    public static boolean handle(Context context, Intent intent) {
        return handle(context, intent.getExtras());
    }

    /**
     * Use this method to process a Swrve rich notification. Requires Swrve push properties to be part
     * of the bundle.
     * @param context A context
     * @param extras A bundle containing Swrve properties used for silent push or rich notifications.
     * @return true if successfully scheduled, false otherwise
     */
    public static boolean handle(Context context, Bundle extras) {
        boolean handled = false;
        if (extras != null) {
            SwrvePushWorkerHelper workerHelper = new SwrvePushWorkerHelper(context, SwrvePushManagerWorkerUnity.class, extras);
            handled = workerHelper.handle();
        }
        return handled;
    }
}
