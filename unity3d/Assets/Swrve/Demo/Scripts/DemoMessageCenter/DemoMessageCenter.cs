
using UnityEngine;
using SwrveUnity;
using System.Collections.Generic;
using SwrveUnity.Messaging;
using System.Collections;
using System;

public class DemoMessageCenter : MonoBehaviour
{

    public GameObject templateButtonGameObject;
    void Start()
    {
        SwrveConfig config = new SwrveConfig();
        config.ResourcesUpdatedCallback = ResourcesUpdatedCallback;
        config.InAppMessageConfig.MessageListener = new MyMessageListener();

        // To use the EU stack, include this in your config.
        // config.SelectedStack = SwrveUnity.Stack.EU;

        //FIXME: FIX ME! Replace <app_id> and "<api_key>" with your app ID and API key.
#if PRODUCTION_BUILD
        SwrveComponent.Instance.Init(-1, "<production_api_key>", config);
#else
        SwrveComponent.Instance.Init(-1, "<dev_api_key>", config);
#endif
    }

    private void ConversationEditorCallback(string obj)
    {
        Debug.Log("CallbackSample:" + obj);
    }

    private void ResourcesUpdatedCallback()
    {
        CheckAndAddCampaigns();
    }

    private void CheckAndAddCampaigns()
    {
        List<SwrveBaseCampaign> campaigns = SwrveComponent.Instance.SDK.GetMessageCenterCampaigns(SwrveOrientation.Both);
        IEnumerator<SwrveBaseCampaign> itCampaign = campaigns.GetEnumerator ();
        while(itCampaign.MoveNext()) {
            //Get the current campaign from iterator.
            SwrveBaseCampaign campaign = itCampaign.Current;

            //Check if this campaign is alrady available in scene.
            Transform campaignTransform = this.transform.Find(campaign.Subject + "-" + campaign.Id.ToString());

            //Just remove it from scene, if it CurStatus is "Deleted".
            if(campaign.State.CurStatus == SwrveCampaignState.Status.Deleted && campaignTransform != null) {
                GameObject.Destroy(transform.parent);
            }

            //Look if this campaign already was added on scene / If not create a new button for it.
            GameObject go;
            if(campaignTransform == null) {
                go = (GameObject)Instantiate(templateButtonGameObject);
                go.transform.SetParent(templateButtonGameObject.transform.parent);
                go.name = campaign.Subject + "-" + campaign.Id.ToString();
                go.transform.localScale = Vector3.one;
                go.SetActive(true);
            } else {
                go = campaignTransform.gameObject;
            }
            //Update the CampaignButton with campaign infos.
            go.GetComponent<SampleCampaignButton>().Setup(campaign);
        }
    }
}
