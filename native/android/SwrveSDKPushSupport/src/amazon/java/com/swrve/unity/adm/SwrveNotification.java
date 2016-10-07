package com.swrve.unity.adm;

import org.json.JSONException;
import org.json.JSONObject;

import android.os.Bundle;

public class SwrveNotification {

    public static class Builder {
        public static SwrveNotification build(Bundle msg) {
            if (msg.containsKey(SwrveAdmHelper.SWRVE_TRACKING_KEY)) {
                String id = msg.get(SwrveAdmHelper.SWRVE_TRACKING_KEY).toString();
                return new SwrveNotification(id, msg);
            }
            return null;
        }
    }

    private String id;
    private String jsonPayload;

    private SwrveNotification(String id, Bundle msg) {
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

