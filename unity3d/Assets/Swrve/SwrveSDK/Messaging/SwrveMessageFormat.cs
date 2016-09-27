using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using SwrveUnity.Helpers;

namespace SwrveUnity.Messaging
{
/// <summary>
/// In-app message format.
/// </summary>
public class SwrveMessageFormat
{
    /// <summary>
    /// Name of the format.
    /// </summary>
    public string Name;

    /// <summary>
    /// Collection of buttons inside the message.
    /// </summary>
    public List<SwrveButton> Buttons;

    /// <summary>
    /// Collection of images inside the message.
    /// </summary>
    public List<SwrveImage> Images;

    /// <summary>
    /// Language of the format.
    /// </summary>
    public string Language;

    /// <summary>
    /// Orientation of the format.
    /// </summary>
    public SwrveOrientation Orientation;

    /// <summary>
    /// Size of the format.
    /// </summary>
    public Point Size = new Point (0, 0);

    /// <summary>
    /// Parent in-app message.
    /// </summary>
    public SwrveMessage Message;

    /// <summary>
    /// Scale set by the server for this device and format.
    /// </summary>
    public float Scale = 1f;

    /// <summary>
    /// Color of the background.
    /// </summary>
    public Color? BackgroundColor = null;

    /// <summary>
    /// Custom button listener to process install button events.
    /// </summary>
    public ISwrveInstallButtonListener InstallButtonListener;

    /// <summary>
    /// Custom button listener to process custom button events.
    /// </summary>
    public ISwrveCustomButtonListener CustomButtonListener;

    /// <summary>
    /// Custom message listener to process message events.
    /// </summary>
    public ISwrveMessageListener MessageListener;

    /// <summary>
    /// Flag to indicate if the message is closing.
    /// </summary>
    public bool Closing = false;

    /// <summary>
    /// Flag to indicate if the message has been dismissed.
    /// </summary>
    public bool Dismissed = false;

    /// <summary>
    /// Flag to indicate if the message has to be rotated.
    /// </summary>
    public bool Rotate = false;

    private SwrveMessageFormat (SwrveMessage message)
    {
        this.Message = message;
        this.Buttons = new List<SwrveButton> ();
        this.Images = new List<SwrveImage> ();
    }

