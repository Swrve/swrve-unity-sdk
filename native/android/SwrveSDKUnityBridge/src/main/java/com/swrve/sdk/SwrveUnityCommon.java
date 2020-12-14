package com.swrve.sdk;

import android.app.Activity;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Build;

import androidx.annotation.RequiresApi;

import com.google.gson.Gson;
import com.google.gson.internal.LinkedTreeMap;
import com.google.gson.reflect.TypeToken;
import com.swrve.sdk.conversations.ui.ConversationActivity;
import com.swrve.sdk.messaging.SwrveOrientation;
import com.unity3d.player.UnityPlayer;

import org.json.JSONArray;
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
import java.util.List;
import java.util.Map;
import java.util.Set;

public class SwrveUnityCommon implements ISwrveCommon, ISwrveConversationSDK {
    public final static String SWRVE_TEMPORARY_PATH_KEY = "swrveTemporaryPath";
    public final static String SDK_VERSION_KEY = "sdkVersion";
    public final static String PREFAB_NAME_KEY = "prefabName";
    public final static String DEVICE_INFO_KEY = "deviceInfo";
    public final static String API_KEY_KEY = "apiKey";
    public final static String DEVICE_ID_KEY = "deviceId";
    public final static String APP_ID_KEY = "appId";
    public final static String USER_ID_KEY = "userId";
    public final static String SWRVE_PATH_KEY = "swrvePath";
    public final static String QAUSER_KEY = "swrve.q2"; // note this
    public final static String SIG_SUFFIX_KEY = "sigSuffix";
    public final static String APP_VERSION_KEY = "appVersion";
    public final static String UNIQUE_KEY_KEY = "uniqueKey";
    public final static String BATCH_URL_KEY = "batchUrl";
    public final static String EVENTS_SERVER_KEY = "eventsServer";
    public final static String CONTENT_SERVER_KEY = "contentServer";
    public final static String HTTP_TIMEOUT_KEY = "httpTimeout";
    public final static int MAX_CACHED_AUTHENTICATED_NOTIFICATIONS = 10;

    public final static String EVENT_KEY = "event";
    public final static String NAME_KEY = "name";
    public final static String CONVERSATION_KEY = "conversation";
    public final static String PAGE_KEY = "page";

    public final static String SHARED_PREFERENCE_FILENAME = "swrve_unity_json_data";
    public final static String NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY = "notifications_id_cache_list";

    public final static String UNITY_USER_UPDATE = "UserUpdate";

    private final static String LOG_TAG = "UnitySwrveCommon";

    private Map<String, Object> currentDetails;
    protected WeakReference<Context> context;

    private String sessionKey;

    private File cacheDir;

    private final SwrveUnityNotifcationChannelUtil notificationChannelUtil;

    private static Object readyNativeCallbacksLock = new Object();
    private static boolean readyToDoNativeCalls = false;
    private static List<SwrveNativeCall> nativeCalls = new ArrayList<SwrveNativeCall>();

    /***
     * Call this method from the Unity Application class or your custom Application
     * class.
     */
    public static void onCreate(final Context context) {
        SwrveCommon.setRunnable(new Runnable() {
            @Override
            public void run() {
                new SwrveUnityCommon(context);
            }
        });
    }

    public SwrveUnityCommon(Context context) {
        this(context, null);
    }

    /***
     * This is the most proper Constructor to call from the Unity layer, via a
     * native plugin, with a jsonString of settings, which will be then cached
     * locally.
     *
     * @param jsonString JSON String of configuration
     */
    public SwrveUnityCommon(String jsonString) {
        this(UnityPlayer.currentActivity, jsonString);
    }

