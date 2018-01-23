#import "UnityAppController.h"
#define WITH_UNITY
#import "UnitySwrveCommon.h"

#ifdef SWRVE_LOCATION_SDK
#import "SwrvePlot.h"
#endif

@interface SwrveUnityAppControllerSub : UnityAppController

@end

@implementation SwrveUnityAppControllerSub

- (BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions
{
    NSLog(@"SwrveUnityAppControllerSub - application didFinishLaunchingWithOptions");

#ifdef SWRVE_LOCATION_SDK
    [UnitySwrveCommonDelegate init:nil];
    UnitySwrveCommonDelegate* unitySwrve = (UnitySwrveCommonDelegate*)[SwrveCommon sharedInstance];
    if(unitySwrve != nil) {
        [SwrvePlot initializeWithLaunchOptions:launchOptions delegate:unitySwrve];
    }
#endif

    if (SYSTEM_VERSION_GREATER_THAN_OR_EQUAL_TO(@"10.0")) {
        UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
        center.delegate = [UnitySwrveCommonDelegate sharedInstance];
    }
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

@end

IMPL_APP_CONTROLLER_SUBCLASS(SwrveUnityAppControllerSub);
