package com.swrve.sdk;

import android.content.Context;

import androidx.annotation.NonNull;
import androidx.work.Worker;
import androidx.work.WorkerParameters;

public class SwrveUnityBackgroundEventSenderWorker extends Worker {

    public SwrveUnityBackgroundEventSenderWorker(@NonNull Context context, @NonNull WorkerParameters workerParams) {
        super(context, workerParams);
    }

    @NonNull
    @Override
    public Result doWork() {
        Result workResult = Result.success();
        try {
            SwrveLogger.i("SwrveSDK: SwrveUnityBackgroundEventSenderWorker started.");
            SwrveUnityBackgroundEventSender sender = new SwrveUnityBackgroundEventSender(getApplicationContext());
            sender.handleSendEvents(getInputData());
        } catch (Exception ex) {
            SwrveLogger.e("SwrveSDK: SwrveUnityBackgroundEventSenderWorker exception.", ex);
            workResult = Result.failure();
        }
        return workResult;
    }
}
