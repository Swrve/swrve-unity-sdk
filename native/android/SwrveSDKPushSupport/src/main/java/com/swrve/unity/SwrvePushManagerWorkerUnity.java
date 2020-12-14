package com.swrve.unity;

import android.content.Context;

import androidx.annotation.NonNull;
import androidx.work.WorkerParameters;

import com.swrve.sdk.SwrvePushManagerBaseWorker;
import com.swrve.sdk.SwrvePushManager;

public class SwrvePushManagerWorkerUnity extends SwrvePushManagerBaseWorker {

    public SwrvePushManagerWorkerUnity(@NonNull Context context, @NonNull WorkerParameters workerParams) {
        super(context, workerParams);
    }

    @Override
    public SwrvePushManager getSwrvePushManager() {
        return new SwrvePushManagerUnityImp(getApplicationContext());
    }
}
