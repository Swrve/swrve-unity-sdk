#import "UnitySwrve.h"
#import "UnitySwrveHelper.h"

#include <sys/time.h>
#import <CommonCrypto/CommonHMAC.h>

#if !defined(SWRVE_NO_PUSH)
#import "SwrvePush.h"
#import "SwrveNotificationConstants.h"
#endif
#import "SwrveQA.h"
#import "SwrveCampaignInfluence.h"
#import "UnitySwrveCommonMessageController.h"

static UnitySwrve *_swrveSharedUnity = NULL;
static dispatch_once_t sharedInstanceToken = 0;

NSString *const SwrvePushCustomButtonOpenAppIdentiferKey = @"open_app";
NSString *const SwrvePushCustomButtonCampaignIdentifierKey = @"open_campaign";
NSString *const SwrvePushButtonToCampaignIdKey = @"PUSH_BUTTON_TO_CAMPAIGN_ID";
NSString *const SwrvePushUnityDoNotProcessKey = @"SWRVE_UNITY_DO_NOT_PROCESS";
NSString *const SwrveUnityStoreConfigKey = @"storedConfig";

@interface UnitySwrve()

#pragma mark - properties & init

@property(atomic, strong) NSDictionary *configDict;

// An in-memory buffer of messages that are ready to be sent to the Swrve
// server the next time sendQueuedEvents is called.
@property (atomic) NSMutableArray *eventBuffer;

// Count the number of UTF-16 code points stored in buffer
@property (atomic) int eventBufferBytes;

@property (atomic) NSDictionary *deviceInfo;

@property(nonatomic, strong) NSString *appGroupIdentifierCache;

// Apple might call different AppDelegate callbacks that could end up calling the Swrve SDK with the same push payload.
// This would result in bad engagement reports etc. This var is used to check that the same push id can't be processed in sequence.
@property(nonatomic, strong) NSString *lastProcessedPushId;

@property (nonatomic, retain) id <SwrvePermissionsDelegate> internalPermissionsDelegate;

/** this is the eventHandler for the conversations, used for getting state for Unity to use */
@property(nonatomic) UnitySwrveMessageEventHandler *msgEventHandler;

@end

@implementation UnitySwrve
@synthesize configDict;
@synthesize eventBuffer;
@synthesize eventBufferBytes;
@synthesize deviceToken;
@synthesize deviceInfo;
@synthesize userId;
@synthesize msgEventHandler;
@synthesize appGroupIdentifierCache;
@synthesize lastProcessedPushId;
@synthesize internalPermissionsDelegate;

- (id)init {
    self = [super init];
    if(self) {
        [self initBuffer];
    }
    return self;
}

+ (UnitySwrve *)sharedInstance {
    dispatch_once(&sharedInstanceToken, ^{
        _swrveSharedUnity = [[UnitySwrve alloc] init];
        [SwrveCommon addSharedInstance:_swrveSharedUnity];
    });
    return _swrveSharedUnity;
}

- (void) shutdown {
    [SwrveCommon addSharedInstance:nil];
    sharedInstanceToken = 0;
    _swrveSharedUnity = nil;
    msgEventHandler = nil;
}


+ (void)init:(char*)_jsonConfig {
    NSString *jsonConfig = [UnitySwrveHelper CStringToNSString:_jsonConfig];
    NSString *spKey = SwrveUnityStoreConfigKey;
    NSUserDefaults *preferences = [NSUserDefaults standardUserDefaults];
    if((jsonConfig == nil) || (0 == [jsonConfig length])) {
        jsonConfig = [preferences stringForKey:spKey];
    }
    if((jsonConfig == nil) || (0 == [jsonConfig length])) {
        return;
    }
    DebugLog(@"Full config dict: %@", jsonConfig);

    NSError *error = nil;
    NSDictionary *newConfigDict = [NSJSONSerialization JSONObjectWithData:[jsonConfig dataUsingEncoding:NSUTF8StringEncoding] options:NSJSONReadingMutableContainers error:&error];
    if(error == nil) {
        UnitySwrve *swrve = [UnitySwrve sharedInstance];
        swrve.configDict = newConfigDict;
        DebugLog(@"Full config dict: %@", swrve.configDict);

        swrve.deviceInfo = [swrve.configDict objectForKey:@"deviceInfo"];
        [preferences setObject:jsonConfig forKey:spKey];
        [preferences synchronize];
    }
}


