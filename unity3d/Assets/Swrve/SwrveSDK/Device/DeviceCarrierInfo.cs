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
#endif

#if UNITY_ANDROID
      private AndroidJavaObject androidTelephonyManager;

      public DeviceCarrierInfo() {
          try {
              using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                  AndroidJavaObject context = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
                  string telephonyService = context.GetStatic<string>("TELEPHONY_SERVICE");
                  androidTelephonyManager = context.Call<AndroidJavaObject>("getSystemService", telephonyService);
              }
          } catch (Exception exp) {
              SwrveLog.LogWarning("Couldn't get access to TelephonyManager: " + exp.ToString());
          }
      }

      private string AndroidGetTelephonyManagerAttribute(string method) {
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

      public string GetName() {
#if UNITY_IPHONE
          return _swrveCarrierName();
#elif UNITY_ANDROID
          return AndroidGetTelephonyManagerAttribute("getSimOperatorName");
#else
          return null;
#endif
      }

      public string GetIsoCountryCode() {
#if UNITY_IPHONE
          return _swrveCarrierIsoCountryCode();
#elif UNITY_ANDROID
          return AndroidGetTelephonyManagerAttribute("getSimCountryIso");
#else
          return null;
#endif
      }

      public string GetCarrierCode() {
#if UNITY_IPHONE
          return _swrveCarrierCode();
#elif UNITY_ANDROID
          return AndroidGetTelephonyManagerAttribute("getSimOperator");
#else
          return null;
#endif
      }
  }
}

