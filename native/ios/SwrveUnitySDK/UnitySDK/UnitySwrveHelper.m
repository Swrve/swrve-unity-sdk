#import "UnitySwrveHelper.h"
#import "UnitySwrveCommon.h"
#import "ISHPermissionRequestNotificationsRemote.h"
#import <CoreTelephony/CTTelephonyNetworkInfo.h>
#import <CoreTelephony/CTCarrier.h>
#ifdef SWRVE_LOG_IDFA
#import <AdSupport/ASIdentifierManager.h>
#endif //SWRVE_LOG_IDFA
#if !defined(SWRVE_NO_PUSH)
#import <UserNotifications/UserNotifications.h>
#import "SwrvePushMediaHelper.h"
#endif //!defined(SWRVE_NO_PUSH)

@implementation UnitySwrveHelper

+(char*) CStringCopy:(const char*)string
{
    if (string == NULL)
        return NULL;

    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}

+(char*) NSStringCopy:(NSString*)string
{
    if([string length] == 0)
        return NULL;
    return [UnitySwrveHelper CStringCopy:[string UTF8String]];
}

+(NSString*) CStringToNSString:(char*)string
{
    if(string == NULL)
        return NULL;

    return [NSString stringWithUTF8String: string];
}

+(char*) language
{
    NSString *preferredLang = [[NSLocale preferredLanguages] objectAtIndex:0];
    return [UnitySwrveHelper NSStringCopy:preferredLang];
}

+(char*) timeZone
{
    NSTimeZone* tz = [NSTimeZone localTimeZone];
    NSString* timezone_name = [tz name];
    return [UnitySwrveHelper NSStringCopy:timezone_name];
}

+(char*) appVersion
{
    NSString* appVersion = [[[NSBundle mainBundle] infoDictionary] valueForKey:@"CFBundleShortVersionString"];
    if (!appVersion || [appVersion length] == 0) {
        appVersion = [[[NSBundle mainBundle] infoDictionary] valueForKey:@"CFBundleVersion"];
    }
    if (!appVersion || [appVersion length] == 0) {
        appVersion = @"";
    }
    return [UnitySwrveHelper NSStringCopy:appVersion];
}

+(char*) UUID
{
    NSString* swrveUUID = [[NSUUID UUID] UUIDString];
    return [UnitySwrveHelper NSStringCopy:swrveUUID];
}

+(char*) carrierName
{
    Class telephonyClass = NSClassFromString(@"CTTelephonyNetworkInfo");
    if (telephonyClass) {
        id netinfo = [[telephonyClass alloc] init]; // CTTelephonyNetworkInfo
        id carrierInfo = [netinfo subscriberCellularProvider]; // CTCarrier
        if (carrierInfo != nil) {
            return [UnitySwrveHelper NSStringCopy:[carrierInfo carrierName]];
        }
    }
    return NULL;
}

+(char*) carrierIsoCountryCode
{
    Class telephonyClass = NSClassFromString(@"CTTelephonyNetworkInfo");
    if (telephonyClass) {
        id netinfo = [[telephonyClass alloc] init]; // CTTelephonyNetworkInfo
        id carrierInfo = [netinfo subscriberCellularProvider]; // CTCarrier
        if (carrierInfo != nil) {
            return [UnitySwrveHelper NSStringCopy:[carrierInfo isoCountryCode]];
        }
    }
    return NULL;
}

+(char*) carrierCode
{
    Class telephonyClass = NSClassFromString(@"CTTelephonyNetworkInfo");
    if (telephonyClass) {
        id netinfo = [[telephonyClass alloc] init]; // CTTelephonyNetworkInfo
        id carrierInfo = [netinfo subscriberCellularProvider]; // CTCarrier
        if (carrierInfo != nil) {
            NSString* mobileCountryCode = [carrierInfo mobileCountryCode];
            NSString* mobileNetworkCode = [carrierInfo mobileNetworkCode];
            if (mobileCountryCode != nil && mobileNetworkCode != nil) {
                NSMutableString* carrierCode = [[NSMutableString alloc] initWithString:mobileCountryCode];
                [carrierCode appendString:mobileNetworkCode];
                return [UnitySwrveHelper NSStringCopy:carrierCode];
            }
        }
    }
    return NULL;
}

+(char*) localeCountry
{
    NSString* localeCountry = [[NSLocale currentLocale] objectForKey: NSLocaleCountryCode];
    return [UnitySwrveHelper NSStringCopy:localeCountry];
}

+(char*) IDFV
{
    NSString *idfv = [[[UIDevice currentDevice] identifierForVendor] UUIDString];
    return [UnitySwrveHelper NSStringCopy:idfv];
}