#pragma mark - getters & setters

- (NSString *)swrveSDKVersion {
    return [self stringFromConfig:@"sdkVersion"];
}

- (NSString *)stringFromConfig:(NSString *)key {
    return [self.configDict valueForKey:key];
}

- (int)intFromConfig:(NSString *)key {
    return [[self.configDict valueForKey:key] intValue];
}

- (long)longFromConfig:(NSString *)key {
    return [[self.configDict valueForKey:key] longValue];
}

- (NSString *)applicationPath {
    return [self stringFromConfig:@"swrvePath"];
}

- (NSString *)prefabName {
    return [self stringFromConfig:@"prefabName"];
}

- (NSString *)sigSuffix {
    return [self stringFromConfig:@"sigSuffix"];
}

- (NSString *)deviceUUID {
    return [self stringFromConfig:@"deviceUUID"];
}

- (NSString *)deviceId {
    return [self stringFromConfig:@"deviceId"];
}

- (NSString *)userId {
    return [self stringFromConfig:@"userId"];
}

- (void)setUserId:(NSString *) userID {
    if (userId != nil || [userId isEqualToString:@""]) { return; }
    userId = userID;
    [self.configDict setValue:userID forKey:@"userId"];
    // Update as well in our local native storage our new swrve user id.
    [[NSUserDefaults standardUserDefaults] setObject:self.configDict forKey:SwrveUnityStoreConfigKey];
    [[NSUserDefaults standardUserDefaults] synchronize];
}

- (NSString*) userID { return [self userId]; }

- (long) appId {
    return [self longFromConfig:@"appId"];
}

- (long) appID { return [self appId]; }

- (NSString *)apiKey {
    return [self stringFromConfig:@"apiKey"];
}

-(NSString *)appVersion {
    return [self stringFromConfig:@"appVersion"];
}

-(NSString *)uniqueKey {
    return [self stringFromConfig:@"uniqueKey"];
}

-(NSString *)eventsServer {
    return [self stringFromConfig:@"eventsServer"];
}

-(NSString *)contentServer {
    return @""; // added for geo, not supported in unity yet
}

- (NSString *)identityServer {
    return @""; // added for identity, not supported in unity yet
}

- (NSString *)joined {
    return @""; // added for geo, not supported in unity yet
}

- (NSString *)language {
    return @""; // added for geo, not supported in unity yet
}

- (double)flushRefreshDelay {
    // added for geo, not supported in unity yet
    return 0.0;
}

- (NSInteger)nextEventSequenceNumber {
    return 0; // added for geo, not supported in unity yet
}

- (NSString *)sessionToken {
    return @""; // added for geo, not supported in unity yet
}

- (NSURL *)batchUrl {
    NSString *baseUrl = [self stringFromConfig:@"batchUrl"];
    return [NSURL URLWithString:baseUrl relativeToURL:[NSURL URLWithString:[self eventsServer]]];
}

- (int)httpTimeout {
    return [self intFromConfig:@"httpTimeout"];
}

- (BOOL)processPermissionRequest:(NSString *)action {
    DebugLog(@"%@", action);
    return TRUE;
}

- (UInt64)getTime {
    // Get the time since the epoch in seconds
    struct timeval time;
    gettimeofday(&time, NULL);
    return (((UInt64)time.tv_sec) * 1000) + (((UInt64)time.tv_usec) / 1000);
}

#pragma mark - conversation handling

- (void) showConversationFromString:(NSString*) conversation {
    if(self.msgEventHandler == nil){
        self.msgEventHandler = [UnitySwrveMessageEventHandler alloc];
    }
    [self.msgEventHandler showConversationFromString:conversation];
}

