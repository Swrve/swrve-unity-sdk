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
        public static void ShowConversation(ISwrveCommon sdk, object conversationJson)
        {
            if(conversationJson is string)
            {
                conversationJson = JsonObject.Parse((string)conversationJson);
            }
            
            SwrveConversationUI conversationUI = new SwrveConversationUI(sdk, false);
            
            SwrveConversation conversation = new SwrveConversation(new SwrveConversationCampaign(), (JsonObject)conversationJson);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            conversationUI.PresentConversation (sdk, conversation);
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
