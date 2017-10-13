#import "NotificationService.h"
#import "SwrvePush.h"

@interface NotificationService ()

@property (nonatomic, strong) void (^contentHandler)(UNNotificationContent *contentToDeliver);
@property (nonatomic, strong) UNMutableNotificationContent *bestAttemptContent;
@end

@implementation NotificationService

- (void)didReceiveNotificationRequest:(UNNotificationRequest *)request withContentHandler:(void (^)(UNNotificationContent * _Nonnull))contentHandler {
    self.contentHandler = contentHandler;
    [SwrvePush handleNotificationContent:[request content] withAppGroupIdentifier:[NotificationService appGroupIdentifier] withCompletedContentCallback:^(UNMutableNotificationContent * content) {
        self.bestAttemptContent = content;
        self.contentHandler(self.bestAttemptContent);
    }];
}

- (void)serviceExtensionTimeWillExpire {
    self.contentHandler(self.bestAttemptContent);
}

+(NSDictionary *) readAppGroupConfigJSON {
    NSString *path = [[NSBundle mainBundle] pathForResource:@"appgroupconfig" ofType:@"json"];
    NSData *data = [NSData dataWithContentsOfFile:path];
    return [NSJSONSerialization JSONObjectWithData:data options:kNilOptions error:nil];
}

+(NSString*) appGroupIdentifier {
    NSDictionary *dict = [NotificationService readAppGroupConfigJSON];
    NSString* appgroupID = nil;

    if(dict != nil) {
        if (dict[@"appGroupIdentifier"]) {
            appgroupID = dict[@"appGroupIdentifier"];
        } else {
            NSLog(@"Swrve - No App Group Identifier found in Dictionary");
        }
    } else {
        NSLog(@"Swrve - No appgroupconfig.json file available");
    }
    return appgroupID;
}

@end