- (bool) isConversationDisplaying {
    if(self.msgEventHandler){
        return [self.msgEventHandler isConversationDisplaying];
    }
    return false;
}

- (int) conversationClosed {
    // UnitySendMessage requires the message field occupied or we get EXC_BAD_ACCESS.
    [self sendMessageUp:@"NativeConversationClosed" msg:@"closing native conversation.."];
    return SWRVE_SUCCESS;
}

#pragma mark - event handling & REST

- (int)eventInternal:(NSString *)eventName payload:(NSDictionary *)eventPayload triggerCallback:(bool)triggerCallback {
    if (!eventPayload) {
        eventPayload = [[NSDictionary alloc]init];
    }

    NSMutableDictionary* json = [[NSMutableDictionary alloc] init];
    [json setValue:NullableNSString(eventName) forKey:@"name"];
    [json setValue:eventPayload forKey:@"payload"];

    return [self queueEvent:@"event" data:json triggerCallback:triggerCallback];;
}

- (int)queueEvent:(NSString*)eventType data:(NSMutableDictionary*)eventData triggerCallback:(bool)triggerCallback {
#pragma unused(triggerCallback)
    NSMutableArray* buffer = self.eventBuffer;
    if (buffer) {
        // Add common attributes (if not already present)
        if (![eventData objectForKey:@"type"]) {
            [eventData setValue:eventType forKey:@"type"];
        }
        if (![eventData objectForKey:@"time"]) {
            [eventData setValue:[NSNumber numberWithUnsignedLongLong:[self getTime]] forKey:@"time"];
        }

        // Convert to string
        NSData *json_data = [NSJSONSerialization dataWithJSONObject:eventData options:0 error:nil];
        if (json_data) {
            NSString* json_string = [[NSString alloc] initWithData:json_data encoding:NSUTF8StringEncoding];
            @synchronized (buffer) {
                [self setEventBufferBytes:self.eventBufferBytes + (int)[json_string length]];
                [buffer addObject:json_string];
            }
        }
        [self sendQueuedEvents];
    }
    return SWRVE_SUCCESS;
}

- (void)sendQueuedEvents {
    // Early out if length is zero.
    NSMutableArray* buffer = self.eventBuffer;
    int bytes = self.eventBufferBytes;

    @synchronized (buffer) {
        if ([buffer count] == 0) return;

        // Swap buffers
        [self initBuffer];
    }

    NSString* session_token = [self createSessionToken];
    NSString* array_body = [self copyBufferToJson:buffer];
    NSString* json_string = [self createJSON:session_token events:array_body];

    NSData* json_data = [json_string dataUsingEncoding:NSUTF8StringEncoding];

    [self sendHttpPOSTRequest:[self batchUrl]
                     jsonData:json_data
            completionHandler:^(NSURLResponse* response, NSData* data, NSError* error) {

                if (error){
                    DebugLog(@"Error opening HTTP stream: %@ %@", [error localizedDescription], [error localizedFailureReason]);
                    [self setEventBufferBytes:self.eventBufferBytes + bytes];
                    NSMutableArray* currentBuffer = self.eventBuffer;
                    @synchronized(currentBuffer) {
                        [currentBuffer addObjectsFromArray:buffer];
                    }
                    return;
                }
                else{
                    DebugLog(@"response: %@", response);
                    DebugLog(@"data: %@", data);
                }
            }];
}

- (void)sendHttpPOSTRequest:(NSURL*)url jsonData:(NSData*)json {
    [self sendHttpPOSTRequest:url jsonData:json completionHandler:nil];
}

- (void)sendHttpPOSTRequest:(NSURL*)url jsonData:(NSData*)json completionHandler:(void (^)(NSURLResponse*, NSData*, NSError*))handler {
    NSMutableURLRequest* request = [NSMutableURLRequest requestWithURL:url cachePolicy:NSURLRequestUseProtocolCachePolicy timeoutInterval:[self httpTimeout]];
    [request setHTTPMethod:@"POST"];
    [request setHTTPBody:json];
    [request setValue:@"application/json; charset=utf-8" forHTTPHeaderField:@"Content-Type"];
    [request setValue:[NSString stringWithFormat:@"%lu", (unsigned long)[json length]] forHTTPHeaderField:@"Content-Length"];

    [self sendHttpRequest:request completionHandler:handler];
}

