#import "UnityAppController.h"
#define WITH_UNITY
#import "UnitySwrve.h"
//importHookForSwrvePermissionsDelegate

@interface SwrveUnityAppControllerSub : UnityAppController

@end

@implementation SwrveUnityAppControllerSub

- (BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions
{
    NSLog(@"SwrveUnityAppControllerSub - application didFinishLaunchingWithOptions");

    if (SYSTEM_VERSION_GREATER_THAN_OR_EQUAL_TO(@"10.0")) {
        UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
        center.delegate = [UnitySwrve sharedInstance];
    }
    //setSwrvePermissionsDelegate
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
