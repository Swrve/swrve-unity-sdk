#import "SwrveMessageEventHandler.h"
#import "SwrveBaseConversation.h"

@class SwrveConversationItemViewController;

@interface UnitySwrveMessageEventHandler : NSObject<SwrveMessageEventHandler>

-(SwrveBaseConversation*) conversationFromString:(NSString*)conversation;
-(void) showConversationFromString:(NSString*)conversation;

@property (nonatomic, retain) SwrveConversationItemViewController* swrveConversationItemViewController;

@end