- (void)sendHttpRequest:(NSMutableURLRequest*)request completionHandler:(void (^)(NSURLResponse*, NSData*, NSError*))handler {
    // Add http request performance metrics for any previous requests into the header of this request (see JIRA SWRVE-5067 for more details)
    NSArray* allMetricsToSend;

    if (allMetricsToSend != nil && [allMetricsToSend count] > 0) {
        NSString* fullHeader = [allMetricsToSend componentsJoinedByString:@";"];
        [request addValue:fullHeader forHTTPHeaderField:@"Swrve-Latency-Metrics"];
    }

    NSURLSession *session = [NSURLSession sharedSession];
    NSURLSessionDataTask *task = [session dataTaskWithRequest:request completionHandler:^(NSData *data, NSURLResponse *response, NSError *error) {
        handler(response, data, error);
    }];
    [task resume];
}

- (void)initBuffer {
    [self setEventBuffer:[[NSMutableArray alloc] initWithCapacity:SWRVE_MEMORY_QUEUE_INITIAL_SIZE]];
    [self setEventBufferBytes:0];
}

- (NSString *)createSessionToken {
    // Get the time since the epoch in seconds
    struct timeval time; gettimeofday(&time, NULL);
    const long session_start = time.tv_sec;

    NSString* source = [NSString stringWithFormat:@"%@%ld%@", [self userId], session_start, [self apiKey]];

    NSString* digest = [self createStringWithMD5:source];

    // $session_token = "$app_id=$user_id=$session_start=$md5_hash";
    NSString* session_token = [NSString stringWithFormat:@"%ld=%@=%ld=%@",
                               [self appId],
                               [self userId],
                               session_start,
                               digest];
    return session_token;
}

- (NSString *)createStringWithMD5:(NSString*)source {
#define C "%02x"
#define CCCC C C C C
#define DIGEST_FORMAT CCCC CCCC CCCC CCCC

    NSString *digestFormat = [NSString stringWithFormat:@"%s", DIGEST_FORMAT];

    NSData *buffer = [source dataUsingEncoding:NSUTF8StringEncoding];

    unsigned char digest[CC_MD5_DIGEST_LENGTH] = {0};
    unsigned int length = (unsigned int)[buffer length];
    CC_MD5_CTX context;
    CC_MD5_Init(&context);
    CC_MD5_Update(&context, [buffer bytes], length);
    CC_MD5_Final(digest, &context);

    NSString* result = [NSString stringWithFormat:digestFormat,
                        digest[ 0], digest[ 1], digest[ 2], digest[ 3],
                        digest[ 4], digest[ 5], digest[ 6], digest[ 7],
                        digest[ 8], digest[ 9], digest[10], digest[11],
                        digest[12], digest[13], digest[14], digest[15]];

    return result;
}

// Convert the array of strings into a json array.
// This does not add the square brackets.
- (NSString *)copyBufferToJson:(NSArray *) buffer {
    return [buffer componentsJoinedByString:@",\n"];
}

- (NSString*) createJSON:(NSString *)sessionToken events:(NSString *)rawEvents {
    NSString *eventArray = [NSString stringWithFormat:@"[%@]", rawEvents];
    NSData *bodyData = [eventArray dataUsingEncoding:NSUTF8StringEncoding];
    NSArray* body = [NSJSONSerialization
                     JSONObjectWithData:bodyData
                     options:NSJSONReadingMutableContainers
                     error:nil];

    NSMutableDictionary *jsonPacket = [[NSMutableDictionary alloc] init];
    [jsonPacket setValue:[self userId] forKey:@"user"];
    [jsonPacket setValue:[NSNumber numberWithInt:SWRVE_VERSION] forKey:@"version"];
    [jsonPacket setValue:NullableNSString([self appVersion]) forKey:@"app_version"];
    [jsonPacket setValue:NullableNSString(sessionToken) forKey:@"session_token"];
    [jsonPacket setValue:body forKey:@"data"];

    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:jsonPacket options:0 error:nil];
    NSString *json = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];

    return json;
}