+(char*) IDFA
{
#ifdef SWRVE_LOG_IDFA
    if([[ASIdentifierManager sharedManager] isAdvertisingTrackingEnabled])
    {
        NSString *idfa = [[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString];
        return [UnitySwrveHelper NSStringCopy:idfa];
    }
#endif
    return NULL;
}


+(NSSet*) categoryFromJson:(NSString*)jsonString {
    NSMutableSet* categorySet = [NSMutableSet set];
    NSError* error = nil;
    NSData* jsonData = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    id jsonObj = [NSJSONSerialization JSONObjectWithData:jsonData options:0 error: &error];
    if (nil == error) {
        for (NSDictionary* categoryDict in jsonObj) {
            NSLog(@"Processing Unity Defined Category: %@", categoryDict);

            NSString *identifier = [categoryDict objectForKey:@"identifier"];
            NSArray *options = [categoryDict objectForKey:@"options"];
            NSMutableArray *actions = [NSMutableArray array];

            for (NSDictionary *actionEntry in [categoryDict objectForKey:@"actions"]) {
                NSString *actionId = [actionEntry objectForKey:@"identifier"];
                NSString *actionTitle = [actionEntry objectForKey:@"title"];
                NSArray *buttonOptions = [actionEntry objectForKey:@"options"];
                UNNotificationAction* actionButton = [UNNotificationAction actionWithIdentifier:actionId title:actionTitle options:[SwrvePushMediaHelper actionOptionsForKeys:buttonOptions]];
                [actions addObject:actionButton];
            }

            NSMutableArray *intentIdentifiers = [NSMutableArray array];
            UNNotificationCategory* category = [UNNotificationCategory categoryWithIdentifier:identifier actions:actions intentIdentifiers:intentIdentifiers options:[SwrvePushMediaHelper categoryOptionsForKeys:options]];

            [categorySet addObject:category];
        }
    }

    return categorySet;
}

#ifdef __IPHONE_8_0
+(NSSet*) preiOS10CategoryFromJson:(NSString*)jsonString {
    NSMutableSet* categorySet = nil;

    NSError* error = nil;
    NSData* jsonData = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    id jsonObj = [NSJSONSerialization JSONObjectWithData:jsonData options:0 error: &error];
    if(nil == error)
    {
        for (NSDictionary* categoryDict in jsonObj)
        {
            UIMutableUserNotificationCategory *category =
            [[UIMutableUserNotificationCategory alloc] init];

            NSDictionary* contextActionsDict = (NSDictionary*)[categoryDict valueForKey:@"contextActions"];
            for(NSString* key in contextActionsDict)
            {
                NSMutableArray* actions = [[NSMutableArray alloc] init];
                for(NSDictionary* actionDict in [contextActionsDict objectForKey:key])
                {
                    UIMutableUserNotificationAction *action =
                    [[UIMutableUserNotificationAction alloc] init];

                    action.identifier = [actionDict valueForKey:@"identifier"];
                    action.title = [actionDict valueForKey:@"title"];
                    action.activationMode = (0 == [[actionDict valueForKey:@"activationMode"] intValue] ?
                                             UIUserNotificationActivationModeForeground :
                                             UIUserNotificationActivationModeBackground);

                    if (SYSTEM_VERSION_GREATER_THAN_OR_EQUAL_TO(@"9.0")) {
#if __IPHONE_OS_VERSION_MIN_REQUIRED >= __IPHONE_9_0
                        UIUserNotificationActionBehavior behaviour = (0 == [[actionDict valueForKey:@"behaviour"] intValue] ?
                                                                      UIUserNotificationActionBehaviorDefault :
                                                                      UIUserNotificationActionBehaviorTextInput);
                        action.behavior = behaviour;
#endif
                    }

                    action.destructive = [[actionDict valueForKey:@"destructive"] boolValue];
                    action.authenticationRequired = [[actionDict valueForKey:@"authenticationRequired"] boolValue];

                    [actions addObject:action];
                }

                if(0 == [actions count]) {
                    continue;
                }

                UIUserNotificationActionContext context = (0 == [key intValue] ?
                                                           UIUserNotificationActionContextDefault :
                                                           UIUserNotificationActionContextMinimal);
                category.identifier = [categoryDict valueForKey:@"identifier"];
                [category setActions:actions forContext:context];
            }

            if(nil == categorySet) {
                categorySet = [NSMutableSet set];
            }

            [categorySet addObject:category];
        }
    }

    return categorySet;
}
#endif //defined(__IPHONE_8_0)

+(void) registerForPushNotifications:(NSString*)jsonCategorySet withBackwardsCompatibility:(NSString*)backCompatJsonCategorySet {
    UIApplication* app = [UIApplication sharedApplication];

    if (SYSTEM_VERSION_GREATER_THAN_OR_EQUAL_TO(@"8.0")) {
        NSSet* backCompatPushCategories = [self preiOS10CategoryFromJson:backCompatJsonCategorySet];
        UIUserNotificationSettings* settings = [UIUserNotificationSettings settingsForTypes:(UIUserNotificationTypeSound | UIUserNotificationTypeAlert | UIUserNotificationTypeBadge) categories:backCompatPushCategories];
        if (SYSTEM_VERSION_GREATER_THAN_OR_EQUAL_TO(@"10.0")) {
            NSSet* pushCategories = [self categoryFromJson:jsonCategorySet];
            [ISHPermissionRequestNotificationsRemote registerForRemoteNotifications:(UNAuthorizationOptionAlert + UNAuthorizationOptionSound + UNAuthorizationOptionBadge) withCategories:pushCategories andBackwardsCompatibility:settings];

        } else {
            // Perform >iOS10 registration
            [app registerUserNotificationSettings:settings];
            [app registerForRemoteNotifications];
        }

    } else {
#if defined(__IPHONE_8_0)
#else
        // Since we no longer support pre-iOS8 builds, this has to be excluded from compilation, here in case
        [app registerForRemoteNotificationTypes:UIRemoteNotificationTypeAlert | UIRemoteNotificationTypeBadge | UIRemoteNotificationTypeSound | UIRemoteNotificationTypeNewsstandContentAvailability];
#endif //defined(__IPHONE_8_0)
    }
}

+(void) initPlot
{
    [[UnitySwrveCommonDelegate sharedInstance] initLocation];
}

+ (bool) isSupportediOSVersion {
    return [SwrveCommon supportedOS];
}

@end
