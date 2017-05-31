#import "UnitySwrveHelper.h"
#import "UnitySwrveCommon.h"
#import "UnitySwrveCommonMessageController.h"
#import "SwrveBaseConversation.h"
#import "UnitySwrveExternC.h"
#import "SwrvePushConstants.h"

#ifdef __cplusplus
extern "C"
{
#endif

    int _swrveiOSConversationVersion()
    {
        return CONVERSATION_VERSION;
    }

    char* _swrveiOSGetLanguage()
    {
        return [UnitySwrveHelper GetLanguage];
    }

    char* _swrveiOSGetTimeZone()
    {
        return [UnitySwrveHelper GetTimeZone];
    }

    char* _swrveiOSGetAppVersion()
    {
        return [UnitySwrveHelper GetAppVersion];
    }

    char* _swrveiOSUUID()
    {
        return [UnitySwrveHelper GetUUID];
    }

    char* _swrveiOSCarrierName()
    {
        return [UnitySwrveHelper GetCarrierName];
    }

    char* _swrveiOSCarrierIsoCountryCode()
    {
        return [UnitySwrveHelper GetCarrierIsoCountryCode];
    }

    char* _swrveiOSCarrierCode()
    {
        return [UnitySwrveHelper GetCarrierCode];
    }

    char* _swrveiOSLocaleCountry()
    {
        return [UnitySwrveHelper GetLocaleCountry];
    }

    char* _swrveiOSIDFV()
    {
        return [UnitySwrveHelper GetIDFV];
    }

    char* _swrveiOSIDFA()
    {
        return [UnitySwrveHelper GetIDFA];
    }

    void _swrveiOSRegisterForPushNotifications(char* jsonCategorySet)
    {
        return [UnitySwrveHelper RegisterForPushNotifications:[UnitySwrveHelper CStringToNSString:jsonCategorySet]];
    }

    void _swrveiOSInitNative(char* jsonConfig)
    {
        [UnitySwrveCommonDelegate init:jsonConfig];
    }

    void _swrveiOSShowConversation(char* conversation)
    {
        [[UnitySwrveMessageEventHandler alloc] showConversationFromString:[UnitySwrveHelper CStringToNSString:conversation]];
    }

    void _swrveiOSStartLocation()
    {
        [[UnitySwrveCommonDelegate sharedInstance] initLocation];
    }

    void _swrveiOSLocationUserUpdate(char* jsonMap)
    {
        [[UnitySwrveCommonDelegate sharedInstance] LocationUserUpdate:[UnitySwrveHelper CStringToNSString:jsonMap]];
    }

    char* _swrveiOSGetPlotNotifications()
    {
        return [UnitySwrveHelper NSStringCopy:[[UnitySwrveCommonDelegate sharedInstance] GetPlotNotifications]];
    }

    bool _swrveiOSIsSupportedOSVersion()
    {
        return [UnitySwrveHelper IsSupportediOSVersion];
    }

    char* _swrveGetInfluencedDataJson()
    {
        NSDictionary* influencedData = [[NSUserDefaults standardUserDefaults] dictionaryForKey:SwrveInfluenceDataKey];
        NSMutableArray* influenceArrayJson = [[NSMutableArray alloc] init];

        if (influencedData != nil) {
            for (NSString* trackingId in influencedData) {
                id maxInfluenceWindowSeconds = [influencedData objectForKey:trackingId];
                if ([maxInfluenceWindowSeconds isKindOfClass:[NSNumber class]]) {
                    long long maxInfluenceWindowSecondsLong = [maxInfluenceWindowSeconds longLongValue];
                    NSNumber* maxInfluenceWindowMillis = [NSNumber numberWithLongLong:(maxInfluenceWindowSecondsLong*1000)];

                    NSDictionary *influenceJson = [NSDictionary dictionaryWithObjectsAndKeys:
                    trackingId, @"trackingId",
                    maxInfluenceWindowMillis, @"maxInfluencedMillis",
                    nil];
                    [influenceArrayJson addObject:influenceJson];
                }
            }
        }
        NSError *error;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:influenceArrayJson options:0 error:&error];
        NSString* influenceDataJson = @"[]";
        if (jsonData) {
          influenceDataJson = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];

          // Clear influence data
          [[NSUserDefaults standardUserDefaults] removeObjectForKey:SwrveInfluenceDataKey];
        } else {
           NSLog(@"_swrveGetInfluencedData: error: %@", error.localizedDescription);
        }

        return [UnitySwrveHelper NSStringCopy:influenceDataJson];
    }

#ifdef __cplusplus
}
#endif
