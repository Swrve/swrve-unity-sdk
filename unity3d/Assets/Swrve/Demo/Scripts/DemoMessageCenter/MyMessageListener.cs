using SwrveUnity.Messaging;
using UnityEngine;
using System.Collections;

class MyMessageListener : ISwrveMessageListener
{
    public void OnShow(SwrveMessageFormat format)
    {
        // Pause app, disable clicks on other UI elements
        // Optionally: custom message display (for example: transparent background)
        // format.Message.BackgroundAlpha = 0f;
    }
    public void OnShowing(SwrveMessageFormat format)
    {
        // Message displaying, UI elements must continue to be disabled
    }
    public void OnDismiss(SwrveMessageFormat format)
    {
        // Resume app and clicks in other UI elements
        // Invoking ResourcesUpdatedCallback on dismiss just to update the UI.
        SwrveComponent.Instance.SDK.config.ResourcesUpdatedCallback.Invoke();
    }
}