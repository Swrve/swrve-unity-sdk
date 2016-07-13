package com.swrve.sdk;

import android.app.IntentService;
import android.content.Intent;

import com.swrve.sdk.rest.IRESTClient;
import com.swrve.sdk.rest.IRESTResponseListener;
import com.swrve.sdk.rest.RESTClient;
import com.swrve.sdk.rest.RESTResponse;

import org.json.JSONException;

import java.util.ArrayList;
import java.util.LinkedHashMap;

public class SwrveUnityWakefulService extends IntentService {

    private static final String LOG_TAG = "SwrveWakeful";
    public static final String EXTRA_EVENTS = "swrve_wakeful_events";

    private ISwrveCommon swrveCommon = SwrveCommon.getInstance();

    public SwrveUnityWakefulService() {
        super("SwrveUnityWakefulService");
    }

    @Override
    protected void onHandleIntent(Intent intent) {
        try {
            ArrayList<String> eventsExtras = intent.getExtras().getStringArrayList(EXTRA_EVENTS);
            if (eventsExtras != null && eventsExtras.size() > 0) {
                sendEvents(eventsExtras);
            } else {
                SwrveLogger.e(LOG_TAG, "SwrveUnityWakefulService: Unknown intent received.");
            }
        }
        catch (Exception e) {
            SwrveLogger.e(LOG_TAG, "Unable to properly process Intent information", e);
        }
        finally {
            SwrveUnityWakefulReceiver.completeWakefulIntent(intent);
        }
    }

    protected int sendEvents(ArrayList<String> events) {
        SwrveLogger.i(LOG_TAG, "Sending batch of events:" + events);
        int eventsSent = 0;
        try {
            LinkedHashMap<Long, String> eventsMap = new LinkedHashMap<>();
            for(int i = 0; i < events.size(); i++) {
                eventsMap.put((long)i, events.get(i));
            }
            IRESTClient restClient = createRESTClient();
            String postData = EventHelper.eventsAsBatch(eventsMap, swrveCommon.getUserId(), swrveCommon.getAppVersion(), swrveCommon.getSessionKey(), swrveCommon.getDeviceId());
            IPostBatchRequestListener pbrl = new IPostBatchRequestListener() {
                public void onResponse(boolean shouldDelete) {
                    SwrveLogger.d(LOG_TAG, "Background sendEventsAsBatch response, success:" + shouldDelete);
                }
            };

            postBatchRequest(restClient, postData, pbrl);
            eventsSent = events.size();
        } catch (JSONException je) {
            SwrveLogger.e(LOG_TAG, "Unable to generate event batch, and send events in background", je);
        }
        return eventsSent;
    }

    protected IRESTClient createRESTClient() {
        return new RESTClient(swrveCommon.getHttpTimeout());
    }

    private void postBatchRequest(IRESTClient restClient, final String postData, final IPostBatchRequestListener listener) {
        restClient.post(SwrveCommon.getInstance().getBatchURL(), postData, new IRESTResponseListener() {
            @Override
            public void onResponse(RESTResponse response) {
                boolean deleteEvents = true;
                if (SwrveHelper.userErrorResponseCode(response.responseCode)) {
                    SwrveLogger.e(LOG_TAG, "Error sending events to Swrve: " + response.responseBody);
                } else if (SwrveHelper.successResponseCode(response.responseCode)) {
                    SwrveLogger.i(LOG_TAG, "Events sent to Swrve");
                } else if (SwrveHelper.serverErrorResponseCode(response.responseCode)) {
                    deleteEvents = false;
                    SwrveLogger.e(LOG_TAG, "Error sending events to Swrve: " + response.responseBody);
                }

                // Resend if we got a server error (5XX)
                listener.onResponse(deleteEvents);
            }

            @Override
            public void onException(Exception ex) {
                SwrveLogger.e(LOG_TAG, "Error posting batch of events. postData:" + postData, ex);
            }
        });
    }
}
