package com.swrve.sdk;

import android.os.Bundle;

import com.swrve.sdk.rest.IRESTClient;
import com.swrve.sdk.rest.RESTClient;

import java.util.ArrayList;
import java.util.LinkedHashMap;

public class SwrveUnityBackgroundEventSender {
    public static final String EXTRA_EVENTS = "swrve_wakeful_events";

    private ISwrveCommon swrveCommon = SwrveCommon.getInstance();

    public int handleSendEvents(Bundle extras) throws Exception {
        ArrayList<String> eventsExtras = extras.getStringArrayList(EXTRA_EVENTS);
        if (eventsExtras != null && eventsExtras.size() > 0) {
            return handleSendEvents(eventsExtras);
        } else {
            SwrveLogger.e("SwrveUnityBackgroundEventSender: Unknown intent received (extras: %s).", extras);
        }
        return 0;
    }

    public int handleSendEvents(ArrayList<String> events) throws Exception {
        LinkedHashMap<Long, String> eventsMap = new LinkedHashMap<>();
        for(int i = 0; i < events.size(); i++) {
            eventsMap.put((long)i, events.get(i));
        }
        String postData = EventHelper.eventsAsBatch(eventsMap, swrveCommon.getUserId(), swrveCommon.getAppVersion(), swrveCommon.getSessionKey(), swrveCommon.getDeviceId());

        IRESTClient restClient = createRESTClient();
        restClient.post(SwrveCommon.getInstance().getBatchURL(), postData, null);

        return events.size();
    }

    protected IRESTClient createRESTClient() {
        return new RESTClient(swrveCommon.getHttpTimeout());
    }
}
