package com.swrve.unity;

import android.content.Context;

import com.swrve.sdk.SwrvePushWorkerHelper;

import java.util.Map;

public class SwrvePushServiceDefault {

    /**
     * This method should be used when multiple push providers are integrated and Swrve's default
     * push implementation is not being used. See samples directory in the public repository on how
     * to use it.
     *
     * @param context   A context
     * @param data      A map containing swrve push payload. For firebase this is the remoteMessage.getData().
     * @param messageId For firebase this is the remoteMessage.getMessageId().
     * @param sentTime  For firebase this is the remoteMessage.getSentTime().
     * @return true if it was a swrve push, false if it was another push provider and should be handled by the caller.
     */
    public static boolean handle(Context context, Map<String, String> data, String messageId, long sentTime) {
        boolean handled = false;
        if (data != null) {
            data.put("provider.message_id", messageId);
            data.put("provider.sent_time", String.valueOf(sentTime));
            SwrvePushWorkerHelper workerHelper = new SwrvePushWorkerHelper(context, SwrvePushManagerWorkerUnity.class, data);
            handled = workerHelper.handle();
        }
        return handled;
    }
}
