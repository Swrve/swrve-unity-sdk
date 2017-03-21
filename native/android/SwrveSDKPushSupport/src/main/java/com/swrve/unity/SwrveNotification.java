package com.swrve.unity;

import org.json.JSONException;
import org.json.JSONObject;

import android.os.Bundle;

import com.swrve.sdk.SwrvePushSDK;

public class SwrveNotification {
	
	public static class Builder {
		public static SwrveNotification build(Bundle msg) {
			String id = SwrvePushSDK.getSwrveId(msg);
			if (id != null) {
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
