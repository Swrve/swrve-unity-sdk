using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Swrve.Device
{
public class DeviceCarrierInfo : ICarrierInfo
{
#if UNITY_IPHONE
    [DllImport ("__Internal")]
    private static extern string _swrveiOSCarrierName();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSCarrierIsoCountryCode();

    [DllImport ("__Internal")]
    private static extern string _swrveiOSCarrierCode();

    private static readonly string PluginError = "Couldn't invoke native code to get carrier information, make sure you have the iOS plugin inside your project and you are running on a iOS device: ";
#endif

#if UNITY_ANDROID
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
#if UNITY_IPHONE
        try {
            return _swrveiOSCarrierName();
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
            return _swrveiOSCarrierIsoCountryCode();
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
            return _swrveiOSCarrierCode();
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

