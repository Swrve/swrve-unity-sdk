using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SwrveUnity.Helpers;
using SwrveUnity.SwrveUsers;

namespace SwrveUnity
{
public sealed class SwrveMigrationsManager
{
    private ISwrveStorage storage;
    private SwrveProfileManager profileManager;
    private const int swrveSdkCacheVersion = 1; // current Unity SDK cache version expected.
    private const string CurrentSdkCacheVersion = "Swrve.current.sdk.version";
    public SwrveMigrationsManager (ISwrveStorage storage, SwrveProfileManager profileManager)
    {
        this.storage = storage;
        this.profileManager = profileManager;
    }

    public void CheckMigrations ()
    {
        int currentUserSDKVersion = this.GetCurrentCacheVersion();
        if (currentUserSDKVersion == swrveSdkCacheVersion) {
            return;
        }

        switch (currentUserSDKVersion) {
        case 0:
            this.MigrateVersion0();
        goto case 1; // in C# is required to add in the switch section a break, goto, or return. (just added here an example what is expected to be doing in future migrations.)
        case 1:
            //this.MigrateVersion1();
            break;
        }
        storage.Save(CurrentSdkCacheVersion, swrveSdkCacheVersion.ToString());
    }

    private int GetCurrentCacheVersion()
    {
        int currentCacheVersion = 0;
        // try load and return from storage the current user data version.
        if (int.TryParse(storage.Load (CurrentSdkCacheVersion), out currentCacheVersion)) {
            return currentCacheVersion;
        }
        return currentCacheVersion;
    }

    private void MigrateVersion0()
    {
        // Check if is very first run. (if it is, just update the current sdk cache version and avoid all migrations)
        string appInstallTimeSecondsSave = storage.Load ("Swrve_JoinedDate", this.profileManager.userId);
        if (string.IsNullOrEmpty(appInstallTimeSecondsSave)) {
            return;
        }
        storage.Save("Swrve_InitTimeDate", appInstallTimeSecondsSave);
        storage.Remove("Swrve_JoinedDate", this.profileManager.userId);
    }
}
}
