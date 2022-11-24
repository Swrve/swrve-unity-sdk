package com.swrve.sdk;

import static android.content.pm.PackageManager.PERMISSION_DENIED;
import static android.content.pm.PackageManager.PERMISSION_GRANTED;
import static com.swrve.sdk.ISwrveCommon.SWRVE_DEVICE_REGION;
import static com.swrve.sdk.ISwrveCommon.SWRVE_LANGUAGE;
import static com.swrve.sdk.ISwrveCommon.SWRVE_OS;
import static com.swrve.sdk.ISwrveCommon.SWRVE_OS_VERSION;
import static com.swrve.sdk.SwrveUnityCommon.SHARED_PREFERENCE_FILENAME;
import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertNotNull;
import static org.junit.Assert.assertNull;
import static org.junit.Assert.assertTrue;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.doReturn;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.spy;

import android.Manifest;
import android.app.Activity;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.SharedPreferences;
import android.os.Build;

import androidx.test.core.app.ApplicationProvider;

import com.google.gson.Gson;
import com.unity3d.player.UnityPlayer;

import org.json.JSONException;
import org.json.JSONObject;
import org.junit.Assert;
import org.junit.Test;
import org.mockito.Mockito;
import org.robolectric.util.ReflectionHelpers;

import java.io.IOException;
import java.io.InputStream;
import java.net.URL;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

public class SwrveUnitySDKTest extends SwrveBaseTest {

    SwrveUnityCommon getUnitySwrve() {
        return (SwrveUnityCommon) SwrveCommon.getInstance();
    }

    HashMap<String, Object> dummyDeviceInfoForLocation() {
        HashMap<String, Object> deviceInfo = new HashMap<>();

        deviceInfo.put(SWRVE_OS, "dummyAndroid");
        deviceInfo.put(SWRVE_OS_VERSION, "6.0");
        deviceInfo.put(SWRVE_LANGUAGE, "en");
        deviceInfo.put(SWRVE_DEVICE_REGION, "IRL");
        return deviceInfo;
    }

    void initSwrve() {
        Map<String, Object> config = new HashMap<>();
        initSwrve(config);
    }

    void initSwrve(Map<String, Object> config) {
        if (!config.containsKey(SwrveUnityCommon.SDK_VERSION_KEY)) {
            config.put(SwrveUnityCommon.SDK_VERSION_KEY, "5.1");
        }
        if (!config.containsKey(SwrveUnityCommon.SWRVE_TEMPORARY_PATH_KEY)) {
            config.put(SwrveUnityCommon.SWRVE_TEMPORARY_PATH_KEY, "temp");
        }
        if (!config.containsKey(SwrveUnityCommon.PREFAB_NAME_KEY)) {
            config.put(SwrveUnityCommon.PREFAB_NAME_KEY, "SwrvePrefab");
        }
        if (!config.containsKey(SwrveUnityCommon.DEVICE_INFO_KEY)) {
            config.put(SwrveUnityCommon.DEVICE_INFO_KEY, dummyDeviceInfoForLocation());
        }
        if (!config.containsKey(SwrveUnityCommon.USER_ID_KEY)) {
            config.put(SwrveUnityCommon.USER_ID_KEY, "userId");
        }
        new SwrveUnityCommon(new Gson().toJson(config));
    }

