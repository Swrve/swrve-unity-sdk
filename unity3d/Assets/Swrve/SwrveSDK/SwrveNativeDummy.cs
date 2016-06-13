#if !UNITY_ANDROID && !UNITY_IOS
using System.Collections.Generic;

public partial class SwrveSDK
{
    private void setNativeInfo (Dictionary<string, string> deviceInfo) {}
    private string getNativeLanguage () { return null; }
    private void setNativeAppVersion () {}
    private void showNativeConversation (string conversation) {}
    private void initNative (string jsonString) {}
    private void startNativeLocation () {}
    private void startNativeLocationAfterPermission () {}
    private void setNativeConversationVersion () {}
}

#endif