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
         #if !defined(SWRVE_NO_PUSH)
            UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
            center.delegate = [UnitySwrve sharedInstance];
         #endif //!defined(SWRVE_NO_PUSH)
    }
    //setSwrvePermissionsDelegate
    return [super application:application didFinishLaunchingWithOptions:launchOptions];
}

@end

IMPL_APP_CONTROLLER_SUBCLASS(SwrveUnityAppControllerSub);