    @Test
    public void testSwrveUnitySDK() {

        final int appId = 1234;
        final String apiKey = "apiKey";
        final String userId = "userId";
        final String appVersion = "appVersion";
        final String uniqueKey = "uniqueKey";
        final String batchUrl = "batchUrl";
        final String eventsServer = "eventsServer";
        final String locTag = "locTag";
        final String swrveTemporaryPath = "swrveTemporaryPath";
        final String sigSuffix = "sigSuffix";
        final int httpTimeout = 60;
        final String deviceId = "12345";

        Map<String, Object> map = new HashMap<>();
        map.put("appId", appId);
        map.put(apiKey, apiKey);
        map.put(userId, userId);
        map.put(appVersion, appVersion);
        map.put(uniqueKey, uniqueKey);
        map.put(batchUrl, batchUrl);
        map.put(eventsServer, eventsServer);
        map.put(locTag, locTag);
        map.put(swrveTemporaryPath, swrveTemporaryPath);
        map.put(sigSuffix, sigSuffix);
        map.put("httpTimeout", httpTimeout);
        map.put(SwrveUnityCommon.DEVICE_ID_KEY, "" + deviceId);

        initSwrve(map);

        SwrveUnityCommon swrve = getUnitySwrve();

        String msg1 = String.format("Expected appId %1$d. Got %2$d.", appId, swrve.getAppId());
        assertTrue(msg1, appId == swrve.getAppId());

        checkString(apiKey, swrve.getApiKey());
        checkString(userId, swrve.getUserId());
        checkString(appVersion, swrve.getAppVersion());
        checkString(uniqueKey, swrve.getUniqueKey(userId));
        checkString(eventsServer + batchUrl, swrve.getBatchURL());
        checkString(eventsServer, swrve.getEventsServer());
        checkString(swrveTemporaryPath, swrve.getSwrveTemporaryPath());
        checkString(sigSuffix, swrve.getSigSuffix());

        String msg2 = String.format("Expected httpTimeout %1$d. Got %2$d.", httpTimeout, swrve.getHttpTimeout());
        assertTrue(msg2, httpTimeout == swrve.getHttpTimeout());

        String msg3 = String.format("Expected deviceId %1$s Got %1$s", deviceId, swrve.getDeviceId());
        assertTrue(msg3, deviceId.equals(swrve.getDeviceId()));

    }

    @Test
    public void testUnityFunctions() {
        initSwrve();
        SwrveUnityCommon swrveUnityCommon = getUnitySwrve();

        // These functions are called by unity csharp layer.

        assertTrue(String.format("Expected conversation version 4, got %d", swrveUnityCommon.getConversationVersion()), 4 == swrveUnityCommon.getConversationVersion());
        assertTrue("Expected notifications enabled True by default", SwrveUnityCommon.getAreNotificationsEnabled());
        assertEquals("android", SwrveUnityCommon.getPlatformOS());
        assertEquals("mobile", SwrveUnityCommon.getOSDeviceType());
        assertTrue("Expected sdkAvailable True in tests", SwrveUnityCommon.sdkAvailable());

        SwrveUnityCommon.copyToClipboard("Lorem ipsum");
        ClipboardManager clipboard = (ClipboardManager) ApplicationProvider.getApplicationContext().getSystemService(Context.CLIPBOARD_SERVICE);
        assertEquals("Lorem ipsum", clipboard.getPrimaryClip().getItemAt(0).coerceToText(ApplicationProvider.getApplicationContext()));

        assertEquals(33, SwrveUnityCommon.getTargetOS());
        assertEquals("denied", SwrveUnityCommon.getNotificationPermission());
        assertFalse(SwrveUnityCommon.getNotificationShowRationale());
    }

    @Test
    public void testConversations() throws JSONException {
        try {
            new SwrveBaseConversation(new JSONObject(""), null);
            Assert.fail("Expected empty conversation string to throw JSONException");
        } catch (JSONException e) {
            //success
        }
        try {
            new SwrveBaseConversation(new JSONObject("{}"), null);
            Assert.fail("Expected empty json object to throw JSONException");
        } catch (JSONException e) {
            //success
        }

        final String jsonCampaign = getResourceAsText(mActivity, "campaign_conversation.json");
        assertFalse("Expect loaded json campaign file to be full", null == jsonCampaign || "" == jsonCampaign);

        SwrveBaseConversation conversation = new SwrveBaseConversation(new JSONObject(jsonCampaign), null);
        assertNotNull("Expect loaded SwrveConversation to not be null", conversation);

        checkInt("Conversation id", 82, conversation.getId());
        checkString("Conversation name", "Choose one", conversation.getName());
        checkInt("Conversation pages count", 1, conversation.getPages().size());

        initSwrve();

        SwrveUnityCommon swrveUnityCommonSpy = spy(getUnitySwrve());
        SwrveUnityBackgroundEventSender backgroundEventSenderMock = mock(SwrveUnityBackgroundEventSender.class);
        doNothing().when(backgroundEventSenderMock).send(anyString(), any(List.class));
        doReturn(backgroundEventSenderMock).when(swrveUnityCommonSpy).getSwrveBackgroundEventSender(any(Context.class));
        SwrveCommon.setSwrveCommon(swrveUnityCommonSpy);

        SwrveConversationEventHelper conversationEventHelper = new SwrveConversationEventHelper();
        conversationEventHelper.swrveConversationSDK = swrveUnityCommonSpy;

        conversationEventHelper.conversationCallActionCalledByUser(conversation, "fromPageTag", "toActionTag");

        Map<String, String> map = new HashMap();
        map.put("control", "toActionTag");
        map.put("page", "fromPageTag");
        map.put("event", "call");
        map.put("conversation", "82");
        Mockito.verify(swrveUnityCommonSpy).queueConversationEvent("Swrve.Conversations.Conversation-82.call", "call", "fromPageTag", 82, map);
    }

