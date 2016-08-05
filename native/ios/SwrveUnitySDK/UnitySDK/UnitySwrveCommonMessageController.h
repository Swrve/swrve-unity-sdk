#if COCOAPODS
#import <SwrveConversationSDK/SwrveMessageEventHandler.h>
#import <SwrveConversationSDK/SwrveBaseConversation.h>
#else
#import "SwrveMessageEventHandler.h"
#import "SwrveBaseConversation.h"
#endif

@interface UnitySwrveMessageEventHandler : NSObject<SwrveMessageEventHandler>

-(SwrveBaseConversation*) conversationFromString:(NSString*)conversation;
-(void) showConversationFromString:(NSString*)conversation;

@end
