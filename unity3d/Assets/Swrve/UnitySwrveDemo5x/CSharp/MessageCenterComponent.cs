#if UNITY_5

using UnityEngine;
using System.Collections;
using SwrveUnity.Messaging;
using UnityEngine.UI;
using System.Linq;

public class MessageCenterComponent : MonoBehaviour
{
    public Transform ContentPanel;
    public GameObject MessageCenterMessagePrefab;

    /// Reference to the Swrve Component in the scene.
    private SwrveComponent swrveComponent;

    // Use this for initialization
    void Start () {
        swrveComponent = (SwrveComponent)FindObjectOfType (typeof(SwrveComponent));

        swrveComponent.SDK.GetMessageCenterCampaigns(SwrveOrientation.Portrait).ForEach(campaign => {
            GameObject newButton = Instantiate (MessageCenterMessagePrefab) as GameObject;
            MessageCenterMessage message = newButton.GetComponent<MessageCenterMessage> ();
            message.setCampaign (campaign);

            message.transform.SetParent (ContentPanel, false);
        });
    }
}

#endif
