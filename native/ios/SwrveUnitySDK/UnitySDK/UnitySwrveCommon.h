#if COCOAPODS
#import <SwrveSDKCommon/SwrveCommon.h>
#else
#import "SwrveCommon.h"
#endif
#if !defined(SWRVE_NO_PUSH)
#import <UserNotifications/UserNotifications.h>
#endif //!defined(SWRVE_NO_PUSH)

void UnitySendMessage(const char *, const char *, const char *);
void UnitySendRemoteNotification(NSDictionary* notification);

@interface UnitySwrveCommonDelegate : NSObject<SwrveCommonDelegate, UNUserNotificationCenterDelegate>

+(UnitySwrveCommonDelegate*) sharedInstance;
+(void) init:(char*)jsonConfig;

-(long) appId;
-(NSString*) apiKey;

-(NSString*) applicationPath;
-(NSString*) locTag;
-(NSString*) sigSuffix;
-(NSString*) userId;
-(NSString*) appVersion;
-(NSString*) uniqueKey;
-(NSString*) locationPath;
-(NSString*) eventsServer;

-(NSURL*) batchUrl;
-(int) httpTimeout;

-(NSData*) campaignData:(int)category;

-(int) userUpdate:(NSDictionary *)attributes;
-(void) shutdown;

+(BOOL) didReceiveRemoteNotification:(NSDictionary*)userInfo withBackgroundCompletionHandler:(void (^)(UIBackgroundFetchResult, NSDictionary*))completionHandler;

-(void) updateQAUser:(NSString*)qaJson;

@end
