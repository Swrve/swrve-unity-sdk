#if UNITY_5

using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DemoEditorConversation : MonoBehaviour {
    public Text header;
    public Text conversation;

    public void OnClose() {
        Destroy (gameObject);
    }
}

#endif
