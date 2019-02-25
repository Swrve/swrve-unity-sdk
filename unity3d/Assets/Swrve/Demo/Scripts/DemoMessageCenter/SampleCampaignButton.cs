using UnityEngine;
using UnityEngine.UI;
using SwrveUnity.Messaging;

public class SampleCampaignButton : MonoBehaviour
{

    private Button buttonComponent;
    public SwrveBaseCampaign campaign;

    public void Setup(SwrveBaseCampaign campaign)
    {
        this.campaign = campaign;
        this.GetComponentInChildren<Text>().text = campaign.Subject;
        buttonComponent = this.GetComponent<Button>();
        updateButtonLayout();
    }

    private void updateButtonLayout()
    {
        if (this.campaign.State.CurStatus == SwrveCampaignState.Status.Seen) {
            buttonComponent.image.color = Color.green;
        } else if (this.campaign.State.CurStatus == SwrveCampaignState.Status.Unseen) {
            buttonComponent.image.color = Color.gray;
        }
    }
    public void ButtonOnClickShowMessage()
    {
        SwrveComponent.Instance.SDK.ShowMessageCenterCampaign(this.campaign);
    }

    public void ButtonOnClickRemovedMessage()
    {
        SwrveComponent.Instance.SDK.RemoveMessageCenterCampaign(this.campaign);
        // Just to remove our button from UI.
        this.gameObject.SetActive(false);
    }
}
