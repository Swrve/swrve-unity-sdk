#if COCOAPODS
#import <SwrveMessageEventHandler.h>
#import <SwrveBaseConversation.h>
#else
#import "SwrveMessageEventHandler.h"
#import "SwrveBaseConversation.h"
#endif

@interface UnitySwrveMessageEventHandler : NSObject<SwrveMessageEventHandler>

-(SwrveBaseConversation*) conversationFromString:(NSString*)conversation;
-(void) showConversationFromString:(NSString*)conversation;

@end
