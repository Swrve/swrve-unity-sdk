#if UNITY_5 || UNITY_5_OR_NEWER || UNITY_2017_1_OR_NEWER

using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class DemoEditorConversation : MonoBehaviour
{
	public Text header;
	public Text conversation;
	public Action OnCloseCallback;

	public void OnClose()
    {
        if(null != OnCloseCallback) {
            OnCloseCallback.Invoke();
        }

        Destroy (gameObject);
    }
}

#endif
