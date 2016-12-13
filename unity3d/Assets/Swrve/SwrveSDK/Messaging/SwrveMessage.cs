using UnityEngine;
using System.Collections.Generic;
using SwrveUnity.Helpers;
using System.Linq;

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

    private ISwrveAssetsManager SwrveAssetsManager;

    private SwrveMessage (ISwrveAssetsManager swrveAssetsManager, SwrveMessagesCampaign campaign)
    {
        this.SwrveAssetsManager = swrveAssetsManager;
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
    public static SwrveMessage LoadFromJSON (ISwrveAssetsManager swrveAssetsManager, SwrveMessagesCampaign campaign, Dictionary<string, object> messageData, Color? defaultBackgroundColor)
    {
        SwrveMessage message = new SwrveMessage (swrveAssetsManager, campaign);
        message.Id = MiniJsonHelper.GetInt (messageData, "id");
        message.Name = (string)messageData ["name"];

        if (messageData.ContainsKey ("priority")) {
            message.Priority = MiniJsonHelper.GetInt (messageData, "priority");
        }

        Dictionary<string, object> template = (Dictionary<string, object>)messageData ["template"];
        IList<object> jsonFormats = (List<object>)template ["formats"];

        for (int i = 0, j = jsonFormats.Count; i < j; i++) {
            Dictionary<string, object> messageFormatData = (Dictionary<string, object>)jsonFormats [i];
            SwrveMessageFormat messageFormat = SwrveMessageFormat.LoadFromJSON (swrveAssetsManager, message, messageFormatData, defaultBackgroundColor);
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
    public HashSet<SwrveAssetsQueueItem> SetOfAssets ()
    {
        HashSet<SwrveAssetsQueueItem> messageAssets = new HashSet<SwrveAssetsQueueItem> ();
        for(int fi = 0; fi < Formats.Count; fi++) {
            SwrveMessageFormat format = Formats[fi];
            for(int ii = 0; ii < format.Images.Count; ii++) {
                SwrveImage image = format.Images[ii];
                if (!string.IsNullOrEmpty (image.File)) {
                    messageAssets.Add (new SwrveAssetsQueueItem(image.File, image.File, true));
                }
            }

            for(int bi = 0; bi < format.Buttons.Count; bi++) {
                SwrveButton button = format.Buttons[bi];
                if (!string.IsNullOrEmpty (button.Image)) {
                    messageAssets.Add (new SwrveAssetsQueueItem(button.Image, button.Image, true));
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
        HashSet<SwrveAssetsQueueItem> assets = this.SetOfAssets ();
        return assets.All (asset => this.SwrveAssetsManager.AssetsOnDisk.Contains(asset.Name));
    }

    override public string GetBaseFormattedMessageType() {
        return "Message";
    }
}
}