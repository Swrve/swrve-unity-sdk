package com.swrve.sdk;

import android.os.Bundle;

import org.json.JSONException;
import org.json.JSONObject;

public class SwrveUnityPushNotification {

    private static final String SWRVE_PUSH_IDENTIFIER = "_p";

    public static class Builder {
        public static SwrveUnityPushNotification build(Bundle msg) {
            if (msg.containsKey(SWRVE_PUSH_IDENTIFIER)) {
                String id = msg.get(SWRVE_PUSH_IDENTIFIER).toString();
                return new SwrveUnityPushNotification(id, msg);
            }
            return null;
        }
    }

    private String id;
    private String jsonPayload;

    private SwrveUnityPushNotification(String id, Bundle msg) {
        this.id = id;

        try {
            // Only one level of serialization
            JSONObject json = new JSONObject();
            for(String key : msg.keySet()) {
                json.put(key, msg.get(key).toString());
            }
            this.jsonPayload = json.toString();
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }

    public String getId() {
        return id;
    }

    public String toJson() {
        return jsonPayload;
    }
}
