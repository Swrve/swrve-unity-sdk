#if COCOAPODS
#import <SwrveSDKCommon/SwrveCommon.h>
#else
#import "SwrveCommon.h"
#endif

void UnitySendMessage(const char *, const char *, const char *);

#ifdef SWRVE_LOCATION_SDK
#import "SwrvePlot.h"
@interface UnitySwrveCommonDelegate : NSObject<SwrveCommonDelegate, PlotDelegate>
#else
@interface UnitySwrveCommonDelegate : NSObject<SwrveCommonDelegate>
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

+ (void) silentPushNotificationReceived:(NSDictionary*)userInfo withCompletionHandler:(void (^)(UIBackgroundFetchResult, NSDictionary*))completionHandler;

@end
