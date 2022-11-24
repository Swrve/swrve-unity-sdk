using UnityEngine;
using System;
using System.Collections.Generic;
using SwrveUnity.Helpers;
using System.IO;
using SwrveUnityMiniJSON;

namespace SwrveUnity.Messaging
{
    public class SwrveMessageCenterDetails
    {
        public String Subject;

        public String Description;

        public Texture2D Image;

        public String ImageSha;

        public String ImageUrl;

        public String ImageAccessibilityText;

        public static SwrveMessageCenterDetails LoadFromJSON(Dictionary<string, object> messageData)
        {
            SwrveMessageCenterDetails swrveMessageCenterDetails = new SwrveMessageCenterDetails();

            if (messageData.ContainsKey("subject"))
            {
                swrveMessageCenterDetails.Subject = MiniJsonHelper.GetString(messageData, "subject");
            }

            if (messageData.ContainsKey("description"))
            {
                swrveMessageCenterDetails.Description = MiniJsonHelper.GetString(messageData, "description");
            }

            if (messageData.ContainsKey("image_asset"))
            {
                swrveMessageCenterDetails.ImageSha = MiniJsonHelper.GetString(messageData, "image_asset");
            }

            if (messageData.ContainsKey("dynamic_image_url"))
            {
                swrveMessageCenterDetails.ImageUrl = MiniJsonHelper.GetString(messageData, "dynamic_image_url");
            }

            if (messageData.ContainsKey("accessibility_text"))
            {
                swrveMessageCenterDetails.ImageAccessibilityText = MiniJsonHelper.GetString(messageData, "accessibility_text");
            }

            return swrveMessageCenterDetails;
        }
    }
}
