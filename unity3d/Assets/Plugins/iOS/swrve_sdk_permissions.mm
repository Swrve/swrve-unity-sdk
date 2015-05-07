#import "ISHPermissionRequest+All.h"
#import "ISHPermissionRequestNotificationsRemote.h"

static ISHPermissionRequest *_locationAlwaysRequest = nil;
static ISHPermissionRequest *_locationWhenInUseRequest = nil;
static ISHPermissionRequest *_photoLibraryRequest = nil;
static ISHPermissionRequest *_cameraRequest = nil;
static ISHPermissionRequest *_contactsRequest = nil;
static ISHPermissionRequest *_remoteNotifications = nil;

extern "C"
{
    ISHPermissionRequest* _swrveLocationAlwaysRequestPermission() {
        if (!_locationAlwaysRequest) {
            _locationAlwaysRequest = [ISHPermissionRequest requestForCategory:ISHPermissionCategoryLocationAlways];
        }
        return _locationAlwaysRequest;
    }

    BOOL _swrveCheckLocationAlwaysPermission() {
        ISHPermissionRequest *r = _swrveLocationAlwaysRequestPermission();
        return ([r permissionState] == ISHPermissionStateAuthorized);
    }

    void _swrveRequestLocationAlwaysPermission() {
        ISHPermissionRequest *r = _swrveLocationAlwaysRequestPermission();
        [r requestUserPermissionWithCompletionBlock:^(ISHPermissionRequest *request, ISHPermissionState state, NSError *error) {
    #pragma unused(request, error)
            UnitySendMessage("SwrveComponent", "RequestLocationAlwaysPermissionListener", (state == ISHPermissionStateAuthorized)? "on" : "off");
        }];
    }

    ISHPermissionRequest* _swrveLocationWhenInUseRequestPermission() {
        if (!_locationWhenInUseRequest) {
            _locationWhenInUseRequest = [ISHPermissionRequest requestForCategory:ISHPermissionCategoryLocationWhenInUse];
        }
        return _locationWhenInUseRequest;
    }

    BOOL _swrveCheckLocationWhenInUsePermission() {
        ISHPermissionRequest *r = _swrveLocationWhenInUseRequestPermission();
        return ([r permissionState] == ISHPermissionStateAuthorized);
    }

    void _swrveRequestLocationWhenInUsePermission() {
        ISHPermissionRequest *r = _swrveLocationWhenInUseRequestPermission();
        [r requestUserPermissionWithCompletionBlock:^(ISHPermissionRequest *request, ISHPermissionState state, NSError *error) {
    #pragma unused(request, error)
            UnitySendMessage("SwrveComponent", "RequestLocationWhenInUsePermissionListener", (state == ISHPermissionStateAuthorized)? "on" : "off");
        }];
    }

    ISHPermissionRequest* _swrvePhotoLibraryRequestPermission() {
        if (!_photoLibraryRequest) {
            _photoLibraryRequest = [ISHPermissionRequest requestForCategory:ISHPermissionCategoryPhotoLibrary];
        }
        return _photoLibraryRequest;
    }

    BOOL _swrveCheckPhotoLibraryPermission() {
        ISHPermissionRequest *r = _swrvePhotoLibraryRequestPermission();
        return ([r permissionState] == ISHPermissionStateAuthorized);
    }

    void _swrveRequestPhotoLibraryPermission() {
        ISHPermissionRequest *r = _swrvePhotoLibraryRequestPermission();
        [r requestUserPermissionWithCompletionBlock:^(ISHPermissionRequest *request, ISHPermissionState state, NSError *error) {
    #pragma unused(request, error)
            UnitySendMessage("SwrveComponent", "RequestPhotoLibraryPermissionListener", (state == ISHPermissionStateAuthorized)? "on" : "off");
        }];
    }

    ISHPermissionRequest* _swrveCameraRequestPermission() {
        if (!_cameraRequest) {
            _cameraRequest = [ISHPermissionRequest requestForCategory:ISHPermissionCategoryPhotoCamera];
        }
        return _cameraRequest;
    }

    BOOL _swrveCheckCameraPermission() {
        ISHPermissionRequest *r = _swrveCameraRequestPermission();
        return ([r permissionState] == ISHPermissionStateAuthorized);
    }

    void _swrveRequestCameraPermission() {
        ISHPermissionRequest *r = _swrveCameraRequestPermission();
        [r requestUserPermissionWithCompletionBlock:^(ISHPermissionRequest *request, ISHPermissionState state, NSError *error) {
    #pragma unused(request, error)
            UnitySendMessage("SwrveComponent", "RequestCameraPermissionListener", (state == ISHPermissionStateAuthorized)? "on" : "off");
        }];
    }

    ISHPermissionRequest* _swrveContactsRequestPermission() {
        if (!_contactsRequest) {
            _contactsRequest = [ISHPermissionRequest requestForCategory:ISHPermissionCategoryAddressBook];
        }
        return _contactsRequest;
    }

    BOOL _swrveCheckContactsPermission() {
        ISHPermissionRequest *r = _swrveContactsRequestPermission();
        return ([r permissionState] == ISHPermissionStateAuthorized);
    }

    void _swrveRequestContactsPermission() {
        ISHPermissionRequest *r = _swrveContactsRequestPermission();
        [r requestUserPermissionWithCompletionBlock:^(ISHPermissionRequest *request, ISHPermissionState state, NSError *error) {
    #pragma unused(request, error)
            UnitySendMessage("SwrveComponent", "RequestContactsPermissionListener", (state == ISHPermissionStateAuthorized)? "on" : "off");
        }];
    }

    ISHPermissionRequest* _swrvePushNotificationsRequestPermission() {
        if (!_remoteNotifications) {
            _remoteNotifications = [ISHPermissionRequest requestForCategory:ISHPermissionCategoryNotificationRemote];
        }
        return _remoteNotifications;
    }

    BOOL _swrveCheckPushNotificationsPermission() {
        ISHPermissionRequest *r = _swrvePushNotificationsRequestPermission();
        return ([r permissionState] == ISHPermissionStateAuthorized);
    }

    void _swrveRequestPushNotificationsPermission() {
        ISHPermissionRequest *r = _swrvePushNotificationsRequestPermission();
#ifdef __IPHONE_8_0
#if __IPHONE_OS_VERSION_MIN_REQUIRED < __IPHONE_8_0
    // Check if the new push API is not available
    UIApplication* app = [UIApplication sharedApplication];
    if ([app respondsToSelector:@selector(registerUserNotificationSettings:)])
#endif
    {
        ((ISHPermissionRequestNotificationsRemote*)r).notificationSettings = [UIUserNotificationSettings settingsForTypes:(UIUserNotificationTypeSound | UIUserNotificationTypeAlert | UIUserNotificationTypeBadge) categories:nil];
    }
#endif
        [r requestUserPermissionWithCompletionBlock:^(ISHPermissionRequest *request, ISHPermissionState state, NSError *error) {
    #pragma unused(request, error)
            UnitySendMessage("SwrveComponent", "RequestPushNotificationsPermissionListener", (state == ISHPermissionStateAuthorized)? "on" : "off");
        }];
    }
}