    /// <summary>
    /// Load an in-app message format from a JSON response.
    /// </summary>
    /// <param name="message">
    /// Parent in-app message.
    /// </param>
    /// <param name="messageFormatData">
    /// JSON object with the individual message format data.
    /// </param>
    /// <returns>
    /// Parsed in-app message format.
    /// </returns>
    public static SwrveMessageFormat LoadFromJSON (SwrveSDK sdk, SwrveMessage message, Dictionary<string, object> messageFormatData)
    {
        SwrveMessageFormat messageFormat = new SwrveMessageFormat (message);

        messageFormat.Name = (string)messageFormatData ["name"];
        messageFormat.Language = (string)messageFormatData ["language"];
        if (messageFormatData.ContainsKey ("scale")) {
            messageFormat.Scale = MiniJsonHelper.GetFloat (messageFormatData, "scale", 1);
        }

        if (messageFormatData.ContainsKey ("orientation")) {
            messageFormat.Orientation = SwrveOrientationHelper.Parse ((string)messageFormatData ["orientation"]);
        }

        messageFormat.BackgroundColor = sdk.DefaultBackgroundColor;
        if (messageFormatData.ContainsKey ("color")) {
            string strColor = (string)messageFormatData ["color"];
            Color? c = messageFormat.BackgroundColor;
            if (strColor.Length == 8) {
                // RRGGBB
                byte a = byte.Parse(strColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte r = byte.Parse(strColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(strColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(strColor.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                c = new Color32(r, g, b, a);
            } else if (strColor.Length == 6) {
                // AARRGGBB
                byte r = byte.Parse(strColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(strColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(strColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                c = new Color32(r, g, b, 255);
            }
            messageFormat.BackgroundColor = c;
        }

        Dictionary<string, object> sizeJson = (Dictionary<string, object>)messageFormatData ["size"];
        messageFormat.Size.X = MiniJsonHelper.GetInt (((Dictionary<string, object>)sizeJson ["w"]), "value");
        messageFormat.Size.Y = MiniJsonHelper.GetInt (((Dictionary<string, object>)sizeJson ["h"]), "value");

        IList<object> jsonButtons = (List<object>)messageFormatData ["buttons"];
        for (int i = 0, j = jsonButtons.Count; i < j; i++) {
            SwrveButton button = LoadButtonFromJSON (message, (Dictionary<string, object>)jsonButtons [i]);
            messageFormat.Buttons.Add (button);
        }

        IList<object> jsonImages = (List<object>)messageFormatData ["images"];
        for (int ii = 0, ji = jsonImages.Count; ii < ji; ii++) {
            SwrveImage image = LoadImageFromJSON (message, (Dictionary<string, object>)jsonImages [ii]);
            messageFormat.Images.Add (image);
        }


        return messageFormat;
    }

    protected static int IntValueFromAttribute (Dictionary<string, object> data, string attribute)
    {
        return MiniJsonHelper.GetInt (((Dictionary<string, object>)data [attribute]), "value");
    }

    protected static string StringValueFromAttribute (Dictionary<string, object> data, string attribute)
    {
        return (string)(((Dictionary<string, object>)data [attribute]) ["value"]);
    }

    protected static SwrveButton LoadButtonFromJSON (SwrveMessage message, Dictionary<string, object> buttonData)
    {
        SwrveButton button = new SwrveButton ();
        button.Position.X = IntValueFromAttribute (buttonData, "x");
        button.Position.Y = IntValueFromAttribute (buttonData, "y");

        button.Size.X = IntValueFromAttribute (buttonData, "w");
        button.Size.Y = IntValueFromAttribute (buttonData, "h");

        button.Image = StringValueFromAttribute (buttonData, "image_up");
        button.Message = message;

        if (buttonData.ContainsKey ("name")) {
            button.Name = (string)buttonData ["name"];
        }

        string actionTypeStr = StringValueFromAttribute (buttonData, "type");
        SwrveActionType actionType = SwrveActionType.Dismiss;
        if (actionTypeStr.ToLower ().Equals ("install")) {
            actionType = SwrveActionType.Install;
        } else if (actionTypeStr.ToLower ().Equals ("custom")) {
            actionType = SwrveActionType.Custom;
        }

        button.ActionType = actionType;
        button.Action = StringValueFromAttribute (buttonData, "action");
        if (button.ActionType == SwrveActionType.Install) {
            string appId = StringValueFromAttribute (buttonData, "game_id");
            if (appId != null && appId != string.Empty) {
                button.AppId = int.Parse (appId);
            }
        }

        return button;
    }

    protected static SwrveImage LoadImageFromJSON (SwrveMessage message, Dictionary<string, object> imageData)
    {
        SwrveImage image = new SwrveImage ();
        image.Position.X = IntValueFromAttribute (imageData, "x");
        image.Position.Y = IntValueFromAttribute (imageData, "y");

        image.Size.X = IntValueFromAttribute (imageData, "w");
        image.Size.Y = IntValueFromAttribute (imageData, "h");

        image.File = StringValueFromAttribute (imageData, "image");

        return image;
    }

    /// <summary>
    /// Dismiss the message format.
    /// </summary>
    public void Dismiss ()
    {
        if (!this.Closing) {
            this.Closing = true;
            this.CustomButtonListener = null;
            this.InstallButtonListener = null;
            Message.Campaign.MessageDismissed();

            if (this.MessageListener != null) {
                this.MessageListener.OnDismiss (this);
                this.MessageListener = null;
            }
        }
    }

    /// <summary>
    /// Initialize the message to be displayed.
    /// </summary>
    public void Init (SwrveOrientation deviceOrientation)
    {
        this.Closing = false;
        this.Dismissed = false;
        // Check if we have to rotate the format
        this.Rotate = (this.Orientation != deviceOrientation);

        if (MessageListener != null) {
            MessageListener.OnShow (this);
        }
    }

    /// <summary>
    /// Initialize the message to be displayed.
    /// </summary>
    public void InitAnimation (Point startPoint, Point endPoint)
    {
        this.Message.Position = startPoint;
        this.Message.TargetPosition = endPoint;
    }

    /// <summary>
    /// Remote the format assets from memory.
    /// </summary>
    public void UnloadAssets ()
    {
        for(int ii = 0; ii < Images.Count; ii++) {
            SwrveImage image = Images[ii];
            Texture2D.Destroy (image.Texture);
            image.Texture = null;
        }

        for(int bi = 0; bi < Buttons.Count; bi++) {
            SwrveButton button = Buttons[bi];
            Texture2D.Destroy (button.Texture);
            button.Texture = null;
        }
    }
}
}
