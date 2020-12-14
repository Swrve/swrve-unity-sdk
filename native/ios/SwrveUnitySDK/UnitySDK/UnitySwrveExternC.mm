#import "UnitySwrveExternC.h"
#import "UnitySwrve.h"
#import "UnitySwrveHelper.h"
#import "SwrveBaseConversation.h"

#if !defined(SWRVE_NO_PUSH)
#import <UserNotifications/UserNotifications.h>
#endif //!defined(SWRVE_NO_PUSH)

#import "SwrveNotificationManager.h"
#import "SwrvePermissions.h"
#import "SwrveCampaignInfluence.h"
#import "SwrveCampaignDelivery.h"
#import "SwrveQA.h"

#ifdef __cplusplus
extern "C"
{
#endif

    int _swrveiOSConversationVersion()
    {
        return CONVERSATION_VERSION;
    }

    char* _swrveiOSLanguage()
    {
        return [UnitySwrveHelper language];
    }
    
   void _swrveUserId(char* userId)
   {
       [[UnitySwrve sharedInstance] setUserId:[UnitySwrveHelper CStringToNSString:userId]];
   }

    char* _swrveiOSTimeZone()
    {
        return [UnitySwrveHelper timeZone];
    }

    char* _swrveiOSAppVersion()
    {
        return [UnitySwrveHelper appVersion];
    }

    char* _swrveiOSUUID()
    {
        return [UnitySwrveHelper UUID];
    }

    char* _swrveiOSCarrierName()
    {
        return [UnitySwrveHelper carrierName];
    }

    char* _swrveiOSCarrierIsoCountryCode()
    {
        return [UnitySwrveHelper carrierIsoCountryCode];
    }

    char* _swrveiOSCarrierCode()
    {
        return [UnitySwrveHelper carrierCode];
    }

    char* _swrveiOSLocaleCountry()
    {
        return [UnitySwrveHelper localeCountry];
    }

    char* _swrveiOSIDFV()
    {
        return [UnitySwrveHelper IDFV];
    }

    char* _swrveiOSIDFA()
    {
        return [UnitySwrveHelper IDFA];
    }


    void _swrveiOSRegisterForPushNotifications(char* jsonUNCategorySet, bool provisional)
    {
        #if !defined(SWRVE_NO_PUSH)
        return [UnitySwrveHelper registerForPushNotifications:[UnitySwrveHelper CStringToNSString:jsonUNCategorySet] andProvisional:provisional];
        #endif
    }

    void _swrveiOSInitNative(char* jsonConfig)
    {
        [UnitySwrve init:jsonConfig];
    }

    void _swrveiOSShowConversation(char* conversation)
    {
        [[UnitySwrve sharedInstance] showConversationFromString:[UnitySwrveHelper CStringToNSString:conversation]];
    }

    bool _swrveiOSIsSupportedOSVersion()
    {
        return [UnitySwrveHelper isSupportediOSVersion];
    }
    
    char* _swrveiOSGetOSDeviceType()
    {
        return [UnitySwrveHelper deviceType];
    }

    char* _swrveiOSGetPlatformOS()
    {
        return [UnitySwrveHelper platformOS];
    }

    bool _swrveiOSIsConversationDisplaying()
    {
        return [[UnitySwrve sharedInstance] isConversationDisplaying];
    }

    char* _swrveInfluencedDataJson()
    {
        NSMutableArray *influenceArrayJson = [NSMutableArray new];
        NSDictionary *influencedData;
        NSDictionary *mainAppInfluence = [[NSUserDefaults standardUserDefaults] dictionaryForKey:SwrveInfluenceDataKey];
        NSUserDefaults *serviceExtensionDefaults = nil;
        NSDictionary *serviceExtensionInfluence = nil;

        if ([UnitySwrve sharedInstance] != nil && [[UnitySwrve sharedInstance] appGroupIdentifier] != nil){
            serviceExtensionDefaults = [[NSUserDefaults alloc] initWithSuiteName:[[UnitySwrve sharedInstance] appGroupIdentifier]];
            serviceExtensionInfluence = [serviceExtensionDefaults dictionaryForKey:SwrveInfluenceDataKey];
        }

        if (mainAppInfluence != nil) {
            influencedData = mainAppInfluence;

        } else if(serviceExtensionInfluence != nil) {
            influencedData = serviceExtensionInfluence;
        }

        if (influencedData != nil) {
            for (NSString *trackingId in influencedData) {
                // Read details about the influenced item to be queued.
                NSDictionary *influenceItem = [influencedData objectForKey:trackingId];
                id maxInfluenceWindowSeconds = [influenceItem objectForKey:@"maxInfluencedMillis"];
                BOOL isSilentPush = [[influenceItem objectForKey:@"silent"] boolValue];

                if ([maxInfluenceWindowSeconds isKindOfClass:[NSNumber class]]) {
                    long long maxInfluenceWindowSecondsLong = [maxInfluenceWindowSeconds longLongValue];
                    NSNumber *maxInfluenceWindowMillis = [NSNumber numberWithLongLong:(maxInfluenceWindowSecondsLong*1000)];

                    NSDictionary *influenceJson = [NSDictionary dictionaryWithObjectsAndKeys:
                                                   trackingId, @"trackingId",
                                                   [NSNumber numberWithBool: isSilentPush], @"silent",
                                                   maxInfluenceWindowMillis, @"maxInfluencedMillis",
                                                   nil];
                    [influenceArrayJson addObject:influenceJson];
                }
            }
        }
        NSError *error;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:influenceArrayJson options:0 error:&error];
        NSString *influenceDataJson = @"[]";
        if (jsonData) {
            influenceDataJson = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];

            // Clear influence data
            if (mainAppInfluence != nil) {
                [[NSUserDefaults standardUserDefaults] removeObjectForKey:SwrveInfluenceDataKey];
            }

            if (serviceExtensionInfluence != nil) {
                [serviceExtensionDefaults removeObjectForKey:SwrveInfluenceDataKey];
                [serviceExtensionDefaults synchronize];
            }
        } else {
            NSLog(@"_swrveInfluencedData: error: %@", error.localizedDescription);
        }

        return [UnitySwrveHelper NSStringCopy:influenceDataJson];
    }

    char* _swrvePushNotificationStatus(char* componentName)
    {
#if !defined(SWRVE_NO_PUSH)
        // This methods will return the current status on cache or and it and send it to the Unity SDK component asynchronously
        __block NSString *prefabNameStr = [UnitySwrveHelper CStringToNSString:componentName];
        NSString *pushAuthorizationFromSettings = [SwrvePermissions pushAuthorizationWithSDK:[UnitySwrve sharedInstance] WithCallback:^(NSString * _Nonnull pushAuthorization) {

#ifdef UNITY_IOS
            UnitySendMessage([UnitySwrveHelper NSStringCopy:prefabNameStr], [UnitySwrveHelper NSStringCopy:@"SetPushNotificationsPermissionStatus"], [UnitySwrveHelper NSStringCopy:pushAuthorization]);
#else
#pragma unused(prefabNameStr, pushAuthorization)
#endif
        }];
        return [UnitySwrveHelper NSStringCopy:pushAuthorizationFromSettings];
#endif
        return [UnitySwrveHelper NSStringCopy:swrve_permission_status_unsupported];
    }

    void _saveConfigForPushDelivery() {
        [SwrveCampaignDelivery saveConfigForPushDeliveryWithUserId:[[UnitySwrve sharedInstance] userId]
                                                WithEventServerUrl:[[UnitySwrve sharedInstance] eventsServer]
                                                      WithDeviceId:[[UnitySwrve sharedInstance] deviceUUID]
                                                  WithSessionToken:[[UnitySwrve sharedInstance] sessionToken]
                                                    WithAppVersion:[[UnitySwrve sharedInstance] appVersion]
                                                     ForAppGroupID:[[UnitySwrve sharedInstance] appGroupIdentifier]
                                                          isQAUser:[[SwrveQA sharedInstance] isQALogging]];
    }

    void _clearAllAuthenticatedNotifications(void)
    {
#if !defined(SWRVE_NO_PUSH)
        [SwrveNotificationManager clearAllAuthenticatedNotifications];
#endif
    }

    char* _swrveBackgroundRefreshStatus()
    {
        NSString *backgroundRefreshStatus = nil;
#if !defined(SWRVE_NO_PUSH)
        backgroundRefreshStatus = swrve_permission_status_unknown;
        UIBackgroundRefreshStatus uiBackgroundRefreshStatus = [[SwrveCommon sharedUIApplication] backgroundRefreshStatus];
        if (uiBackgroundRefreshStatus == UIBackgroundRefreshStatusAvailable) {
            backgroundRefreshStatus = swrve_permission_status_authorized;
        } else if (uiBackgroundRefreshStatus == UIBackgroundRefreshStatusDenied) {
            backgroundRefreshStatus = swrve_permission_status_denied;
        } else if (uiBackgroundRefreshStatus == UIBackgroundRefreshStatusRestricted) {
            backgroundRefreshStatus = swrve_permission_status_unknown;
        }
#endif
        return [UnitySwrveHelper NSStringCopy:backgroundRefreshStatus];
    }

    void _swrveiOSUpdateQaUser(char* jsonMap)
    {
        [[UnitySwrve sharedInstance] updateQAUser:[UnitySwrveHelper CStringToNSString:jsonMap]];
    }

    void _swrveCopyToClipboard(char* content) {
#if TARGET_OS_IOS /** exclude tvOS **/
        NSString *contentStr = [UnitySwrveHelper CStringToNSString:content];
        UIPasteboard *pb = [UIPasteboard generalPasteboard];
        [pb setString:contentStr];
#endif
    }

#ifdef __cplusplus
}
#endif