-(int) userUpdate:(NSDictionary *)attributes {
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:attributes options:0 error:nil];
    NSString *json = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];

    [self sendMessageUp:@"UserUpdate" msg:json];

    return SWRVE_SUCCESS;
}

- (void)mergeWithCurrentDeviceInfo:(NSDictionary *)attributes {
    #pragma unused(attributes)
    //to be implemented when user identity
}

- (void)sendMessageUp:(NSString*)method msg:(NSString*)msg {
    UnitySendMessage([UnitySwrveHelper NSStringCopy:[self prefabName]],
                     [UnitySwrveHelper NSStringCopy:method],
                     [UnitySwrveHelper NSStringCopy:msg]);
}


- (void)sendPushResponse:(NSDictionary*)notification {
    #if !defined(SWRVE_NO_PUSH)
    UnitySendRemoteNotification(notification);
    #endif //SWRVE_NO_PUSH
}


- (NSSet *)pushCategories {
    return nil;
}

- (NSSet *)notificationCategories {
    return nil;
}


- (NSDictionary *)readAppGroupConfigJSON {
    NSString *path = [[NSBundle mainBundle] pathForResource:@"appgroupconfig" ofType:@"json"];

    if ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
        NSData *data = [NSData dataWithContentsOfFile:path];
        return [NSJSONSerialization JSONObjectWithData:data options:kNilOptions error:nil];
    }
    return nil;
}

- (NSString *)appGroupIdentifier {
    if (appGroupIdentifierCache == nil) {
        NSDictionary *dict = [self readAppGroupConfigJSON];
        appGroupIdentifierCache = @"";

        if(dict != nil) {
            if (dict[@"appGroupIdentifier"]) {
                appGroupIdentifierCache = dict[@"appGroupIdentifier"];
            } else {
                NSLog(@"Swrve - No App Group Identifier found in Dictionary");
            }
        } else {
            NSLog(@"Swrve - No appgroupconfig.json file available. Please check your postprocess.json file");
        }
    }
    return appGroupIdentifierCache;
}

- (void)sendPushNotificationEngagedEvent:(NSString *)pushId {
    NSString* eventName = [NSString stringWithFormat:@"Swrve.Messages.Push-%@.engaged", pushId];
    [self eventInternal:eventName payload:nil triggerCallback:true];
}

-(NSData*) campaignData:(int)category {
#pragma unused(category)
    return nil;
}

- (void)handleNotificationToCampaign:(NSString *)campaignId {
#pragma unused(campaignId)
}

- (void)setPermissionsDelegate:(id<SwrvePermissionsDelegate>)permissionsDelegate {
    self.internalPermissionsDelegate = permissionsDelegate;
}

- (id<SwrvePermissionsDelegate>)permissionsDelegate {
    return self.internalPermissionsDelegate;
}

#if !defined(SWRVE_NO_PUSH)
#pragma mark - UNUserNotificationCenterDelegate

- (void)userNotificationCenter:(UNUserNotificationCenter *)center willPresentNotification:(UNNotification *)notification withCompletionHandler:(void (^)(UNNotificationPresentationOptions))completionHandler {
#pragma unused(center, notification)
    if(completionHandler) {
        completionHandler(UNNotificationPresentationOptionNone);
    }

}

