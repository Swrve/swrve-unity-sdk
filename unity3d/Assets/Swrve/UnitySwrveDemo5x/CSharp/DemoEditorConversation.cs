#if UNITY_5

using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class DemoEditorConversation : MonoBehaviour {
    public Text header;
    public Text conversation;
    public Action OnCloseCallback;

    public void OnClose() {
        if(null != OnCloseCallback) {
            OnCloseCallback.Invoke();
        }

        Destroy (gameObject);
    }
}

#endif