    private SwrveUnityCommon(Context context, String jsonString) {
        SwrveCommon.setSwrveCommon(this);
        if (context instanceof Activity) {
            this.context = new WeakReference<>(context.getApplicationContext());
        } else {
            this.context = new WeakReference<>(context);
        }

        SharedPreferences sp = this.context.get().getSharedPreferences(SHARED_PREFERENCE_FILENAME,
                Context.MODE_PRIVATE);
        if (null == jsonString) {
            try {
                jsonString = sp.getString(LOG_TAG, "");
            } catch (Exception e) {
                SwrveLogger.e("Error loading Unity settings from shared prefs", e);
            }
        }

        SwrveLogger.d("UnitySwrveCommon constructor called");

        if (null != jsonString) {
            try {
                Gson gson = new Gson();
                this.currentDetails = gson.fromJson(jsonString, new TypeToken<Map<String, Object>>() {
                }.getType());

                resetDeviceInfo();
                SharedPreferences.Editor editor = sp.edit();
                editor.putString(LOG_TAG, jsonString);
                editor.apply();

                String tempPath = getSwrveTemporaryPath();
                if (tempPath != null) {
                    this.cacheDir = new File(tempPath);
                }
                sessionKey = SwrveHelper.generateSessionToken(this.getApiKey(), this.getAppId(), getUserId());
            } catch (Exception e) {
                SwrveLogger.e("Error loading settings from JSON", e);
            }
        } else {
            SwrveLogger.d("UnitySwrveCommon error no jsonString, nothing native will work correctly");
        }

        notificationChannelUtil = new SwrveUnityNotifcationChannelUtil(this.context);
    }

    @CalledByUnity
    public static void ready() {
        readyToDoNativeCalls();
    }

    private void resetDeviceInfo() {
        if (this.currentDetails != null && this.currentDetails.containsKey(DEVICE_INFO_KEY)) {
            LinkedTreeMap<String, Object> _deviceInfo = (LinkedTreeMap<String, Object>) this.currentDetails
                    .get(DEVICE_INFO_KEY);
            try {
                JSONObject deviceInfo = new JSONObject("{}");
                for (Map.Entry<String, Object> entry : _deviceInfo.entrySet()) {
                    deviceInfo.put(entry.getKey(), entry.getValue());
                }
                this.currentDetails.remove(DEVICE_INFO_KEY);
                this.currentDetails.put(DEVICE_INFO_KEY, deviceInfo);
            } catch (JSONException ex) {
                SwrveLogger.e("Error while creating device info json object", ex);
            }
        }
    }

    private String readFile(String userId, String dir, String filename) {
        String fileContent = "";

        String filePath = new File(dir, filename).getPath();
        String hmacFile = filePath + getSigSuffix();
        String fileSignature = readFile(hmacFile);

        try {
            if (SwrveHelper.isNullOrEmpty(fileSignature)) {
                throw new SecurityException("Signature validation failed, signature empty");
            }
            String _fileContent = readFile(filePath);
            String computedSignature = SwrveHelper.createHMACWithMD5(_fileContent, getUniqueKey(userId));

            if (!fileSignature.trim().equals(computedSignature.trim())) {
                throw new SecurityException("Signature validation failed, signatures mismatch");
            }
            fileContent = _fileContent;

        } catch (NoSuchAlgorithmException e) {
            SwrveLogger.e("Computing signature failed because of invalid algorithm", e);
        } catch (InvalidKeyException e) {
            SwrveLogger.e("Computing signature failed because of an invalid key", e);
        }

        return fileContent;
    }

    private void tryCloseCloseable(Closeable closeable) {
        if (null != closeable) {
            try {
                closeable.close();
            } catch (IOException e) {
                SwrveLogger.e("Error closing closable: " + closeable, e);
            }
        }
    }

    private String readFile(String filePath) {
        StringBuilder text = new StringBuilder();

        InputStream is = null;
        InputStreamReader isr = null;
        BufferedReader br = null;

        try {
            // Open input stream "FilePath" for reading purpose.
            is = new FileInputStream(filePath);

            // create new input stream reader
            isr = new InputStreamReader(is);

            // create new buffered reader
            br = new BufferedReader(isr);

            int value;

            // reads to the end of the stream
            while ((value = br.read()) != -1) {
                // prints character
                text.append((char) value);
            }
            SwrveLogger.d("FileReader read file: %s, content: %s", filePath, text);
        } catch (Exception e) {
            SwrveLogger.e("Error reading file:" + filePath, e);
        } finally {
            // releases resources associated with the streams
            tryCloseCloseable(is);
            tryCloseCloseable(isr);
            tryCloseCloseable(br);
        }

        return text.toString();
    }

    private String getStringDetail(String key) {
        if (currentDetails != null && currentDetails.containsKey(key)) {
            return (String) currentDetails.get(key);
        }
        return null;
    }

