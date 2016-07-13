package com.swrve.sdk;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;

import com.google.gson.Gson;
import com.google.gson.internal.LinkedTreeMap;
import com.google.gson.reflect.TypeToken;
import com.swrve.sdk.conversations.ui.ConversationActivity;
import com.unity3d.player.UnityPlayer;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.Closeable;
import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.lang.ref.WeakReference;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;

public class SwrveUnityCommon implements ISwrveCommon, ISwrveConversationSDK
{
    public final static String SWRVE_TEMPORARY_PATH_KEY = "swrveTemporaryPath";
    public final static String SDK_VERSION_KEY = "sdkVersion";
    public final static String PREFAB_NAME_KEY = "prefabName";
    public final static String DEVICE_INFO_KEY = "deviceInfo";
    public final static String API_KEY_KEY = "apiKey";
    public final static String DEVICE_ID_KEY = "deviceId";
    public final static String APP_ID_KEY = "appId";
    public final static String USER_ID_KEY = "userId";
    public final static String SWRVE_PATH_KEY = "swrvePath";
    public final static String LOC_TAG_KEY = "locTag";
    public final static String SIG_SUFFIX_KEY = "sigSuffix";
    public final static String APP_VERSION_KEY = "appVersion";
    public final static String UNIQUE_KEY_KEY = "uniqueKey";
    public final static String BATCH_URL_KEY = "batchUrl";
    public final static String EVENTS_SERVER_KEY = "eventsServer";
    public final static String HTTP_TIMEOUT_KEY = "httpTimeout";
    public final static String MAX_EVENTS_PER_FLUSH_KEY = "maxEventsPerFlush";

    public final static String EVENT_KEY = "event";
    public final static String NAME_KEY = "name";
    public final static String CONVERSATION_KEY = "conversation";
    public final static String PAGE_KEY = "page";

    public final static String SHARED_PREFERENCE_FILENAME = "swrve_unity_json_data";

    public final static String UNITY_SET_LOCATION_SEGMENT_VERSION = "SetLocationSegmentVersion";
    public final static String UNITY_USER_UPDATE = "UserUpdate";

    private final static String LOG_TAG = "UnitySwrveCommon";

    private Map<String, Object> currentDetails;
    protected WeakReference<Context> context;

    private String sessionKey;

    private File cacheDir;

    public static boolean isInitialised() {
        return null != SwrveCommon.getInstance();
    }

    /***
     * This is the automatically called Constructor from SwrveUnityApplication
     * Application class, if used.
     */
    public SwrveUnityCommon(Context context) {
        this(context, null);
    }

    /***
     * This is the most proper Constructor to call from the Unity layer,
     * via a native plugin, with a jsonString of settings, which will be
     * then cached locally.
     *
     * @param jsonString JSON String of configuration
     */
    public SwrveUnityCommon(String jsonString) {
        this(UnityPlayer.currentActivity, jsonString);
    }

    private SwrveUnityCommon(Context context, String jsonString) {
        SwrveCommon.setSwrveCommon(this);

        SwrveLogger.d(LOG_TAG, "UnitySwrveCommon constructor called with jsonString \"" + jsonString + "\"");

        if (context instanceof Activity) {
            this.context = new WeakReference<>(context.getApplicationContext());
        } else {
            this.context = new WeakReference<>(context);
        }

        SharedPreferences sp = this.context.get().getSharedPreferences(SHARED_PREFERENCE_FILENAME, Context.MODE_PRIVATE);
        if(null == jsonString) {
            try {
                jsonString = sp.getString(LOG_TAG, "");
            } catch (Exception e) {
                SwrveLogger.e(LOG_TAG, "Error loading Unity settings from shared prefs", e);
            }
        }

        if(null != jsonString) {
            try {
                Gson gson = new Gson();
                this.currentDetails = gson.fromJson(jsonString, new TypeToken<Map<String, Object>>(){}.getType());

                resetDeviceInfo();
                SharedPreferences.Editor editor = sp.edit();
                editor.putString(LOG_TAG, jsonString);
                editor.apply();

                this.cacheDir = new File(getSwrveTemporaryPath());
                sessionKey = SwrveHelper.generateSessionToken(this.getApiKey(), this.getAppId(), getUserId());
            } catch (Exception e) {
                SwrveLogger.e(LOG_TAG, "Error loading settings from JSON", e);
            }
        }
        else {
            SwrveLogger.d(LOG_TAG, "UnitySwrveCommon error no jsonString, nothing native will work correctly");
        }
    }

