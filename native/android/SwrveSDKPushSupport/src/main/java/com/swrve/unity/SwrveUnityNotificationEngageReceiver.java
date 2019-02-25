package com.swrve.unity;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;

import com.swrve.sdk.SwrveHelper;
import com.swrve.sdk.SwrveLogger;
import com.swrve.sdk.SwrveNotificationConstants;
import com.swrve.sdk.notifications.model.SwrveNotificationButton;
import static com.swrve.sdk.notifications.model.SwrveNotificationButton.ActionType.OPEN_CAMPAIGN;


public class SwrveUnityNotificationEngageReceiver extends com.swrve.sdk.SwrveNotificationEngageReceiver {

    private final static String PUSH_BUTTON_TO_CAMPAIGN_ID = "PUSH_BUTTON_TO_CAMPAIGN_ID";

    @Override
    public void onReceive(Context context, Intent intent) {
        try {
            super.onReceive(context, intent);
            informUnityLayer(context, intent);
        } catch (Exception ex) {
            SwrveLogger.e("SwrveUnityNotificationEngageReceiver. Error processing intent. Intent: %s", ex, intent.toString());
        }
    }

    private void informUnityLayer(Context context, Intent intent) {
        if (intent == null) {
            return;
        }
        Bundle extras = intent.getExtras();
        if (extras == null || extras.isEmpty()) {
            return;
        }
        Bundle pushBundle = extras.getBundle(SwrveNotificationConstants.PUSH_BUNDLE);
        if (pushBundle == null) {
            return;
        }
        Object rawId = pushBundle.get(SwrveNotificationConstants.SWRVE_TRACKING_KEY);
        String msgId = (rawId != null) ? rawId.toString() : null;
        if (SwrveHelper.isNullOrEmpty(msgId)) {
            return;
        }

        String contextId = extras.getString(SwrveNotificationConstants.CONTEXT_ID_KEY);
        if (SwrveHelper.isNotNullOrEmpty(contextId)) {
            SwrveNotificationButton.ActionType type = (SwrveNotificationButton.ActionType) extras.get(SwrveNotificationConstants.PUSH_ACTION_TYPE_KEY);
            if (type == OPEN_CAMPAIGN){
                pushBundle.putString(PUSH_BUTTON_TO_CAMPAIGN_ID, extras.getString(SwrveNotificationConstants.PUSH_ACTION_URL_KEY));
            }
        }

        // Inform the Unity native layer
        SwrveUnityNotification notification = SwrveUnityNotification.Builder.build(pushBundle);
        SwrvePushSupport.newOpenedNotification(SwrvePushServiceManagerCommon.getGameObject(context), notification);
    }
}