    private int getIntDetail(String key) {
        if (currentDetails != null && currentDetails.containsKey(key)) {
            return ((Double) currentDetails.get(key)).intValue();
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
    public String getDeviceId() {
        if (currentDetails != null && currentDetails.containsKey(DEVICE_ID_KEY)) {
            return getStringDetail(DEVICE_ID_KEY);
        }
        return "";
    }

    @Override
    public int getAppId() {
        return getIntDetail(APP_ID_KEY);
    }

    @Override
    public String getUserId() {
        return getStringDetail(USER_ID_KEY);
    }

    @CalledByUnity
    public void setUserId(final String userId) {
        try {
            if (SwrveHelper.isNotNullOrEmpty(userId)) {
                SwrveLogger.d("setUserId called: User will change from: %s, To: %s", getStringDetail(USER_ID_KEY),
                        userId);
                this.currentDetails.put(USER_ID_KEY, userId);
                // Session token need to update because we changed UserId.
                sessionKey = SwrveHelper.generateSessionToken(this.getApiKey(), this.getAppId(), userId);
            }
        } catch (Exception ex) {
            SwrveLogger.e("Exception trying to update SwrveUserId", ex);
        }
    }

    public String getSwrvePath() {
        return getStringDetail(SWRVE_PATH_KEY);
    }

    String getPrefabName() {
        return getStringDetail(PREFAB_NAME_KEY);
    }

    @Override
    public String getSwrveSDKVersion() {
        return getStringDetail(SDK_VERSION_KEY);
    }

    public String getSwrveTemporaryPath() {
        return getStringDetail(SWRVE_TEMPORARY_PATH_KEY);
    }

    public String getSigSuffix() {
        return getStringDetail(SIG_SUFFIX_KEY);
    }

    @Override
    public String getAppVersion() {
        return getStringDetail(APP_VERSION_KEY);
    }

    @Override
    public String getUniqueKey(String userId) {
        return getStringDetail(UNIQUE_KEY_KEY);
    }

    @Override
    public String getBatchURL() {
        return getEventsServer() + getStringDetail(BATCH_URL_KEY);
    }

    @Override
    public String getContentURL() {
        return getStringDetail(CONTENT_SERVER_KEY);
    }

    @Override
    public void userUpdate(Map<String, String> attributes) {
        Gson gson = new Gson();
        SwrveUnityCommon.UnitySendMessage(getPrefabName(), UNITY_USER_UPDATE, gson.toJson(attributes));
    }

    @Override
    public void sendQueuedEvents() {
        // no operation, events will be send when the game is focused again
    }

    @Override
    public String getCachedData(String userId, String key) {
        String cacheData = null;
        switch (key) {
        case CACHE_QA:
            String fileName = QAUSER_KEY + userId;
            String swrvePath = getSwrvePath();
            if (new File(swrvePath, fileName).exists()) {
                cacheData = readFile(userId, swrvePath, fileName);
            }
            if (SwrveHelper.isNullOrEmpty(cacheData)) {
                SwrveLogger.i("No cached Qa file found, starting as normal User On Android Native Side.");
                return "{\"reset_device_state\":false,\"logging\":false}";
            }
            break;
        }
        return cacheData;
    }

    @Override
    public void sendEventsInBackground(Context context, String userId, ArrayList<String> events) {

        QaUser.wrappedEvents(new ArrayList<>(events)); // use copy of events

        Intent intent;
        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.O) {
            // Avoid using the deprecated wakeful receiver
            SwrveUnityEventSenderJobService.scheduleJob(context, events);
        } else {
            intent = new Intent(context, SwrveUnityWakefulReceiver.class);
            intent.putStringArrayListExtra(SwrveUnityBackgroundEventSender.EXTRA_EVENTS, events);
            context.sendBroadcast(intent);
        }
    }

    @Override
    public void queueConversationEvent(String eventParamName, String eventPayloadName, String page, int conversationId,
            Map<String, String> payload) {
        if (payload == null) {
            payload = new HashMap<>();
        }
        payload.put(EVENT_KEY, eventPayloadName);
        payload.put(CONVERSATION_KEY, Integer.toString(conversationId));
        payload.put(PAGE_KEY, page);

        SwrveLogger.d("Sending view conversation event: %s", eventParamName);

        Map<String, Object> parameters = new HashMap<>();
        parameters.put(NAME_KEY, eventParamName);

        ArrayList<String> conversationEvents = new ArrayList<>();
        try {
            conversationEvents.add(EventHelper.eventAsJSON(EVENT_KEY, parameters, payload, getNextSequenceNumber(),
                    System.currentTimeMillis()));
        } catch (JSONException e) {
            SwrveLogger.e("Could not queue conversation events params: " + parameters, e);
        }
        sendEventsInBackground(context.get(), getUserId(), conversationEvents);
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
    public JSONObject getDeviceInfo() throws JSONException {
        if (currentDetails.containsKey(DEVICE_INFO_KEY)) {
            return (JSONObject) currentDetails.get(DEVICE_INFO_KEY);
        }
        return null;
    }

    /***
     * eo Config
     */

    @CalledByUnity
    public static boolean isInitialised() {
        return null != SwrveCommon.getInstance();
    }

    @CalledByUnity
    public void showConversation(String conversationJson, String orientation) {
        try {
            SwrveBaseConversation conversation = new SwrveBaseConversation(new JSONObject(conversationJson), cacheDir);
            ConversationActivity.showConversation(context.get(), conversation, SwrveOrientation.parse(orientation));
        } catch (Exception exc) {
            SwrveLogger.e(
                    "Could not JSONify conversation (or another error), conversation string didn't have the correct structure.");
        }
    }

    @CalledByUnity
    public int getConversationVersion() {
        return ISwrveConversationSDK.CONVERSATION_VERSION;
    }

    @CalledByUnity
    public static boolean sdkAvailable() {
        return SwrveHelper.sdkAvailable();
    }

    @CalledByUnity
    public static String getOSDeviceType() {
        final Context context = UnityPlayer.currentActivity;
        return SwrveHelper.getPlatformDeviceType(context);
    }

    @CalledByUnity
    public static String getPlatformOS() {
        final Context context = UnityPlayer.currentActivity;
        return SwrveHelper.getPlatformOS(context);
    }

    @Override
    public int getNextSequenceNumber() {
        return 0;
    }

    @CalledByUnity
    public void setDefaultNotificationChannel(String id, String name, String importance) {
        try {
            notificationChannelUtil.setDefaultNotificationChannel(id, name, importance);
        } catch (Exception ex) {
            SwrveLogger.e("Exception trying to set notification channel details. [id:%s] [name:%s] [importance:%s]", ex,
                    id, name, importance);
        }
    }

    @RequiresApi(api = Build.VERSION_CODES.O)
    @Override
    public NotificationChannel getDefaultNotificationChannel() {
        return notificationChannelUtil.getDefaultNotificationChannel();
    }

    @CalledByUnity
    public static void updateQaUser() {
        try {
            QaUser.update();
        } catch (Exception ex) {
            SwrveLogger.e("Exception trying to update QaUser instance", ex);
        }
    }

    @Override
    public SwrveNotificationConfig getNotificationConfig() {
        // Not used, processed differently in Unity
        return null;
    }

    @Override
    public SwrvePushNotificationListener getNotificationListener() {
        // Not used, we replace SwrveNotificationEngageReceiver with a Unity version and
        // inform the Unity layer from there
        return null;
    }

    @Override
    public SwrveSilentPushListener getSilentPushListener() {
        // Not used, processed differently in Unity
        return null;
    }

    @Override
    public String getJoined() {
        return ""; // Added for geo but not used in Unity yet
    }

    @Override
    public String getLanguage() {
        return ""; // Added for geo but not used in Unity yet
    }

    @Override
    public void setNotificationSwrveCampaignId(String swrveCampaignId) {
        // Not used, processed differently in Unity
    }

    @Override
    public void saveNotificationAuthenticated(int notificationId) {
        try {
            // Load cached notifications
            final Context context = UnityPlayer.currentActivity;
            SharedPreferences sharedPreferences = context.getSharedPreferences(SHARED_PREFERENCE_FILENAME,
                    Context.MODE_PRIVATE);
            JSONArray jsonArray = new JSONArray(
                    sharedPreferences.getString(NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY, "[]"));

            // Create a new notification
            JSONObject authenticatedNotification = new JSONObject();
            authenticatedNotification.put("id", notificationId);
            jsonArray.put(authenticatedNotification);

            jsonArray = checkMaxCachedAuthenticatedNotifications(jsonArray);

            // Save it on cache
            SharedPreferences.Editor editor = sharedPreferences.edit();
            editor.putString(NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY, jsonArray.toString());
            editor.commit();
        } catch (Exception ex) {
            SwrveLogger.e("Exception trying to save an Authenticated Notification", ex);
        }

    }

    @CalledByUnity
    public static void clearAllAuthenticatedNotifications() {
        try {
            // Authenticated notifications are persisted into SharedPreferences because
            // NotificationManager.getActiveNotifications is only available
            // in api 23 and current minVersion is below that
            final Context context = UnityPlayer.currentActivity;
            SharedPreferences sharedPreferences = context.getSharedPreferences(SHARED_PREFERENCE_FILENAME,
                    Context.MODE_PRIVATE);
            final NotificationManager notificationManager = (NotificationManager) context
                    .getSystemService(Context.NOTIFICATION_SERVICE);
            JSONArray allAuthenticatedNotifications = new JSONArray(
                    sharedPreferences.getString(NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY, "[]"));

            // Remove the notification from Android NotificationManager.NOTIFICATION_SERVICE
            for (int i = 0; i < allAuthenticatedNotifications.length(); i++) {
                JSONObject notification = allAuthenticatedNotifications.getJSONObject(i);
                int notificationId = notification.getInt("id");
                notificationManager.cancel(notificationId);
            }

            // Clear our NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY on SharedPreferences
            SharedPreferences.Editor editor = sharedPreferences.edit();
            editor.putString(NOTIFICATIONS_AUTHENTICATED_ID_CACHE_KEY, "[]");
            editor.commit();
        } catch (Exception ex) {
            SwrveLogger.e("Exception trying remove notifications", ex);
        }
    }

    @CalledByUnity
    public static void copyToClipboard(String content) {
        try {
            final Context context = UnityPlayer.currentActivity;
            ClipboardManager clipboard = (ClipboardManager) context.getSystemService(Context.CLIPBOARD_SERVICE);
            ClipData clip = ClipData.newPlainText("simple text", content);
            clipboard.setPrimaryClip(clip);
        } catch (Exception e) {
            SwrveLogger.e("Couldn't copy text to clipboard: %s", e, content);
        }
    }

    private JSONArray checkMaxCachedAuthenticatedNotifications(JSONArray allCachedNotifications) throws Exception {
        if (allCachedNotifications.length() > MAX_CACHED_AUTHENTICATED_NOTIFICATIONS) {
            JSONArray tidyUpNotifications = new JSONArray();
            // starting this for at 1 because "0" is the element that will not be keep on
            // our system.
            for (int i = 1; i < allCachedNotifications.length(); i++) {
                JSONObject notification = allCachedNotifications.getJSONObject(i);
                tidyUpNotifications.put(notification);
            }
            return tidyUpNotifications;
        }
        return allCachedNotifications;
    }

    @Override
    public int getFlushRefreshDelay() {
        return 0; // Added for geo but not used in Unity yet
    }

    // Internal method to notify that the app is ready to receive native calls
    private static void readyToDoNativeCalls() {
        synchronized (readyNativeCallbacksLock) {
            readyToDoNativeCalls = true;
            for (SwrveNativeCall call : nativeCalls) {
                UnityPlayer.UnitySendMessage(call.getObject(), call.getMethod(), call.getMsg());
            }
            nativeCalls.clear();
        }
    }

    // Internal method to send a message to the SDK now or when the app finishes
    // loading.
    // Call this instead of UnityEditor.UnitySendMessage
    public static void UnitySendMessage(String object, String method, String msg) {
        synchronized (readyNativeCallbacksLock) {
            if (readyToDoNativeCalls) {
                UnityPlayer.UnitySendMessage(object, method, msg);
            } else {
                // Store message to notify the SDK when its ready
                nativeCalls.add(new SwrveNativeCall(object, method, msg));
            }
        }
    }

    @Override
    public void setSessionListener(SwrveSessionListener sessionListener) {
        // Added for geo but not used in Unity yet
    }

    @Override
    public void fetchNotificationCampaigns(Set<Long> campaignIds) {
        // Added for geo but not used in Unity yet
    }

    @Override
    public File getCacheDir(Context context) {
        return null;
    }

    @Override
    public void saveEvent(String event) {
        // Added for push event delivery feature, but not implemented on unity.
    }
}