    private void resetDeviceInfo() {
        if(this.currentDetails.containsKey(DEVICE_INFO_KEY)) {
            LinkedTreeMap<String, Object> _deviceInfo = (LinkedTreeMap<String, Object>)this.currentDetails.get(DEVICE_INFO_KEY);
            try {
                JSONObject deviceInfo = new JSONObject("{}");
                for (Map.Entry<String, Object> entry: _deviceInfo.entrySet()) {
                    deviceInfo.put(entry.getKey(), entry.getValue());
                }
                this.currentDetails.remove(DEVICE_INFO_KEY);
                this.currentDetails.put(DEVICE_INFO_KEY, deviceInfo);
            }
            catch (JSONException ex) {
                SwrveLogger.e(LOG_TAG, "Error while creating device info json object", ex);
            }
        }
    }

    private String readFile(String dir, String filename) {
        String fileContent = "";

        String filePath = new File(dir, filename).getPath();
        String hmacFile = filePath + getSigSuffix();
        String fileSignature = readFile(hmacFile);

        try {
            if (SwrveHelper.isNullOrEmpty(fileSignature)) {
                throw new SecurityException("Signature validation failed, signature empty");
            }
            String _fileContent = readFile(filePath);

            String computedSignature = SwrveHelper.createHMACWithMD5(_fileContent, getUniqueKey());

            if (!fileSignature.trim().equals(computedSignature.trim())) {
                throw new SecurityException("Signature validation failed, signatures mismatch");
            }
            fileContent = _fileContent;

        } catch (NoSuchAlgorithmException e) {
            SwrveLogger.e(LOG_TAG, "Computing signature failed because of invalid algorithm", e);
        } catch (InvalidKeyException e) {
            SwrveLogger.e(LOG_TAG, "Computing signature failed because of an invalid key", e);
        }

        return fileContent;
    }

    private void tryCloseCloseable(Closeable closeable) {
        if(null != closeable) {
            try {
                closeable.close();
            } catch (IOException e) {
                SwrveLogger.e(LOG_TAG, "Error closing closable: " + closeable, e);
            }
        }
    }

    private String readFile(String filePath) {
        StringBuilder text = new StringBuilder();

        InputStream is = null;
        InputStreamReader isr = null;
        BufferedReader br = null;

        try {
            // open input stream test.txt for reading purpose.
            is = new FileInputStream(filePath);

            // create new input stream reader
            isr = new InputStreamReader(is);

            // create new buffered reader
            br = new BufferedReader(isr);

            int value;

            // reads to the end of the stream
            while((value = br.read()) != -1)
            {
                // prints character
                text.append((char)value);
            }
            SwrveLogger.d(LOG_TAG, "FileReader read file: " + filePath + ", content: " + text);

        } catch(Exception e) {
            e.printStackTrace();
        } finally {
            // releases resources associated with the streams
            tryCloseCloseable(is);
            tryCloseCloseable(isr);
            tryCloseCloseable(br);
        }

        return text.toString();
    }

    private String getStringDetail(String key) {
        if(currentDetails.containsKey(key)) {
            return (String)currentDetails.get(key);
        }
        return null;
    }

    private int getIntDetail(String key) {
        if(currentDetails.containsKey(key)) {
            return ((Double)currentDetails.get(key)).intValue();
        }
        return 0;
    }

    @Override
    public String getApiKey() {
        return getStringDetail(API_KEY_KEY);
    }

    @Override
    public String getSessionKey() {
        return this.sessionKey;
    }

    @Override
    public short getDeviceId() {
        if(currentDetails.containsKey(DEVICE_ID_KEY)) {
            return ((Double)currentDetails.get(DEVICE_ID_KEY)).shortValue();
        }
        return 0;
    }

    @Override
    public int getAppId() {
        return getIntDetail(APP_ID_KEY);
    }

    @Override
    public String getUserId() {
        return getStringDetail(USER_ID_KEY);
    }

    public String getSwrvePath() {
        return getStringDetail(SWRVE_PATH_KEY);
    }

