#if !UNITY_ANDROID && !UNITY_IOS
using System.Collections.Generic;

public partial class SwrveSDK
{

private void setNativeInfo (Dictionary<string, string> deviceInfo) {}
    private string getNativeLanguage ()
    {
        return null;
    }
    private void setNativeAppVersion () {}
    private void showNativeConversation (string conversation) {}
    private void setNativeConversationVersion () {}
    private bool NativeIsBackPressed ()
    {
        return false;
    }
    private bool IsConversationDisplaying()
    {
        return false;
    }
    public void updateQAUser(Dictionary<string, object> map) {}
    private void initNative () {}
}

#endif
