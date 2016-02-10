#import "SwrvePlotDelegate.h"
#import "UnityAppController.h"

@interface SwrveUnityAppControllerSub : UnityAppController

@end

@implementation SwrveUnityAppControllerSub

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions
{
    [SwrvePlotDelegate sharedInstance].launchOptions = launchOptions;
    return [super application:application didFinishLaunchingWithOptions:launchOptions];
}

- (void)application:(UIApplication*)application didReceiveRemoteNotification:(NSDictionary*)userInfo
{
    UIApplicationState swrveState = [application applicationState];
    BOOL swrveInBackground = (swrveState == UIApplicationStateInactive || swrveState == UIApplicationStateBackground);
    if (!swrveInBackground) {
        NSMutableDictionary* mutableUserInfo = [userInfo mutableCopy];
        [mutableUserInfo setValue:@"YES" forKey:@"_swrveForeground"];
        userInfo = mutableUserInfo;
    }
}

#if !UNITY_TVOS
- (void)application:(UIApplication*)application didReceiveLocalNotification:(UILocalNotification*)notification
{
    [[SwrvePlotDelegate sharedInstance] didReceiveLocalNotification:notification];
}
#endif

@end

IMPL_APP_CONTROLLER_SUBCLASS(SwrveUnityAppControllerSub);