    String getPrefabName() { return getStringDetail(PREFAB_NAME_KEY); }

    @Override
    public String getSwrveSDKVersion() {
        return getStringDetail(SDK_VERSION_KEY);
    }

    public String getSwrveTemporaryPath() {
        return getStringDetail(SWRVE_TEMPORARY_PATH_KEY);
    }

    public String getLocTag() {
        return getStringDetail(LOC_TAG_KEY);
    }

    public String getSigSuffix() {
        return getStringDetail(SIG_SUFFIX_KEY);
    }

    @Override
    public String getAppVersion() {
        return getStringDetail(APP_VERSION_KEY);
    }

    @Override
    public String getUniqueKey() {
        return getStringDetail(UNIQUE_KEY_KEY);
    }

    @Override
    public String getBatchURL() {
        return getEventsServer() + getStringDetail(BATCH_URL_KEY);
    }

    @Override
    public void setLocationSegmentVersion(int locationSegmentVersion) {
        sendMessageUp(UNITY_SET_LOCATION_SEGMENT_VERSION, Integer.toString(locationSegmentVersion));
    }

    @Override
    public void userUpdate(Map<String, String> attributes) {
        Gson gson = new Gson();
        sendMessageUp(UNITY_USER_UPDATE, gson.toJson(attributes));
    }

    private void sendMessageUp(String method, String msg)
    {
        UnityPlayer.UnitySendMessage(getPrefabName(), method, msg);
    }

    @Override
    public String getCachedData(String userId, String key) {
        if(LOCATION_CAMPAIGN_CATEGORY.equals(key))
        {
            return readFile(getSwrvePath(), getLocTag() + userId);
        }
        return null;
    }

    public void showConversation(String conversation) {
        try {
            Intent intent = new Intent(context.get(), ConversationActivity.class);
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            intent.putExtra(CONVERSATION_KEY, new SwrveBaseConversation(new JSONObject(conversation), cacheDir));
            context.get().startActivity(intent);
        }
        catch (JSONException exc) {
            SwrveLogger.e(LOG_TAG, "Could not JSONify conversation, conversation string didn't have the correct structure.");
        }
    }

    public int getConversationVersion() {
        return ISwrveConversationSDK.CONVERSATION_VERSION;
    }

    @Override
    public void sendEventsWakefully(Context context, ArrayList<String> events) {
        Intent intent = new Intent(context, SwrveUnityWakefulReceiver.class);
        intent.putStringArrayListExtra(SwrveUnityWakefulService.EXTRA_EVENTS, events);
        context.sendBroadcast(intent);
    }

    @Override
    public void queueConversationEvent(String eventParamName, String eventPayloadName, String page, int conversationId, Map<String, String> payload) {
        if (payload == null) {
            payload = new HashMap<>();
        }
        payload.put(EVENT_KEY, eventPayloadName);
        payload.put(CONVERSATION_KEY, Integer.toString(conversationId));
        payload.put(PAGE_KEY, page);

        SwrveLogger.d(LOG_TAG, "Sending view conversation event: " + eventParamName);

        Map<String, Object> parameters = new HashMap<>();
        parameters.put(NAME_KEY, eventParamName);

        ArrayList<String> conversationEvents = new ArrayList<>();
        try {
            conversationEvents.add(EventHelper.eventAsJSON(EVENT_KEY, parameters, null, null));
        } catch (JSONException e) {
            SwrveLogger.e(LOG_TAG, "Could not queue conversation events params: " + parameters, e);
        }
        sendEventsWakefully(context.get(), conversationEvents);
    }

    /***
     * Config area
     */

    @Override
    public String getEventsServer() {
        return getStringDetail(EVENTS_SERVER_KEY);
    }

    @Override
    public int getHttpTimeout() {
        return getIntDetail(HTTP_TIMEOUT_KEY);
    }

    @Override
    public int getMaxEventsPerFlush() {
        return getIntDetail(MAX_EVENTS_PER_FLUSH_KEY);
    }

    @Override
    public JSONObject getDeviceInfo() throws JSONException {
        if(currentDetails.containsKey(DEVICE_INFO_KEY)) {
            return (JSONObject)currentDetails.get(DEVICE_INFO_KEY);
        }
        return null;
    }

    /***
     * eo Config
     */
}
