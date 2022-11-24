#import "UnitySwrveHelper.h"
#import "UnitySwrve.h"
#import "SwrveUtils.h"
#import "SwrvePermissions.h"

#if TARGET_OS_IOS /* exclude tvOS */
#endif //TARGET_OS_IOS

#if !defined(SWRVE_NO_PUSH)

#import "SwrveNotificationOptions.h"

#endif //!defined(SWRVE_NO_PUSH)

@implementation UnitySwrveHelper

+ (char *)CStringCopy:(const char *)string {
    if (string == NULL)
        return NULL;

    char *res = (char *) malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}

+ (char *)NSStringCopy:(NSString *)string {
    if ([string length] == 0)
        return NULL;
    return [UnitySwrveHelper CStringCopy:[string UTF8String]];
}

+ (NSString *)CStringToNSString:(char *)string {
    if (string == NULL)
        return NULL;

    return [NSString stringWithUTF8String:string];
}

+ (char *)language {
    NSString *preferredLang = [[NSLocale preferredLanguages] objectAtIndex:0];
    return [UnitySwrveHelper NSStringCopy:preferredLang];
}

+ (char *)timeZone {
    NSTimeZone *tz = [NSTimeZone localTimeZone];
    NSString *timezone_name = [tz name];
    return [UnitySwrveHelper NSStringCopy:timezone_name];
}

+ (char *)appVersion {
    NSString *appVersion = [[[NSBundle mainBundle] infoDictionary] valueForKey:@"CFBundleShortVersionString"];
    if (!appVersion || [appVersion length] == 0) {
        appVersion = [[[NSBundle mainBundle] infoDictionary] valueForKey:@"CFBundleVersion"];
    }
    if (!appVersion || [appVersion length] == 0) {
        appVersion = @"";
    }
    return [UnitySwrveHelper NSStringCopy:appVersion];
}

+ (char *)UUID {
    NSString *swrveUUID = [[NSUUID UUID] UUIDString];
    return [UnitySwrveHelper NSStringCopy:swrveUUID];
}

+ (char *)carrierName {
#if TARGET_OS_IOS /** Telephony is only available in iOS **/
    Class telephonyClass = NSClassFromString(@"CTTelephonyNetworkInfo");
    if (telephonyClass) {
        id netinfo = [[telephonyClass alloc] init]; // CTTelephonyNetworkInfo
        id carrierInfo = [netinfo subscriberCellularProvider]; // CTCarrier
        if (carrierInfo != nil) {
            return [UnitySwrveHelper NSStringCopy:[carrierInfo carrierName]];
        }
    }
#endif
    return NULL;
}

+ (char *)carrierIsoCountryCode {
#if TARGET_OS_IOS /** Telephony is only available in iOS **/
    Class telephonyClass = NSClassFromString(@"CTTelephonyNetworkInfo");
    if (telephonyClass) {
        id netinfo = [[telephonyClass alloc] init]; // CTTelephonyNetworkInfo
        id carrierInfo = [netinfo subscriberCellularProvider]; // CTCarrier
        if (carrierInfo != nil) {
            return [UnitySwrveHelper NSStringCopy:[carrierInfo isoCountryCode]];
        }
    }
#endif
    return NULL;
}

+ (char *)carrierCode {
#if TARGET_OS_IOS /** Telephony is only available in iOS **/
    Class telephonyClass = NSClassFromString(@"CTTelephonyNetworkInfo");
    if (telephonyClass) {
        id netinfo = [[telephonyClass alloc] init]; // CTTelephonyNetworkInfo
        id carrierInfo = [netinfo subscriberCellularProvider]; // CTCarrier
        if (carrierInfo != nil) {
            NSString *mobileCountryCode = [carrierInfo mobileCountryCode];
            NSString *mobileNetworkCode = [carrierInfo mobileNetworkCode];
            if (mobileCountryCode != nil && mobileNetworkCode != nil) {
                NSMutableString *carrierCode = [[NSMutableString alloc] initWithString:mobileCountryCode];
                [carrierCode appendString:mobileNetworkCode];
                return [UnitySwrveHelper NSStringCopy:carrierCode];
            }
        }
    }
#endif
    return NULL;
}

+ (char *)localeCountry {
    NSString *localeCountry = [[NSLocale currentLocale] objectForKey:NSLocaleCountryCode];
    return [UnitySwrveHelper NSStringCopy:localeCountry];
}

+ (char *)IDFV {
    NSString *idfv = [[[UIDevice currentDevice] identifierForVendor] UUIDString];
    return [UnitySwrveHelper NSStringCopy:idfv];
}

+ (NSSet *)categoryFromJson:(NSString *)jsonString {
    NSMutableSet *categorySet = [NSMutableSet set];
#if !defined(SWRVE_NO_PUSH)
    NSError *error = nil;
    NSData *jsonData = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
    id jsonObj = [NSJSONSerialization JSONObjectWithData:jsonData options:0 error:&error];
    if (nil == error) {
        for (NSDictionary *categoryDict in jsonObj) {
            NSLog(@"Processing Unity Defined Category: %@", categoryDict);

            NSString *identifier = [categoryDict objectForKey:@"identifier"];
            NSArray *options = [categoryDict objectForKey:@"options"];
            NSMutableArray *actions = [NSMutableArray array];

            for (NSDictionary *actionEntry in [categoryDict objectForKey:@"actions"]) {
                NSString *actionId = [actionEntry objectForKey:@"identifier"];
                NSString *actionTitle = [actionEntry objectForKey:@"title"];
                NSArray *buttonOptions = [actionEntry objectForKey:@"options"];
                UNNotificationAction *actionButton = [UNNotificationAction actionWithIdentifier:actionId title:actionTitle options:[SwrveNotificationOptions actionOptionsForKeys:buttonOptions]];
                [actions addObject:actionButton];
            }

            NSMutableArray *intentIdentifiers = [NSMutableArray array];
            UNNotificationCategory *category = [UNNotificationCategory categoryWithIdentifier:identifier actions:actions intentIdentifiers:intentIdentifiers options:[SwrveNotificationOptions categoryOptionsForKeys:options]];

            [categorySet addObject:category];
        }
    }
#endif

    return categorySet;
}

+ (bool)isSupportediOSVersion {
    return [SwrveCommon supportedOS];
}

+ (char *)deviceType {
    NSString *deviceType = [SwrveUtils platformDeviceType];
    return [UnitySwrveHelper NSStringCopy:deviceType];
}

+ (char *)platformOS {
    UIDevice *device = [UIDevice currentDevice];
    NSString *platformOS = [[device systemName] lowercaseString];
    return [UnitySwrveHelper NSStringCopy:platformOS];
}

@end
