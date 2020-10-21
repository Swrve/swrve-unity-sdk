#import <Foundation/Foundation.h>

@interface UnitySwrveHelper : NSObject

+(char*) CStringCopy:(const char*)string;
+(char*) NSStringCopy:(NSString*)string;
+(NSString*) CStringToNSString:(char*)string;

+(char*) language;
+(char*) timeZone;
+(char*) appVersion;
+(char*) UUID;
+(char*) carrierName;
+(char*) carrierIsoCountryCode;
+(char*) carrierCode;
+(char*) localeCountry;
+(char*) IDFV;
+(char*) IDFA;
+(void) registerForPushNotifications:(NSString*)jsonCategorySet andProvisional:(BOOL)provisional;
+(bool) isSupportediOSVersion;
+(char*) deviceType;
+(char*) platformOS;

+(NSSet*) categoryFromJson:(NSString*)jsonString;
+ (BOOL)isValidIDFA:(NSString *)idfa;

@end