    @Test
    public void testResolveNotificationPermissionAnsweredTime() {
        Activity activitySpy = spy(UnityPlayer.currentActivity);
        UnityPlayer.currentActivity = activitySpy;
        SharedPreferences sharedPreferences = activitySpy.getSharedPreferences(SHARED_PREFERENCE_FILENAME, Context.MODE_PRIVATE);

        // default
        assertEquals(0, SwrveUnityCommon.resolveNotificationPermissionAnsweredTime());
        assertNull(sharedPreferences.getString("permission_notification_rationale_was_true", null)); // this should always stay true if it is ever set.
        assertNull(sharedPreferences.getString("permission_notification_answered_times", null));

        // simulate a permission request where shouldShowRequestPermissionRationale returns true, thus resolveNotificationPermissionAnsweredTime will always be 1
        doReturn(true).when(activitySpy).shouldShowRequestPermissionRationale(Manifest.permission.POST_NOTIFICATIONS);
        assertEquals(1, SwrveUnityCommon.resolveNotificationPermissionAnsweredTime());
        assertEquals(true, sharedPreferences.getBoolean("permission_notification_rationale_was_true", false));
        assertEquals(1, sharedPreferences.getInt("permission_notification_answered_times", -1));

        // simulate a subsequent request by forcing shouldShowRequestPermissionRationale to be false
        doReturn(false).when(activitySpy).shouldShowRequestPermissionRationale(Manifest.permission.POST_NOTIFICATIONS);
        assertEquals(2, SwrveUnityCommon.resolveNotificationPermissionAnsweredTime());
        assertEquals(true, sharedPreferences.getBoolean("permission_notification_rationale_was_true", false)); // this should always stay true if it is ever set.
        assertEquals(2, sharedPreferences.getInt("permission_notification_answered_times", -1));
    }

    @Test
    public void testResolveNotificationPermissionAnsweredTimeWithRequestsOutsideOfSwrveSDK() {
        Activity activitySpy = spy(UnityPlayer.currentActivity);
        UnityPlayer.currentActivity = activitySpy;
        SharedPreferences sharedPreferences = activitySpy.getSharedPreferences(SHARED_PREFERENCE_FILENAME, Context.MODE_PRIVATE);

        // simulate a permission request happened already outside of swrvesdk. This might have happened through another sdk or customer own code.
        // shouldShowRequestPermissionRationale returns false, and permission_notification_rationale_was_true is true
        doReturn(false).when(activitySpy).shouldShowRequestPermissionRationale(Manifest.permission.POST_NOTIFICATIONS);
        sharedPreferences.edit().putBoolean("permission_notification_rationale_was_true", true).commit();
        assertEquals(2, SwrveUnityCommon.resolveNotificationPermissionAnsweredTime());
        assertEquals(true, sharedPreferences.getBoolean("permission_notification_rationale_was_true", false));
        assertEquals(2, sharedPreferences.getInt("permission_notification_answered_times", -1));
    }

