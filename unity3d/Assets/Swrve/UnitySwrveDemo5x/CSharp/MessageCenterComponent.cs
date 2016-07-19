#if UNITY_5

using UnityEngine;
using System.Collections;
using Swrve.Messaging;
using UnityEngine.UI;
using System.Linq;

public class MessageCenterComponent : MonoBehaviour
{
    public Transform ContentPanel;
    public GameObject MessageCenterMessagePrefab;

    // Use this for initialization
    void Start () {
        SwrveComponent.SDK.GetMessageCenterCampaigns(SwrveOrientation.Portrait).ForEach(campaign => {
            GameObject newButton = Instantiate (MessageCenterMessagePrefab) as GameObject;
            MessageCenterMessage message = newButton.GetComponent<MessageCenterMessage> ();
            message.setCampaign (campaign);

            message.transform.SetParent (ContentPanel, false);
        });
    }
}

#endif
