#import "UnityAppController.h"
#define WITH_UNITY
#import "UnitySwrveCommon.h"

#ifdef SWRVE_LOCATION_SDK
#import "SwrvePlot.h"
#endif

@interface SwrveUnityAppControllerSub : UnityAppController

@end

@implementation SwrveUnityAppControllerSub

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions
{
#ifdef SWRVE_LOCATION_SDK
    [SwrvePlot initializeWithLaunchOptions:launchOptions delegate:[UnitySwrveCommonDelegate sharedInstance]];
#endif
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
#ifdef SWRVE_LOCATION_SDK
    [SwrvePlot handleNotification:notification forApplication:application];
#endif
}
#endif

@end

IMPL_APP_CONTROLLER_SUBCLASS(SwrveUnityAppControllerSub);
