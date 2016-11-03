#if !UNITY_ANDROID && !UNITY_IOS && !(UNITY_WSA_10_0 && SWRVE_WINDOWS_SDK)
using System.Collections.Generic;

public partial class SwrveSDK
{
    private void setNativeInfo (Dictionary<string, string> deviceInfo) {}
    private string getNativeLanguage () { return null; }
    private void setNativeAppVersion () {}
    private void showNativeConversation (string conversation) {}
    private void initNative () {}
    private void startNativeLocation () {}
    private void startNativeLocationAfterPermission () {}
    public void LocationUserUpdate (Dictionary<string, string> map) {}
    public string GetPlotNotifications () { return "[]"; }
    private void setNativeConversationVersion () {}
    private bool NativeIsBackPressed () { return false; }
}

#endif