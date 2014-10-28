#import <CoreTelephony/CTCarrier.h>
#import <CoreTelephony/CTTelephonyNetworkInfo.h>

char* swrveCStringCopy(const char* string)
{
    if (string == NULL)
        return NULL;
    
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}

extern "C"
{
    char* _swrveiOSGetLanguage()
    {
        NSString *preferredLang = [[NSLocale preferredLanguages] objectAtIndex:0];
        return swrveCStringCopy([preferredLang UTF8String]);
    }

    char* _swrveiOSGetTimeZone()
    {
        NSTimeZone* tz = [NSTimeZone localTimeZone];
        NSString* timezone_name = [tz name];
  
        return swrveCStringCopy([timezone_name UTF8String]);    
    }

    char * _swrveiOSGetAppVersion()
    {
        NSString* appVersion = [[[NSBundle mainBundle] infoDictionary] valueForKey:@"CFBundleShortVersionString"];
        if (!appVersion || [appVersion length] == 0) {
            appVersion = [[[NSBundle mainBundle] infoDictionary] valueForKey:@"CFBundleVersion"];
        }
        if (!appVersion || [appVersion length] == 0) {
            appVersion = @"";
        }
        return swrveCStringCopy([appVersion UTF8String]);
    }

    char* _swrveiOSUUID()
    {
        NSString* swrveUUID = [[NSUUID UUID] UUIDString];
        return swrveCStringCopy([swrveUUID UTF8String]);    
    }

    char* _swrveCarrierName()
    {
        CTTelephonyNetworkInfo *netinfo = [[CTTelephonyNetworkInfo alloc] init];
        CTCarrier* carrierInfo = [netinfo subscriberCellularProvider];
        if (carrierInfo != nil) {
            return swrveCStringCopy([[carrier carrierName] UTF8String]);
        }
        return NULL;
    }

    char* _swrveCarrierIsoCountryCode()
    {
        CTTelephonyNetworkInfo *netinfo = [[CTTelephonyNetworkInfo alloc] init];
        CTCarrier* carrierInfo = [netinfo subscriberCellularProvider];
        if (carrierInfo != nil) {
            return swrveCStringCopy([[carrier isoCountryCode] UTF8String]);
        }
        return NULL;
    }

    char* _swrveCarrierCode()
    {
        CTTelephonyNetworkInfo *netinfo = [[CTTelephonyNetworkInfo alloc] init];
        CTCarrier* carrierInfo = [netinfo subscriberCellularProvider];
        if (carrierInfo != nil) {
            NSString* mobileCountryCode = [carrierInfo mobileCountryCode];
            NSString* mobileNetworkCode = [carrierInfo mobileNetworkCode];
            if (mobileCountryCode != nil && mobileNetworkCode != nil) {
                NSMutableString* carrierCode = [[NSMutableString alloc] initWithString:mobileCountryCode];
                return swrveCStringCopy([[carrierCode appendString:mobileNetworkCode] UTF8String]);
            }
        }
        return NULL;
    }

    void _swrveRegisterForPushNotifications() 
    {
        UIApplication* app = [UIApplication sharedApplication];
#ifdef __IPHONE_8_0
#if __IPHONE_OS_VERSION_MIN_REQUIRED < __IPHONE_8_0
    // Check if the new push API is not available
    if (![app respondsToSelector:@selector(registerUserNotificationSettings:)])
    {
        // Use the old API
        [app registerForRemoteNotificationTypes:UIRemoteNotificationTypeAlert | UIRemoteNotificationTypeBadge | UIRemoteNotificationTypeSound];
    }
    else
#endif
    {
        [app registerUserNotificationSettings:[UIUserNotificationSettings settingsForTypes:(UIUserNotificationTypeSound | UIUserNotificationTypeAlert | UIUserNotificationTypeBadge) categories:nil]];
        [app registerForRemoteNotifications];
    }
#else
    // Not building with the latest XCode that contains iOS 8 definitions
    [app registerForRemoteNotificationTypes:UIRemoteNotificationTypeAlert | UIRemoteNotificationTypeBadge | UIRemoteNotificationTypeSound];
#endif
    }
}
