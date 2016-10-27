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
        private static SwrvePush _push;

        public static void ShowConversation(ISwrveCommon sdk, object conversationJson)
        {
            if(conversationJson is string)
            {
                conversationJson = JsonObject.Parse((string)conversationJson);
            }
            else if(!(conversationJson is JsonObject))
            {
                SwrveLog.e(string.Format("Unable to handle object of type {0} for ShowConversation", (conversationJson == null ? "null" : "" + conversationJson.GetType()) ));
                return;
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

        private static SwrvePush GetPush()
        {
            if(_push == null)
            {
                _push = new SwrvePush();
            }
            return _push;
        }

        public static async Task<string> RegisterForPush (ISwrveCommon sdk)
        {
            SwrvePush push = GetPush ();
            push.SetCommonSDK (sdk);
            await push.UpdateUriAsync ();
            string uri;
            push.GetStoredUri (out uri);
            return uri;
        }

        public static void OnActivated (IActivatedEventArgs args)
        {
            GetPush().OnActivated (args);
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
