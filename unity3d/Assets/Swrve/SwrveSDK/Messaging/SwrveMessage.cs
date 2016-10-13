using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using SwrveUnity.Helpers;

namespace SwrveUnity.Messaging
{
/// <summary>
/// In-app message.
/// </summary>
public class SwrveMessage : SwrveBaseMessage
{
    /// <summary>
    /// Name of the message.
    /// </summary>
    public string Name;

    /// <summary>
    /// Priority of the message.
    /// </summary>
    public int Priority = 9999;

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
    /// Background alpha.
    /// </summary>
    public float BackgroundAlpha = 1f;

    /// <summary>
    /// Global message animation extra scale.
    /// </summary>
    public float AnimationScale = 1f;

    public ISwrveAssetController assetController;

    private SwrveMessage (ISwrveAssetController assetController, SwrveMessagesCampaign campaign)
    {
        this.assetController = assetController;
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
    public static SwrveMessage LoadFromJSON (SwrveSDK sdk, SwrveMessagesCampaign campaign, Dictionary<string, object> messageData)
    {
        SwrveMessage message = new SwrveMessage (sdk, campaign);
        message.Id = MiniJsonHelper.GetInt (messageData, "id");
        message.Name = (string)messageData ["name"];

        if (messageData.ContainsKey ("priority")) {
            message.Priority = MiniJsonHelper.GetInt (messageData, "priority");
        }

        Dictionary<string, object> template = (Dictionary<string, object>)messageData ["template"];
        IList<object> jsonFormats = (List<object>)template ["formats"];

        for (int i = 0, j = jsonFormats.Count; i < j; i++) {
            Dictionary<string, object> messageFormatData = (Dictionary<string, object>)jsonFormats [i];
            SwrveMessageFormat messageFormat = SwrveMessageFormat.LoadFromJSON (sdk, message, messageFormatData);
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
        if (orientation == SwrveOrientation.Both) {
            return true;
        }
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
        for(int fi = 0; fi < Formats.Count; fi++) {
            SwrveMessageFormat format = Formats[fi];
            for(int ii = 0; ii < format.Images.Count; ii++) {
                SwrveImage image = format.Images[ii];
                if (!string.IsNullOrEmpty (image.File)) {
                    messageAssets.Add (image.File);
                }
            }

            for(int bi = 0; bi < format.Buttons.Count; bi++) {
                SwrveButton button = format.Buttons[bi];
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
    public bool IsDownloaded ()
    {
        List<string> assets = this.ListOfAssets ();
        for(int ai = 0; ai < assets.Count; ai++) {
            string asset = assets[ai];
            if(!assetController.IsAssetInCache (asset)) {
                return false;
            }
        }

        return true;
    }

    override public string GetBaseFormattedMessageType() {
        return "Message";
    }
}
}