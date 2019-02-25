package com.unity3d.player;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.util.Log;

import java.util.ArrayList;
import java.util.List;

public class UnityPlayer {
    private static List<Object> messages = new ArrayList<Object>();
    @SuppressLint("StaticFieldLeak")
    public static Activity currentActivity;

    public static void UnitySendMessage(String object, String method, String msg) {
        Log.d("UnitySendMessage", "object: " + object + ", method: " + method + ", msg: " + msg);

        List<String> message = new ArrayList<String>();
        message.add(object);
        message.add(method);
        message.add(msg);

        messages.add(message);
    }

    public static List<Object> getMessages() {
        return messages;
    }
}
