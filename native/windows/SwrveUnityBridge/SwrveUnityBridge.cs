using Swrve;
using Swrve.Conversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace SwrveUnityWindows
{
    public static class SwrveUnityBridge
    {
        static SwrveConversationUI _conversationUI;
        static SwrveCommon SDK;

        public static void ShowConversation(object conversationJson)
        {
            if(conversationJson is string)
            {
                conversationJson = JsonObject.Parse((string)conversationJson);
            }

            SDK = new SwrveCommon();
            _conversationUI = new SwrveConversationUI(SDK, false);
            
            SwrveConversation conversation = new SwrveConversation(new SwrveConversationCampaign(), (JsonObject)conversationJson);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            LaunchConversationAsync(conversation);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public static int GetConversationVersion()
        {
            return SwrveConversation.ConversationVersionSupported;
        }

        public static string GetAppLanguage(string defaultLanguage)
        {
            return SwrveHelper.GetAppLanguage(defaultLanguage);
        }

        public static string GetAppVersion()
        {
            return SwrveHelper.GetAppVersion ();
        }

        static async Task<bool> LaunchConversationAsync(SwrveConversation conversation)
        {
            SwrveLog.i("");
            await _conversationUI.PresentConversation(SDK, conversation);
            return true;
        }

        class SwrveCommon : ISwrveCommon
        {
            public void ConversationWasShownToUser(ISwrveConversationCampaign campaign)
            {
                SwrveLog.i("" + campaign);
            }

            public void EventInternal(string eventName, Dictionary<string, string> payload)
            {
                SwrveLog.i("" + eventName);
            }

            public void PushNotificationWasEngaged(string pushId, Dictionary<string, string> payload)
            {
                SwrveLog.i("" + pushId + ", " + payload);
            }

            public void TriggerConversationClosed(ISwrveConversationCampaign conversationCampaign)
            {
                SwrveLog.i("" + conversationCampaign);
            }

            public void TriggerConversationOpened(ISwrveConversationCampaign conversationCampaign)
            {
                SwrveLog.i("" + conversationCampaign);
            }
        }

        class SwrveConversationCampaign : ISwrveConversationCampaign
        {
            public int Id
            {
                get
                {
                    return 0;
                }
            }
        }


    }
}
