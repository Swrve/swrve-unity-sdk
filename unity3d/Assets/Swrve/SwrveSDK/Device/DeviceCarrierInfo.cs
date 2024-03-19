using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace SwrveUnity.Device
{
    public class DeviceCarrierInfo : ICarrierInfo
    {

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject androidTelephonyManager;

    public DeviceCarrierInfo()
    {
        try {
            using (AndroidJavaClass contextClass = new AndroidJavaClass("android.content.Context"))
            using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject context = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity")) {
                string telephonyService = contextClass.GetStatic<string>("TELEPHONY_SERVICE");
                androidTelephonyManager = context.Call<AndroidJavaObject>("getSystemService", telephonyService);
            }
        } catch (Exception exp) {
            SwrveLog.LogWarning("Couldn't get access to TelephonyManager: " + exp.ToString());
        }
    }

    private string AndroidGetTelephonyManagerAttribute(string method)
    {
        if (androidTelephonyManager != null) {
            try {
                return androidTelephonyManager.Call<string>(method);
            } catch (Exception exp) {
                SwrveLog.LogWarning("Problem accessing the TelephonyManager - " + method + ": " + exp.ToString());
            }
        }

        return null;
    }
#endif

        public string GetName()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        return AndroidGetTelephonyManagerAttribute("getSimOperatorName");
#else
            return null;
#endif
        }

        public string GetIsoCountryCode()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        return AndroidGetTelephonyManagerAttribute("getSimCountryIso");
#else
            return null;
#endif
        }

        public string GetCarrierCode()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        return AndroidGetTelephonyManagerAttribute("getSimOperator");
#else
            return null;
#endif
        }
    }
}
