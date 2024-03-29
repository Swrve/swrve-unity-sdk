#import "UnitySwrveCommonMessageController.h"
#import "SwrveConversationItemViewController.h"
#import "SwrveCommon.h"
#import "UnitySwrve.h"

@class SwrveConversationItemViewController;

@interface UnitySwrveMessageEventHandler ()

@property(nonatomic, retain) SwrveConversationItemViewController *swrveConversationItemViewController;

@end

@implementation UnitySwrveMessageEventHandler

@synthesize conversationWindow;
@synthesize swrveConversationItemViewController;

- (void)conversationWasShownToUser:(SwrveBaseConversation *)conversation {
#pragma unused(conversation)
    // this is handled on Unity's side
}

- (void)conversationClosed {
    @synchronized (self) {
        self.conversationWindow.hidden = YES;
        self.conversationWindow = nil;
        self.swrveConversationItemViewController = nil;
        [[UnitySwrve sharedInstance] conversationClosed];
    }
}

- (SwrveBaseConversation *)conversationFromString:(NSString *)conversation {
    NSError *jsonError;
    NSDictionary *jsonDict =
            [NSJSONSerialization JSONObjectWithData:[conversation dataUsingEncoding:NSUTF8StringEncoding]
                                            options:0
                                              error:&jsonError];
    if (nil == jsonDict) {
        return nil;
    }
    return [SwrveBaseConversation fromJSON:jsonDict forController:self];
}

- (void)showConversationFromString:(NSString *)conversationJson {
    @synchronized (self) {
        SwrveBaseConversation *conversation = [self conversationFromString:conversationJson];
        if (conversation && self.conversationWindow == nil) {
            self.conversationWindow = [[UIWindow alloc] initWithFrame:[[UIScreen mainScreen] bounds]];
            self.swrveConversationItemViewController = [SwrveConversationItemViewController initConversation];
            bool success = [SwrveConversationItemViewController showConversation:conversation
                                                              withItemController:self.swrveConversationItemViewController
                                                                withEventHandler:(id <SwrveMessageEventHandler>) self
                                                                        inWindow:self.conversationWindow
                                                             withMessageDelegate:nil];
            if (!success) {
                self.conversationWindow = nil;
            }
        }
    }
}

- (bool)isConversationDisplaying {
    return (self.conversationWindow != nil);
}

@end
