using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using Swrve.Helpers;

namespace Swrve.Messaging
{
/// <summary>
/// In-app message.
/// </summary>
public class SwrveMessage
{
    /// <summary>
    /// Identifies the message in a campaign.
    /// </summary>
    public int Id;

    /// <summary>
    /// Name of the message.
    /// </summary>
    public string Name;

    /// <summary>
    /// Priority of the message.
    /// </summary>
    public int Priority = 9999;

    /// <summary>
    /// Parent in-app campaign.
    /// </summary>
    public SwrveCampaign Campaign;

    /// <summary>
    /// List of formats available for the device.
    /// </summary>
    public List<SwrveMessageFormat> Formats;

    /// <summary>
    /// Position of the message in the screen.
    /// </summary>
    public Point Position = new Point (0, 0);

    /// <summary>
    /// Target position used for animation.
    /// </summary>
    public Point TargetPosition = new Point (0, 0);

    /// <summary>
    /// Color of the background.
    /// </summary>
    public Color? BackgroundColor = Color.black;

    /// <summary>
    /// Background alpha.
    /// </summary>
    public float BackgroundAlpha = 1f;

    /// <summary>
    /// Global message animation extra scale.
    /// </summary>
    public float AnimationScale = 1f;

    private SwrveMessage (SwrveCampaign campaign)
    {
        this.Campaign = campaign;
        this.Formats = new List<SwrveMessageFormat> ();
    }

    /// <summary>
    /// Get the format for the given orientation.
    /// </summary>
    /// <param name="orientation">
    /// Current orientation.
    /// </param>
    /// <returns>
    /// The format with the given orientation if available.
    /// </returns>
    public SwrveMessageFormat GetFormat (SwrveOrientation orientation)
    {
        IEnumerator<SwrveMessageFormat> formatsIt = Formats.GetEnumerator ();
        while (formatsIt.MoveNext()) {
            if (formatsIt.Current.Orientation == orientation) {
                return formatsIt.Current;
            }
        }

        return null;
    }

    /// <summary>
    /// Load an in-app message from a JSON response.
    /// </summary>
    /// <param name="campaign">
    /// Parent in-app campaign.
    /// </param>
    /// <param name="messageData">
    /// JSON object with the individual message data.
    /// </param>
    /// <returns>
    /// Parsed in-app message.
    /// </returns>
    public static SwrveMessage LoadFromJSON (SwrveCampaign campaign, Dictionary<string, object> messageData)
    {
        SwrveMessage message = new SwrveMessage (campaign);
        message.Id = MiniJsonHelper.GetInt (messageData, "id");
        message.Name = (string)messageData ["name"];

        if (messageData.ContainsKey ("priority")) {
            message.Priority = MiniJsonHelper.GetInt (messageData, "priority");
        }

        Dictionary<string, object> template = (Dictionary<string, object>)messageData ["template"];
        IList<object> jsonFormats = (List<object>)template ["formats"];

        for (int i = 0, j = jsonFormats.Count; i < j; i++) {
            Dictionary<string, object> messageFormatData = (Dictionary<string, object>)jsonFormats [i];
            SwrveMessageFormat messageFormat = SwrveMessageFormat.LoadFromJSON (message, messageFormatData);
            message.Formats.Add (messageFormat);
        }

        return message;
    }

    /// <summary>
    /// Check if the message supports the given orientation
    /// </summary>
    /// <returns>
    /// True if there is any format that supports the given orientation.
    /// </returns>
    public bool SupportsOrientation (SwrveOrientation orientation)
    {
        return (GetFormat (orientation) != null);
    }

    /// <summary>
    /// Get all the assets in the in-app message.
    /// </summary>
    /// <returns>
    /// All the assets in the in-app message.
    /// </returns>
    public List<string> ListOfAssets ()
    {
        List<string> messageAssets = new List<string> ();
        foreach (SwrveMessageFormat format in Formats) {
            foreach (SwrveImage image in format.Images) {
                if (!string.IsNullOrEmpty (image.File)) {
                    messageAssets.Add (image.File);
                }
            }

            foreach (SwrveButton button in format.Buttons) {
                if (!string.IsNullOrEmpty (button.Image)) {
                    messageAssets.Add (button.Image);
                }
            }
        }
        return messageAssets;
    }

    /// <summary>
    /// Check if the campaign assets have been downloaded.
    /// </summary>
    /// <returns>
    /// True if the campaign assets have been downloaded.
    /// </returns>
    public bool isDownloaded (string assetPath)
    {
        List<string> assets = this.ListOfAssets ();
        foreach (string asset in assets) {
            if (!CrossPlatformFile.Exists (assetPath + "/" + asset)) {
                return false;
            }
        }

        return true;
    }
}
}