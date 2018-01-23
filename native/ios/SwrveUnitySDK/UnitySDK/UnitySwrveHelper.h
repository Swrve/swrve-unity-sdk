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
+(void) registerForPushNotifications:(NSString*)jsonCategorySet withBackwardsCompatibility:(NSString*)backCompatJsonCategorySet;
+(void) initPlot;
+(bool) isSupportediOSVersion;

+(NSSet*) categoryFromJson:(NSString*)jsonString;
+(NSSet*) preiOS10CategoryFromJson:(NSString*)jsonString;

@end
