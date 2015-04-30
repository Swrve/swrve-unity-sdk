//
//  ISHPermissionRequestAddressBook.m
//  ISHPermissionKit
//
//  Created by Felix Lamouroux on 02.07.14.
//  Copyright (c) 2014 iosphere GmbH. All rights reserved.
//

#import <AddressBook/AddressBook.h>
#import "ISHPermissionRequestAddressBook.h"
#import "ISHPermissionRequest+Private.h"

@implementation ISHPermissionRequestAddressBook {
    ABAddressBookRef _addressBook;
}

- (ISHPermissionState)permissionState {
    ABAuthorizationStatus status = ABAddressBookGetAuthorizationStatus();
    
    switch (status) {
        case kABAuthorizationStatusAuthorized:
            return ISHPermissionStateAuthorized;
            
        case kABAuthorizationStatusRestricted:
        case kABAuthorizationStatusDenied:
            return ISHPermissionStateDenied;
            
        case kABAuthorizationStatusNotDetermined:
            return [self internalPermissionState];
    }
}

- (void)requestUserPermissionWithCompletionBlock:(ISHPermissionRequestCompletionBlock)completion {
    NSAssert(completion, @"requestUserPermissionWithCompletionBlock requires a completion block");
    ISHPermissionState currentState = self.permissionState;
    
    if (!ISHPermissionStateAllowsUserPrompt(currentState)) {
        completion(self, currentState, nil);
        return;
    }
    
    ABAddressBookRequestAccessWithCompletion(self.addressBook, ^(bool granted, CFErrorRef error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            completion(self, granted ? ISHPermissionStateAuthorized : ISHPermissionStateDenied, (__bridge NSError *)(error));
        });
    });
}

- (ABAddressBookRef)addressBook {
    if (!_addressBook) {
        ABAddressBookRef addressBook = ABAddressBookCreateWithOptions(NULL, NULL);
        
        if (addressBook) {
            [self setAddressBook:CFAutorelease(addressBook)];
        }
    }
    
    return _addressBook;
}

- (void)setAddressBook:(ABAddressBookRef)newAddressBook {
    if (_addressBook != newAddressBook) {
        if (_addressBook) {
            CFRelease(_addressBook);
        }
        
        if (newAddressBook) {
            CFRetain(newAddressBook);
        }
        
        _addressBook = newAddressBook;
    }
}

- (void)dealloc {
    if (_addressBook) {
        CFRelease(_addressBook);
        _addressBook = NULL;
    }
}

@end
