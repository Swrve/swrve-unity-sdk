#import <Foundation/Foundation.h>

@interface UnitySwrveHelper : NSObject

+(char*) CStringCopy:(const char*)string;
+(char*) NSStringCopy:(NSString*)string;
+(NSString*) CStringToNSString:(char*)string;

+(char*) language;
+(char*) timeZone;
+(char*) appVersion;
+(char*) UUID;
+(char*) localeCountry;
+(char*) IDFV;
+(bool) isSupportediOSVersion;
+(char*) deviceType;
+(char*) platformOS;

+(NSSet*) categoryFromJson:(NSString*)jsonString;

@end
