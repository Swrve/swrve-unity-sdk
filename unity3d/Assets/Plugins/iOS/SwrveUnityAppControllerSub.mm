#import "SwrvePlot.h"
#import "UnityAppController.h"

@interface SwrveUnityAppControllerSub : UnityAppController

@end

@implementation SwrveUnityAppControllerSub

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions
{
    return [super application:application didFinishLaunchingWithOptions:launchOptions];
}

- (void)application:(UIApplication*)application didReceiveRemoteNotification:(NSDictionary*)userInfo
{
    UIApplicationState swrveState = [application applicationState];
    
    BOOL swrveInBackground = (swrveState == UIApplicationStateInactive) || (swrveState == UIApplicationStateBackground);
    if (!swrveInBackground) {
        NSMutableDictionary* mutableUserInfo = [userInfo mutableCopy];
        [mutableUserInfo setValue:@"YES" forKey:@"_swrveForeground"];
        userInfo = mutableUserInfo;
    }
}

#if !UNITY_TVOS
- (void)application:(UIApplication*)application didReceiveLocalNotification:(UILocalNotification*)notification
{
#if SWRVE_LOCATION_SDK
    [SwrvePlot handleNotification:notification forApplication:application];
#endif
}
#endif

@end

IMPL_APP_CONTROLLER_SUBCLASS(SwrveUnityAppControllerSub);
