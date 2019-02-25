#if COCOAPODS
#import <SwrveConversationSDK/SwrveMessageEventHandler.h>
#import <SwrveConversationSDK/SwrveBaseConversation.h>
#else
#import "SwrveMessageEventHandler.h"
#import "SwrveBaseConversation.h"
#endif

@interface UnitySwrveMessageEventHandler : NSObject<SwrveMessageEventHandler>

@property (nonatomic, retain) UIWindow* conversationWindow;

-(SwrveBaseConversation*) conversationFromString:(NSString*)conversation;
-(void) showConversationFromString:(NSString*)conversation;
-(bool) isConversationDisplaying;

@end
