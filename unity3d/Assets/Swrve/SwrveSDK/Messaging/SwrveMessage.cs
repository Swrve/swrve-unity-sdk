using UnityEngine;
using System.Collections.Generic;
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
        /// List of formats available for the device.
        /// </summary>
        public List<SwrveMessageFormat> Formats;

        private ISwrveAssetsManager SwrveAssetsManager;

        private SwrveMessage(ISwrveAssetsManager swrveAssetsManager, SwrveInAppCampaign campaign)
        {
            this.SwrveAssetsManager = swrveAssetsManager;
            this.Campaign = campaign;
            this.Formats = new List<SwrveMessageFormat>();
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
        public SwrveMessageFormat GetFormat(SwrveOrientation orientation)
        {
            IEnumerator<SwrveMessageFormat> formatsIt = Formats.GetEnumerator();
            while (formatsIt.MoveNext())
            {
                if (formatsIt.Current.Orientation == orientation)
                {
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
        public static SwrveMessage LoadFromJSON(ISwrveAssetsManager swrveAssetsManager, SwrveInAppCampaign campaign, Dictionary<string, object> messageData, Color? defaultBackgroundColor)
        {
            SwrveMessage message = new SwrveMessage(swrveAssetsManager, campaign);
            message.Id = MiniJsonHelper.GetInt(messageData, "id");
            message.Name = (string)messageData["name"];

            if (messageData.ContainsKey("priority"))
            {
                message.Priority = MiniJsonHelper.GetInt(messageData, "priority");
            }

            Dictionary<string, object> template = (Dictionary<string, object>)messageData["template"];
            IList<object> jsonFormats = (List<object>)template["formats"];

            for (int i = 0, j = jsonFormats.Count; i < j; i++)
            {
                Dictionary<string, object> messageFormatData = (Dictionary<string, object>)jsonFormats[i];
                SwrveMessageFormat messageFormat = SwrveMessageFormat.LoadFromJSON(message, messageFormatData, defaultBackgroundColor);
                message.Formats.Add(messageFormat);
            }

            return message;
        }

        /// <summary>
        /// Get all the assets in the in-app message.
        /// </summary>
        /// <returns>
        /// All the assets in the in-app message.
        /// </returns>
        public HashSet<SwrveAssetsQueueItem> SetOfAssets(Dictionary<string, string> personalizationProperties)
        {
            HashSet<SwrveAssetsQueueItem> messageAssets = new HashSet<SwrveAssetsQueueItem>();
            for (int fi = 0; fi < Formats.Count; fi++)
            {
                SwrveMessageFormat format = Formats[fi];
                var pagesKeys = format.Pages.Keys;
                foreach (long key in pagesKeys)
                {
                    SwrveMessagePage page = format.Pages[key];
                    for (int ii = 0; ii < page.Images.Count; ii++)
                    {
                        SwrveImage image = page.Images[ii];
                        if (!string.IsNullOrEmpty(image.DynamicImageUrl))
                        {
                            try
                            {
                                string resolvedUrl = SwrveTextTemplating.Apply(image.DynamicImageUrl, personalizationProperties);
                                byte[] dynamicAssetBytes = System.Text.Encoding.UTF8.GetBytes(resolvedUrl);
                                messageAssets.Add(new SwrveAssetsQueueItem(SwrveHelper.sha1(dynamicAssetBytes), resolvedUrl, true, true));
                            }
                            catch (SwrveSDKTextTemplatingException exception)
                            {
                                SwrveLog.LogWarning("Could not resolve personalization for: " + image.DynamicImageUrl + " " + exception.ToString());
                            }
                        }

                        if (!string.IsNullOrEmpty(image.File))
                        {
                            messageAssets.Add(new SwrveAssetsQueueItem(image.File, image.File, true, false));
                        }
                    }

                    for (int bi = 0; bi < page.Buttons.Count; bi++)
                    {
                        SwrveButton button = page.Buttons[bi];

                        if (!string.IsNullOrEmpty(button.DynamicImageUrl))
                        {
                            try
                            {
                                string resolvedUrl = SwrveTextTemplating.Apply(button.DynamicImageUrl, personalizationProperties);
                                byte[] dynamicAssetBytes = System.Text.Encoding.UTF8.GetBytes(resolvedUrl);
                                messageAssets.Add(new SwrveAssetsQueueItem(SwrveHelper.sha1(dynamicAssetBytes), resolvedUrl, true, true));
                            }
                            catch (SwrveSDKTextTemplatingException exception)
                            {
                                SwrveLog.LogWarning("Could not resolve personalization for: " + button.DynamicImageUrl + " " + exception.ToString());
                            }
                        }

                        if (!string.IsNullOrEmpty(button.Image))
                        {
                            messageAssets.Add(new SwrveAssetsQueueItem(button.Image, button.Image, true, false));
                        }
                    }
                }
            }

            return messageAssets;
        }

        /// <summary>
        /// Check if a single asset name is available on disk.
        /// </summary>
        /// <returns>
        /// True if the asset by that name has an asset on disk.
        /// </returns>
        public bool IsAssetDownloaded(string assetName)
        {
            if (assetName == null)
            {
                return false;
            }

            return this.SwrveAssetsManager.AssetsOnDisk.Contains(assetName);
        }

        /// <summary>
        /// Check if the campaign assets have been downloaded.
        /// </summary>
        /// <returns>
        /// True if the campaign has enough assets to be successfully displayed.
        /// </returns>
        public bool IsDownloaded(Dictionary<string, string> personalizationProperties)
        {
            if (Formats != null)
            {
                for (int fi = 0; fi < Formats.Count; fi++)
                {
                    SwrveMessageFormat format = Formats[fi];
                    if (!IsButtonAssetDownloaded(format, personalizationProperties) || !IsImageAssetDownloaded(format, personalizationProperties))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Check if there are any features that aren't currently supported by the SDK in a campaign
        /// </summary>
        /// <returns>
        /// True if there are no features that aren't supported by the SDK
        /// </returns>
        public bool IsSupportedBySDK()
        {
            if (Formats != null)
            {
                for (int fi = 0; fi < Formats.Count; fi++)
                {
                    SwrveMessageFormat format = Formats[fi];
                    var pagesKeys = format.Pages.Keys;
                    foreach (long key in pagesKeys)
                    {
                        SwrveMessagePage page = format.Pages[key];
                        for (int bi = 0; bi < page.Buttons.Count; bi++)
                        {
                            SwrveButton button = page.Buttons[bi];
                            if (button.ActionType == SwrveActionType.Capability)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool IsButtonAssetDownloaded(SwrveMessageFormat format, Dictionary<string, string> personalizationProperties)
        {
            var pagesKeys = format.Pages.Keys;
            foreach (long key in pagesKeys)
            {
                SwrveMessagePage page = format.Pages[key];
                for (int bi = 0; bi < page.Buttons.Count; bi++)
                {
                    SwrveButton button = page.Buttons[bi];
                    if (!IsAssetDownloaded(button.Image, button, personalizationProperties))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsImageAssetDownloaded(SwrveMessageFormat format, Dictionary<string, string> personalizationProperties)
        {
            var pagesKeys = format.Pages.Keys;
            foreach (long key in pagesKeys)
            {
                SwrveMessagePage page = format.Pages[key];
                for (int ii = 0; ii < page.Images.Count; ii++)
                {
                    SwrveImage image = page.Images[ii];
                    if (!IsAssetDownloaded(image.File, image, personalizationProperties))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsAssetDownloaded(string baseAsset, SwrveWidget widget, Dictionary<string, string> personalizationProperties)
        {
            if (!widget.IsMultiLine)
            {
                bool hasBaseImage = IsAssetDownloaded(baseAsset);
                if (!hasBaseImage && !string.IsNullOrEmpty(widget.DynamicImageUrl))
                {
                    return IsDynamicAssetDownloaded(widget.DynamicImageUrl, personalizationProperties);
                }

                if (!hasBaseImage)
                {
                    SwrveLog.LogInfo("Asset not yet downloaded: " + baseAsset);
                    return false;
                }
            }

            return true;
        }

        private bool IsDynamicAssetDownloaded(string dynamicImageUrl, Dictionary<string, string> personalizationProperties)
        {
            try
            {
                string resolvedUrl = SwrveTextTemplating.Apply(dynamicImageUrl, personalizationProperties);
                byte[] dynamicAssetBytes = System.Text.Encoding.UTF8.GetBytes(resolvedUrl);
                if (IsAssetDownloaded(SwrveHelper.sha1(dynamicAssetBytes)))
                {
                    return true;
                }
                else
                {
                    SwrveLog.LogInfo("Button dynamic asset not yet downloaded: " + resolvedUrl);
                    return false;
                }
            }
            catch (SwrveSDKTextTemplatingException exception)
            {
                SwrveLog.LogWarning("Could not resolve personalization for: " + dynamicImageUrl + " " + exception.ToString());
                return false;
            }
        }

        #region SwrveBaseMessage

        /// <summary>
        /// Check if the message supports the given orientation
        /// </summary>
        /// <returns>
        /// True if there is any format that supports the given orientation.
        /// </returns>
        override public bool SupportsOrientation(SwrveOrientation orientation)
        {
            if (orientation == SwrveOrientation.Both)
            {
                return true;
            }

            return (GetFormat(orientation) != null);
        }

        #endregion
    }
}