    @Test
    public void testIncrementNotificationPermissionAnsweredTime() {
        SharedPreferences sharedPreferences = UnityPlayer.currentActivity.getSharedPreferences(SHARED_PREFERENCE_FILENAME, Context.MODE_PRIVATE);

        SwrveUnityCommon.incrementNotificationPermissionAnsweredTime();
        assertEquals(1, sharedPreferences.getInt("permission_notification_answered_times", 0));

        SwrveUnityCommon.incrementNotificationPermissionAnsweredTime();
        assertEquals(2, sharedPreferences.getInt("permission_notification_answered_times", 0));

        SwrveUnityCommon.incrementNotificationPermissionAnsweredTime();
        assertEquals(3, sharedPreferences.getInt("permission_notification_answered_times", 0));
    }

    @Test
    public void testCheckNotificationPermissionChange() {
        Activity activitySpy = spy(UnityPlayer.currentActivity);
        UnityPlayer.currentActivity = activitySpy;
        SharedPreferences sharedPreferences = activitySpy.getSharedPreferences(SHARED_PREFERENCE_FILENAME, Context.MODE_PRIVATE);

        assertNull(sharedPreferences.getString("permission_notification_current", null));

        assertNull(SwrveUnityCommon.checkNotificationPermissionChange());
        assertEquals(-1, sharedPreferences.getInt("permission_notification_current", -1000));

        shadowApplication.grantPermissions(Manifest.permission.POST_NOTIFICATIONS);
        assertEquals("Swrve.permission.android.notification.granted", SwrveUnityCommon.checkNotificationPermissionChange());
        assertEquals(PERMISSION_GRANTED, sharedPreferences.getInt("permission_notification_current", -1000));

        shadowApplication.denyPermissions(Manifest.permission.POST_NOTIFICATIONS);
        assertEquals("Swrve.permission.android.notification.denied", SwrveUnityCommon.checkNotificationPermissionChange());
        assertEquals(PERMISSION_DENIED, sharedPreferences.getInt("permission_notification_current", -1000));
    }


    void checkUnityHelper(String method, String message, int index) {
        List<Object> deets = UnityPlayer.getMessages();

        if (0 > index) {
            index = deets.size() - index;
        }
        deets = (List<Object>) deets.get(index);

        String obj = "SwrvePrefab";

        String lastObj = (String) deets.get(0);
        String lastMethod = (String) deets.get(1);
        String lastMessage = (String) deets.get(2);

        try {
            JSONObject jmessage = new JSONObject(message);
            JSONObject jlastMessage = new JSONObject(lastMessage);
            Iterator<String> keysIterator = jmessage.keys();
            String _message = "";
            String _lastMessage = "";
            while (keysIterator.hasNext()) {
                String key = keysIterator.next();
                _message += key + ":" + jmessage.get(key) + ",";
                _lastMessage += key + ":" + jlastMessage.get(key) + ",";
            }
            message = _message;
            lastMessage = _lastMessage;
        } catch (Exception e) {
            e.printStackTrace();
        }

        checkString("lastObj", obj, lastObj);
        checkString("lastMethod", method, lastMethod);
        checkString("lastMessage", message, lastMessage);
    }

    void checkString(String left, String right) {
        checkString(left, left, right);
    }

    void checkString(String key, String left, String right) {
        assertTrue(String.format("Expected %1$s to be \"%2$s\", got \"%3$s\".", key, left, right), left.equals(right));
    }

    void checkInt(String key, int left, int right) {
        assertTrue(String.format("Expected %1$s to be %2$d, %3$d", key, left, right), left == right);
    }

    public static String getResourceAsText(Context context, String filename) {
        InputStream in = null;
        String result = null;
        try {
            URL resource = context.getClassLoader().getResource(filename);
            assertNotNull(resource);
            in = resource.openStream();
            assertNotNull(in);
            java.util.Scanner s = new java.util.Scanner(in).useDelimiter("\\A");
            result = s.hasNext() ? s.next() : "";
            assertFalse(result.length() == 0);
        } catch (IOException e) {
            SwrveLogger.e("Error SwrveTestHelper.", e);
            assert (false);
        } finally {
            if (in != null) {
                try {
                    in.close();
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        }
        return result;
    }
}
