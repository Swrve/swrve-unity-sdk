#if UNITY_5 || UNITY_2017_1_OR_NEWER

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using SwrveUnity.Messaging;

public class MessageCenterMessage : MonoBehaviour
{
public Text leftText;
public Text middleText;
public Button deleteButton;
public SwrveBaseCampaign campaign;

/// Reference to the Swrve Component in the scene.
private SwrveComponent swrveComponent;

// Use this for initialization
void Start ()
    {
        swrveComponent = (SwrveComponent)FindObjectOfType (typeof(SwrveComponent));
    }

    public void setCampaign(SwrveBaseCampaign campaign)
    {
        this.campaign = campaign;
        updateCampaignInfo ();
    }

    public void updateCampaignInfo ()
    {
        formatText (
            leftText,
            "ID: {1}\n{0}\nLandscape: {2}",
            campaign.StartDate.ToString("MM/dd/yyyy"), campaign.Id, campaign.SupportsOrientation(SwrveOrientation.Landscape) ? "Yes" : "No"
        );
        formatText (
            middleText,
            "Subject: {0}\nStatus: {1}\nPortrait: {2}",
            campaign.Subject, campaign.Status, campaign.SupportsOrientation(SwrveOrientation.Portrait) ? "Yes" : "No"
        );
    }

    public void onSelected()
    {
        swrveComponent.SDK.ShowMessageCenterCampaign (campaign, SwrveOrientation.Portrait);
        updateCampaignInfo ();
    }

    public void onDeleted()
    {
        HomeMenuComponent.AskModalQuestion (
            "Delete Campaign",
            "Are you sure you want to delete this campaign?",
        () => {
            swrveComponent.SDK.RemoveMessageCenterCampaign (campaign);
            transform.SetParent (null);
            Destroy (gameObject);
        }
        );
    }

    void formatText(Text text, string format, params object[] textBits)
    {
        text.text = string.Format (format, textBits);
    }

}

#endif
