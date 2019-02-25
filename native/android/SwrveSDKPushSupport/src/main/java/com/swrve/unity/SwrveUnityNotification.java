package com.swrve.unity;

import android.os.Bundle;

import com.swrve.sdk.SwrveHelper;

import org.json.JSONException;
import org.json.JSONObject;

public class SwrveUnityNotification {
	
	public static class Builder {
		public static SwrveUnityNotification build(Bundle msg) {
			String id = SwrveHelper.getRemotePushId(msg);
			if (SwrveHelper.isNullOrEmpty(id)) {
				id = SwrveHelper.getSilentPushId(msg);
			}
			SwrveUnityNotification unityNotification = null;
			if (SwrveHelper.isNotNullOrEmpty(id)) {
				unityNotification = new SwrveUnityNotification(id, msg);
			}
			return unityNotification;
		}
	}
	
	private String id;
	private String jsonPayload;
	
	private SwrveUnityNotification(String id, Bundle msg) {
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
