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

#ifdef SWRVE_LOCATION_SDK
#import "SwrvePlot.h"
@interface UnitySwrveCommonDelegate : NSObject<SwrveCommonDelegate, UNUserNotificationCenterDelegate, PlotDelegate>
#else
@interface UnitySwrveCommonDelegate : NSObject<SwrveCommonDelegate, UNUserNotificationCenterDelegate>
#endif

+(UnitySwrveCommonDelegate*) sharedInstance;
+(void) init:(char*)jsonConfig;

-(void) initLocation;
-(void) LocationUserUpdate:(NSString*)jsonMap;
-(NSString*) GetPlotNotifications;

-(long) appId;
-(NSString*) apiKey;

-(NSString*) applicationPath;
-(NSString*) locTag;
-(NSString*) sigSuffix;
-(NSString*) userId;
-(NSString*) appVersion;
-(NSString*) uniqueKey;
-(NSString*) getLocationPath;

-(NSString*) batchUrl;
-(NSString*) eventsServer;

-(NSURL*) getBatchUrl;
-(int) httpTimeout;

-(NSData*) getCampaignData:(int)category;

-(void) setLocationSegmentVersion:(int)version;
-(int) userUpdate:(NSDictionary *)attributes;
-(void) shutdown;

+(BOOL) didReceiveRemoteNotification:(NSDictionary*)userInfo withBackgroundCompletionHandler:(void (^)(UIBackgroundFetchResult, NSDictionary*))completionHandler;

@end
