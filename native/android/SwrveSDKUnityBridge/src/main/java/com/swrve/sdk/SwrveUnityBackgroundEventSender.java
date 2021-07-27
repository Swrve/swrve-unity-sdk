package com.swrve.sdk;

import android.content.Context;

import androidx.work.Constraints;
import androidx.work.Data;
import androidx.work.NetworkType;
import androidx.work.OneTimeWorkRequest;
import androidx.work.WorkManager;

import com.swrve.sdk.rest.IRESTClient;
import com.swrve.sdk.rest.RESTClient;

import java.util.Arrays;
import java.util.LinkedHashMap;
import java.util.List;

public class SwrveUnityBackgroundEventSender {

    private ISwrveCommon swrveCommon = SwrveCommon.getInstance();

    protected static final String DATA_KEY_USER_ID = "userId";
    protected static final String DATA_KEY_EVENTS = "events";

    private final Context context;
    private String userId;

    protected OneTimeWorkRequest workRequest; // exposed for testing

    public SwrveUnityBackgroundEventSender(final Context context) {
        this.context = context;
    }

    protected void send(String userId, List<String> events) {
        try {
            workRequest = getOneTimeWorkRequest(userId, events);
            enqueueWorkRequest(workRequest);
        } catch (Exception ex) {
            SwrveLogger.e("SwrveSDK: Error trying to queue events to be sent in the background worker.", ex);
        }
    }

    protected OneTimeWorkRequest getOneTimeWorkRequest(String userId, List<String> events) {
        Constraints constraints = new Constraints.Builder()
                .setRequiredNetworkType(NetworkType.CONNECTED)
                .build();
        Data inputData = new Data.Builder()
                .putString(DATA_KEY_USER_ID, userId)
                .putStringArray(DATA_KEY_EVENTS, events.toArray(new String[events.size()]))
                .build();
        OneTimeWorkRequest workRequest = new OneTimeWorkRequest.Builder(SwrveUnityBackgroundEventSenderWorker.class)
                .setConstraints(constraints)
                .setInputData(inputData)

                .build();
        return workRequest;
    }

    // separate method for testing
    protected synchronized void enqueueWorkRequest(OneTimeWorkRequest workRequest) {
        WorkManager.getInstance(context).enqueue(workRequest);
    }

    protected int handleSendEvents(Data data) throws Exception {

        userId = data.getString(DATA_KEY_USER_ID);
        String[] events = data.getStringArray(DATA_KEY_EVENTS);

        int eventsSent = 0;
        if (events != null && events.length > 0) {
            eventsSent = handleSendEvents(Arrays.asList(events));
        }

        return eventsSent;
    }

    public int handleSendEvents(List<String> events) throws Exception {
        LinkedHashMap<Long, String> eventsMap = new LinkedHashMap<>();
        for (int i = 0; i < events.size(); i++) {
            eventsMap.put((long) i, events.get(i));
        }
        String postData = EventHelper.eventsAsBatch(eventsMap, userId, swrveCommon.getAppVersion(), swrveCommon.getSessionKey(), swrveCommon.getDeviceId());

        IRESTClient restClient = getRestClient();
        restClient.post(SwrveCommon.getInstance().getBatchURL(), postData, null);

        return events.size();
    }

    protected IRESTClient getRestClient() {
        return new RESTClient(swrveCommon.getHttpTimeout());
    }
}
