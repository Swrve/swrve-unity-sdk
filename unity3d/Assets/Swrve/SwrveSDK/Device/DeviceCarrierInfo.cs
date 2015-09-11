using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Swrve.Device
{
public class DeviceCarrierInfo : ICarrierInfo
{
#if UNITY_IPHONE
    [DllImport ("__Internal")]
    private static extern string _swrveCarrierName();

    [DllImport ("__Internal")]
    private static extern string _swrveCarrierIsoCountryCode();

    [DllImport ("__Internal")]
    private static extern string _swrveCarrierCode();

    private static readonly string PluginError = "Couldn't invoke native code to get carrier information, make sure you have the iOS plugin inside your project and you are running on a iOS device: ";
#endif

#if UNITY_ANDROID
    private AndroidJavaObject androidTelephonyManager;

    public DeviceCarrierInfo()
    {
        try {
            using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                AndroidJavaObject context = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
                string telephonyService = "phone";//ontext.GetStatic<string>("TELEPHONY_SERVICE");
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
#if UNITY_IPHONE
        try {
            return _swrveCarrierName();
        } catch(Exception exp) {
            SwrveLog.LogWarning(PluginError + exp.ToString());
            return null;
        }
#elif UNITY_ANDROID
        return AndroidGetTelephonyManagerAttribute("getSimOperatorName");
#else
        return null;
#endif
    }

    public string GetIsoCountryCode()
    {
#if UNITY_IPHONE
        try {
            return _swrveCarrierIsoCountryCode();
        } catch(Exception exp) {
            SwrveLog.LogWarning(PluginError + exp.ToString());
            return null;
        }
#elif UNITY_ANDROID
        return AndroidGetTelephonyManagerAttribute("getSimCountryIso");
#else
        return null;
#endif
    }

    public string GetCarrierCode()
    {
#if UNITY_IPHONE
        try {
            return _swrveCarrierCode();
        } catch(Exception exp) {
            SwrveLog.LogWarning(PluginError + exp.ToString());
            return null;
        }
#elif UNITY_ANDROID
        return AndroidGetTelephonyManagerAttribute("getSimOperator");
#else
        return null;
#endif
    }
}
}