#ifdef __IPHONE_11_0
- (void)userNotificationCenter:(UNUserNotificationCenter *)center didReceiveNotificationResponse:(UNNotificationResponse *)response withCompletionHandler:(void(^)(void))completionHandler {
#else
- (void)userNotificationCenter:(UNUserNotificationCenter *)center didReceiveNotificationResponse:(UNNotificationResponse *)response withCompletionHandler:(void(^)())completionHandler {
#endif
#pragma unused(center)

    [self pushNotificationResponseReceived:response.actionIdentifier withUserInfo:response.notification.request.content.userInfo];

    if (completionHandler) {
        completionHandler();
    }
}

- (void)pushNotificationResponseReceived:(NSString *)identifier withUserInfo:(NSDictionary *) userInfo {
    id pushIdentifier = [userInfo objectForKey:SwrveNotificationIdentifierKey];
    if (pushIdentifier && ![pushIdentifier isKindOfClass:[NSNull class]]) {
        NSString* pushId = @"-1";
        if ([pushIdentifier isKindOfClass:[NSString class]]) {
            pushId = (NSString*)pushIdentifier;
        }
        else if ([pushIdentifier isKindOfClass:[NSNumber class]]) {
            pushId = [((NSNumber*)pushIdentifier) stringValue];
        }
        else {
            DebugLog(@"Unknown Swrve notification ID class for _p attribute", nil);
            return;
        }

        // Only process this push if we haven't seen it before or its a QA push
        if (lastProcessedPushId == nil || [pushId isEqualToString:@"0"] || ![pushId isEqualToString:lastProcessedPushId]) {
            lastProcessedPushId = pushId;

            // Engagement replaces Influence Data
            NSString *appGroupId = [self appGroupIdentifier];
            [SwrveCampaignInfluence removeInfluenceDataForId:pushId fromAppGroupId:appGroupId];

            if([identifier isEqualToString:SwrveNotificationResponseDefaultActionKey]) {
                // if the user presses the push directly
                id pushDeeplinkRaw = [userInfo objectForKey:SwrveNotificationDeeplinkKey];
                if (pushDeeplinkRaw == nil || ![pushDeeplinkRaw isKindOfClass:[NSString class]]) {
                    // Retrieve old push deeplink for backwards compatibility
                    pushDeeplinkRaw = [userInfo objectForKey:SwrveNotificationDeprecatedDeeplinkKey];
                }
                if ([pushDeeplinkRaw isKindOfClass:[NSString class]]) {
                    NSString* pushDeeplink = (NSString*)pushDeeplinkRaw;
                    [self handlePushDeeplinkString:pushDeeplink];
                }

                // Unity will send the engagement event when the app opens
                [self sendPushResponse:userInfo];
                DebugLog(@"Performed a Direct Press on Swrve notification with ID %@", pushId);
            } else {
                NSDictionary *swrveValues = [userInfo objectForKey:SwrveNotificationContentIdentifierKey];
                NSArray *swrvebuttons = [swrveValues objectForKey:SwrveNotificationButtonListKey];

                if (swrvebuttons != nil && [swrvebuttons count] > 0) {
                    int position = [identifier intValue];

                    NSDictionary *selectedButton = [swrvebuttons objectAtIndex:(NSUInteger)position];
                    NSString *action = [selectedButton objectForKey:SwrveNotificationButtonActionKey];
                    NSString *actionType = [selectedButton objectForKey:SwrveNotificationButtonActionTypeKey];
                    NSString *actionText = [selectedButton objectForKey:SwrveNotificationButtonTitleKey];

                    // Send button click event
                    DebugLog(@"Selected Button:'%@' on Swrve notification with ID %@", identifier, pushId);
                    NSMutableDictionary* actionEvent = [[NSMutableDictionary alloc] init];
                    [actionEvent setValue:pushId forKey:@"id"];
                    [actionEvent setValue:@"push" forKey:@"campaignType"];
                    [actionEvent setValue:@"button_click" forKey:@"actionType"];
                    [actionEvent setValue:identifier forKey:@"contextId"];
                    NSMutableDictionary* eventPayload = [[NSMutableDictionary alloc] init];
                    [eventPayload setValue:actionText forKey:@"buttonText"];
                    [actionEvent setValue:eventPayload forKey:@"payload"];

                    // Create generic campaign for button click
                    (void)[self queueEvent:@"generic_campaign_event" data:actionEvent triggerCallback:NO];
                    
                    [self sendPushNotificationEngagedEvent:pushId];
                    [self sendQueuedEvents];
                    // Now that we've processed the events, we need to fulfill the action on the Unity later
                    
                    // Create a mutable copy and let unity know that the events are already processed
                    NSMutableDictionary *mutableUserInfo = [userInfo mutableCopy];
                    [mutableUserInfo setValue:@"YES" forKey:SwrvePushUnityDoNotProcessKey];
                    
                    // If the action is open app we let Unity know about the push payload
                    if ([actionType isEqualToString:SwrveNotificationCustomButtonUrlIdentiferKey]) {
                        [self handlePushDeeplinkString:action];
                    }else if ([actionType isEqualToString:SwrvePushCustomButtonOpenAppIdentiferKey]) {
                        // This open app action will be ignored as everything has already been done.
                    }else if([actionType isEqualToString:SwrvePushCustomButtonCampaignIdentifierKey]){
                        // Unity will process the new campaign id Key added to the userInfo and the engagement event
                        [mutableUserInfo setValue:action forKey:SwrvePushButtonToCampaignIdKey];
                    }
                    
                    userInfo = mutableUserInfo;
                    // Then send it onto the Unity layer
                    [self sendPushResponse:userInfo];
                } else {
                    DebugLog(@"Receieved a push with an unrecognised identifier %@", identifier);
                }
            }
        } else {
            DebugLog(@"Got Swrve notification with ID %@, ignoring as we already processed it", pushId);
        }
    } else {
        DebugLog(@"Got unidentified notification", nil);
        return;
    }
}

- (void)handlePushDeeplinkString:(NSString *)pushDeeplink {
    NSURL *url = [NSURL URLWithString:pushDeeplink];
    BOOL canOpen = [[SwrveCommon sharedUIApplication] canOpenURL:url];
    if (url != nil && canOpen) {
        DebugLog(@"Action - %@ - handled.  Sending to application as URL", pushDeeplink);
        [self deeplinkReceived:url];
    } else {
        DebugLog(@"Could not process push deeplink - %@", pushDeeplink);
    }
}

+ (BOOL)didReceiveRemoteNotification:(NSDictionary *)userInfo withBackgroundCompletionHandler:(void (^)(UIBackgroundFetchResult, NSDictionary *))completionHandler {
    id silentPushIdentifier = [userInfo objectForKey:SwrveSilentPushIdentifierKey];
    if (silentPushIdentifier && ![silentPushIdentifier isKindOfClass:[NSNull class]]) {
        [self silentPushReceived:userInfo withCompletionHandler:completionHandler];
        // Customer should handle the payload in the completionHandler
        return YES;
    } else {
        id pushIdentifier = [userInfo objectForKey:SwrveNotificationIdentifierKey];
        NSString *authenticatedPush = userInfo[SwrveNotificationAuthenticatedUserKey];
        if (pushIdentifier && ![pushIdentifier isKindOfClass:[NSNull class]] &&
                authenticatedPush && ![authenticatedPush isKindOfClass:[NSNull class]]) {
            [self handleAuthenticatedPushNotification:userInfo];
            return NO;
        }
    }
    // We won't call the completionHandler and the customer should handle it themselves
    return NO;
}

+ (void)silentPushReceived:(NSDictionary *)userInfo withCompletionHandler:(void (^)(UIBackgroundFetchResult, NSDictionary *))completionHandler {
    id pushIdentifier = [userInfo objectForKey:SwrveSilentPushIdentifierKey];
    if (pushIdentifier && ![pushIdentifier isKindOfClass:[NSNull class]]) {
        NSString *pushId = @"-1";
        if ([pushIdentifier isKindOfClass:[NSString class]]) {
            pushId = (NSString *) pushIdentifier;
        } else if ([pushIdentifier isKindOfClass:[NSNumber class]]) {
            pushId = [((NSNumber *) pushIdentifier) stringValue];
        } else {
            DebugLog(@"Unknown Swrve notification ID class for _sp attribute", nil);
            return;
        }

        [SwrveCampaignInfluence saveInfluencedData:userInfo withId:pushId withAppGroupID:nil atDate:[NSDate date]];

        if (completionHandler != nil) {
            // The SDK currently does no fetch operation on its own but will in future releases

            // Obtain the silent push payload and call the customers code
            @try {
                id silentPayloadRaw = [userInfo objectForKey:SwrveSilentPushPayloadKey];
                if (silentPayloadRaw != nil && [silentPayloadRaw isKindOfClass:[NSDictionary class]]) {
                    completionHandler(UIBackgroundFetchResultNoData, (NSDictionary *) silentPayloadRaw);
                } else {
                    completionHandler(UIBackgroundFetchResultNoData, nil);
                }
            } @catch (NSException *exception) {
                DebugLog(@"Could not execute the silent push listener: %@", exception.reason);
            }
        }
        DebugLog(@"Got Swrve silent notification with ID %@", pushId);

    } else {
        DebugLog(@"Got unidentified notification", nil);
    }
}

+ (void)handleAuthenticatedPushNotification:(NSDictionary *)userInfo {
    id pushIdentifier = [userInfo objectForKey:SwrveNotificationIdentifierKey];
    if (pushIdentifier && ![pushIdentifier isKindOfClass:[NSNull class]]) {

        NSString *targetedUserId = userInfo[SwrveNotificationAuthenticatedUserKey];
        if (![targetedUserId isEqualToString:[[UnitySwrve sharedInstance] userId]]) {
            DebugLog(@"Could not handle authenticated notification.");
            return;
        }

        if (@available(iOS 10.0, *)) {
            UNMutableNotificationContent *notification = [[UNMutableNotificationContent alloc] init];
            notification.userInfo = userInfo;
            notification.title = userInfo[SwrveNotificationTitleKey];
            notification.subtitle = userInfo[SwrveNotificationSubtitleKey];
            notification.body = userInfo[SwrveNotificationBodyKey];

            [SwrvePush handleNotificationContent:notification withAppGroupIdentifier:nil
                    withCompletedContentCallback:^(UNMutableNotificationContent *content) {

                        NSString *requestIdentifier = [NSDateFormatter localizedStringFromDate:[NSDate date]
                                                                                     dateStyle:NSDateFormatterShortStyle
                                                                                     timeStyle:NSDateFormatterFullStyle];
                        requestIdentifier = [requestIdentifier stringByAppendingString:[NSString stringWithFormat:@" Id: %@", pushIdentifier]];

                        UNTimeIntervalNotificationTrigger *trigger = [UNTimeIntervalNotificationTrigger triggerWithTimeInterval:0.5 repeats:NO];
                        UNNotificationRequest *request = [UNNotificationRequest requestWithIdentifier:requestIdentifier
                                                                                              content:content
                                                                                              trigger:trigger];

                        [[UNUserNotificationCenter currentNotificationCenter] addNotificationRequest:request withCompletionHandler:^(NSError *_Nullable error) {
                            if (error == nil) {
                                DebugLog(@"Authenticated notification completed correctly");
                            } else {
                                DebugLog(@"Authenticated Notification error %@", error);
                            }
                        }];
                    }];
        }
    }
}
#endif

-(void)deeplinkReceived:(NSURL *)url {
    UIApplication *application = [UIApplication sharedApplication];
    [application openURL:url options:@{} completionHandler:^(BOOL success) {
        DebugLog(@"Opening url [%@] successfully: %d", url, success);
    }];
}

- (void)updateQAUser:(NSString *)qaJson {

    NSError* error = nil;
    NSDictionary* map =
    [NSJSONSerialization JSONObjectWithData:[qaJson dataUsingEncoding:NSUTF8StringEncoding]
                                    options:NSJSONReadingMutableContainers error:&error];
    if (error == nil) {

        [SwrveQA updateQAUser:map];
    }
}

@end
