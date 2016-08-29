#import "UnitySwrveCommonMessageController.h"
#import "SwrveConversationItemViewController.h"
#import "SwrveConversationsNavigationController.h"
#import "SwrveConversationContainerViewController.h"
#import "SwrveCommon.h"

@class SwrveConversationItemViewController;

@interface UnitySwrveMessageEventHandler()

@property (nonatomic, retain) UIWindow* conversationWindow;
@property (nonatomic, retain) SwrveConversationItemViewController* swrveConversationItemViewController;

@end

@implementation UnitySwrveMessageEventHandler

@synthesize conversationWindow;
@synthesize swrveConversationItemViewController;

-(void)conversationWasShownToUser:(SwrveBaseConversation*)conversation {
#pragma unused(conversation)
    // this is handled on Unity's side
}

- (void) conversationClosed {
    @synchronized(self) {
        self.conversationWindow.hidden = YES;
        self.conversationWindow = nil;
        self.swrveConversationItemViewController = nil;
    }
}

-(SwrveBaseConversation*) conversationFromString:(NSString*)conversation
{
    NSError* jsonError;
    NSDictionary *jsonDict =
        [NSJSONSerialization JSONObjectWithData:[conversation dataUsingEncoding:NSUTF8StringEncoding]
                                        options:0
                                          error:&jsonError];
    if(nil == jsonDict) {
        return nil;
    }
    return [SwrveBaseConversation fromJSON:jsonDict forController:self];
}

-(void) showConversationFromString:(NSString*)conversation
{
    [self showConversation:[self conversationFromString:conversation]];
}

-(void) showConversation:(SwrveBaseConversation*)conversation
{
    @synchronized(self) {
        if ( conversation && self.conversationWindow == nil ) {
            // Create a view to show the conversation
            
            @try {
                UIStoryboard* storyBoard = [SwrveBaseConversation loadStoryboard];
                SwrveConversationItemViewController* scivc = [storyBoard instantiateViewControllerWithIdentifier:@"SwrveConversationItemViewController"];
                self.swrveConversationItemViewController = scivc;
            }
            @catch (NSException *exception) {
                DebugLog(@"Unable to load Conversation Item View Controller. %@", exception);
                return;
            }
            
            self.conversationWindow = [[UIWindow alloc] initWithFrame:[[UIScreen mainScreen] bounds]];
            [self.swrveConversationItemViewController setConversation:conversation
                                                 andMessageController:self];
            
            // Create a navigation controller in which to push the conversation, and choose iPad presentation style
            SwrveConversationsNavigationController *svnc =
                [[SwrveConversationsNavigationController alloc] initWithRootViewController:self.swrveConversationItemViewController];
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wselector"
            // Attach cancel button to the conversation navigation options
            UIBarButtonItem *cancelButton = [[UIBarButtonItem alloc] initWithBarButtonSystemItem:UIBarButtonSystemItemCancel
                                                                                          target:self.swrveConversationItemViewController
                                                                                          action:@selector(cancelButtonTapped:)];
#pragma clang diagnostic pop
            self.swrveConversationItemViewController.navigationItem.leftBarButtonItem = cancelButton;
            
            dispatch_async(dispatch_get_main_queue(), ^{
                SwrveConversationContainerViewController* rootController = [[SwrveConversationContainerViewController alloc] initWithChildViewController:svnc];
                self.conversationWindow.rootViewController = rootController;
                [self.conversationWindow makeKeyAndVisible];
                [self.conversationWindow.rootViewController.view endEditing:YES];
            });
            
        }
    }
}

@end
