using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using Swrve.Messaging;

public class MessageCenterMessage : MonoBehaviour
{
    public Text leftText;
    public Text middleText;
    public Button deleteButton;
    public SwrveCampaign campaign;

    public void setCampaign(SwrveCampaign campaign) {
        this.campaign = campaign;
        formatText (leftText, campaign.StartDate.ToString("MM/dd/yyyy"), campaign.Id, campaign.SupportsOrientation(SwrveOrientation.Landscape) ? "Yes" : "No");
        formatText (middleText, campaign.Subject, campaign.Status, campaign.SupportsOrientation(SwrveOrientation.Portrait) ? "Yes" : "No");
    }

    public void onSelected() {
        SwrveComponent.SDK.showMessageCenterCampaign (campaign);
    }

    public void onDeleted() {
        DemoGUI.AskModalQuestion (
            "",
            "Are you sure you want to delete this campaign?",
            () => {
                SwrveComponent.SDK.removeMessageCenterCampaign (campaign);
                transform.SetParent (null);
                Destroy (gameObject);
            }
        );
    }

    void formatText(Text text, params object[] textBits) {
        text.text = string.Format (text.text, textBits);
    }

}