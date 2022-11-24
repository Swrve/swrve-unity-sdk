#if __has_include(<SwrveSDKCommon/SwrveCommon.h>)
#import <SwrveSDKCommon/SwrveCommon.h>
#else
#import "SwrveCommon.h"
#endif

void UnitySendMessage(const char *, const char *, const char *);

#if !defined(SWRVE_NO_PUSH)
#import <UserNotifications/UserNotifications.h>
@interface UnitySwrve : NSObject<SwrveCommonDelegate, UNUserNotificationCenterDelegate>
#else
@interface UnitySwrve : NSObject<SwrveCommonDelegate>
#endif

@property(nonatomic, strong) NSString *userId;

+ (UnitySwrve *) sharedInstance;
+ (void)init:(char *)jsonConfig;

- (long)appId;
- (NSString *)apiKey;
- (NSString *)deviceUUID;
- (NSString *)applicationPath;
- (NSString *)sigSuffix;
- (NSString *)appVersion;
- (NSString *)uniqueKey;
- (NSString *)eventsServer;
- (NSString *)contentServer;
- (NSString *)identityServer;
- (NSString *)joined;
- (NSString *)language;
- (void)setTrackingStateStopped:(BOOL)isTrackingStateStopped;

- (NSURL *)batchUrl;
- (int)httpTimeout;

- (NSData *)campaignData:(int)category;

- (int)userUpdate:(NSDictionary *)attributes;
- (void)setPermissionsDelegate:(id<SwrvePermissionsDelegate>)permissionsDelegate;
- (void)shutdown;

/** conversation handling **/
- (void)showConversationFromString:(NSString*) conversation;
- (bool)isConversationDisplaying;
- (int)conversationClosed;
- (void)updateQAUser:(NSString *)qaJson;

#if !defined(SWRVE_NO_PUSH)
+ (BOOL)didReceiveRemoteNotification:(NSDictionary *)userInfo withBackgroundCompletionHandler:(void (^)(UIBackgroundFetchResult, NSDictionary*))completionHandler;
#endif

@end
