using Swrve;
using Swrve.Conversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Data.Json;
using Windows.System.Profile;

namespace SwrveUnityWindows
{
    public static class SwrveUnityBridge
    {
        private static SwrvePush push;

        public static void ShowConversation(ISwrveCommon sdk, object conversationJson)
        {
            if(conversationJson is string)
            {
                conversationJson = JsonObject.Parse((string)conversationJson);
            }

            string os = AnalyticsInfo.VersionInfo.DeviceFamily.ToLower ();
            SwrveConversationUI conversationUI = new SwrveConversationUI(sdk, os.Contains("desktop"));
            
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
            push = new SwrvePush (sdk);
            await push.UpdateUriAsync ();
            string uri;
            push.GetStoredUri (out uri);
            return uri;
        }

        public static void OnActivated (IActivatedEventArgs args)
        {
            push.OnActivated (args);
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
