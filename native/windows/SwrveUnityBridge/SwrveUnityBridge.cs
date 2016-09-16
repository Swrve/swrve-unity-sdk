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

        public static async Task<string> RegisterForPush (ISwrveCommon sdk)
        {
            SwrvePush push = new SwrvePush (sdk);

            string uri = null;
            // Get the latest uri, this is stored in local settings
            var wasUpdated = await push.UpdateUriAsync ();
            if (wasUpdated) {
                SwrveLog.i ("Uri updated. Sending to swrve");
                var attributes = new Dictionary<string, string> ();
                if (!push.GetStoredUri (out uri)) {
                    SwrveLog.i ("Unable to find the stored uri!");
                }
            }
            return uri;
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
