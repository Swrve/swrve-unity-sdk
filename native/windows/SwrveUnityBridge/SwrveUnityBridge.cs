using Swrve;
using Swrve.Conversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace SwrveUnityBridge
{
    public static class SwrveUnityBridge
    {
        static SwrveConversationUI _conversationUI;
        static SwrveCommon SDK;

        public static void ShowConversation(int campaignId, string conversationJson)
        {
            ShowConversation(campaignId, JsonObject.Parse(conversationJson));
        }

        public static void ShowConversation(int campaignId, object conversationJson)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            SDK = new SwrveCommon();
            _conversationUI = new SwrveConversationUI(SDK, false);
            
            SwrveConversation conversation = new SwrveConversation(new SwrveConversationCampaign(), (JsonObject)conversationJson);
            LaunchConversationAsync(conversation);
        }

        public static int GetConversationVersion()
        {
            return SwrveConversation.ConversationVersionSupported;
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
