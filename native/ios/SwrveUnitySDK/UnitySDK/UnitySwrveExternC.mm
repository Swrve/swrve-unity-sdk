#import "UnitySwrveExternC.h"
#import "UnitySwrveCommon.h"
#import "UnitySwrveHelper.h"
#import "UnitySwrveCommonMessageController.h"
#import "SwrveBaseConversation.h"
#import "SwrveCampaignInfluence.h"

#if !defined(SWRVE_NO_PUSH)
#import <UserNotifications/UserNotifications.h>
#import "SwrvePermissions.h"
#endif //!defined(SWRVE_NO_PUSH)

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

    void _swrveiOSRegisterForPushNotifications(char* jsonUNCategorySet, char* jsonUICategorySet)
    {
        return [UnitySwrveHelper registerForPushNotifications:[UnitySwrveHelper CStringToNSString:jsonUNCategorySet] withBackwardsCompatibility:[UnitySwrveHelper CStringToNSString:jsonUICategorySet]];
    }

    void _swrveiOSInitNative(char* jsonConfig)
    {
        [UnitySwrveCommonDelegate init:jsonConfig];
    }

    void _swrveiOSShowConversation(char* conversation)
    {
        [[UnitySwrveMessageEventHandler alloc] showConversationFromString:[UnitySwrveHelper CStringToNSString:conversation]];
    }

    bool _swrveiOSIsSupportedOSVersion()
    {
        return [UnitySwrveHelper isSupportediOSVersion];
    }

    char* _swrveInfluencedDataJson()
    {
        NSMutableArray* influenceArrayJson = [[NSMutableArray alloc] init];

        NSDictionary *influencedData;
        NSDictionary* mainAppInfluence = [[NSUserDefaults standardUserDefaults] dictionaryForKey:SwrveInfluenceDataKey];
        NSUserDefaults* serviceExtensionDefaults = nil;
        NSDictionary* serviceExtensionInfluence = nil;

        if ([UnitySwrveCommonDelegate sharedInstance] != nil && [[UnitySwrveCommonDelegate sharedInstance] appGroupIdentifier] != nil){
            serviceExtensionDefaults = [[NSUserDefaults alloc] initWithSuiteName:[[UnitySwrveCommonDelegate sharedInstance] appGroupIdentifier]];
            serviceExtensionInfluence = [serviceExtensionDefaults dictionaryForKey:SwrveInfluenceDataKey];
        }

        if (mainAppInfluence != nil) {
            influencedData = mainAppInfluence;

        } else if(serviceExtensionInfluence != nil) {
            influencedData = serviceExtensionInfluence;
        }

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
        // This methods will return the current status for old iOS versions or get it and send it to the Unity SDK component asynchronously
        if (SYSTEM_VERSION_GREATER_THAN_OR_EQUAL_TO(@"10.0")) {
            __block NSString* prefabNameStr = [UnitySwrveHelper CStringToNSString:componentName];
            UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
            [center getNotificationSettingsWithCompletionHandler:^(UNNotificationSettings *_Nonnull settings) {
                NSString *pushAuthorizationFromSettings = swrve_permission_status_unknown;
                if (settings.authorizationStatus == UNAuthorizationStatusAuthorized) {
                    pushAuthorizationFromSettings = swrve_permission_status_authorized;
                } else if (settings.authorizationStatus == UNAuthorizationStatusDenied) {
                    pushAuthorizationFromSettings = swrve_permission_status_denied;
                } else if (settings.authorizationStatus == UNAuthorizationStatusNotDetermined) {
                    pushAuthorizationFromSettings = swrve_permission_status_unknown;
                }
#ifdef UNITY_IOS
                UnitySendMessage([UnitySwrveHelper NSStringCopy:prefabNameStr], [UnitySwrveHelper NSStringCopy:@"SetPushNotificationsPermissionStatus"], [UnitySwrveHelper NSStringCopy:pushAuthorizationFromSettings]);
#else
#pragma unused(prefabNameStr)
#endif
            }];
        } else {
            UIUserNotificationType uiUserNotificationType = [[[SwrveCommon sharedUIApplication] currentUserNotificationSettings] types];
            NSString *pushAuthorization = nil;
            if (uiUserNotificationType  & UIUserNotificationTypeAlert){
                // Best guess is that user can receive notifications. No API available for lockscreen and notification center
                pushAuthorization = swrve_permission_status_authorized;
            } else {
                pushAuthorization = swrve_permission_status_denied;
            }
            return [UnitySwrveHelper NSStringCopy:pushAuthorization];
        }
#endif
        return nil;
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
        [[UnitySwrveCommonDelegate sharedInstance] updateQAUser:[UnitySwrveHelper CStringToNSString:jsonMap]];
    }

#ifdef __cplusplus
}
#endif
