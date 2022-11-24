#if UNITY_IOS
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;

/// <summary>
/// Integrates the native code required for Conversations support on iOS.
/// </summary>
public class SwrveIOSPostProcess : SwrveCommonBuildComponent
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        try
        {
            if (target == BuildTarget.iOS)
            {
                SwrveLog.Log("SwrveIOSPostProcess");
                CorrectXCodeProject(pathToBuiltProject, true);
                SilentPushNotifications(pathToBuiltProject);
                PermissionsDelegateSupport(pathToBuiltProject);
            }
        }
        catch (Exception exp)
        {
            UnityEngine.Debug.LogError("Swrve could not post process the iOS build: " + exp);
#if UNITY_2018_2_OR_NEWER
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
#endif
        }
    }

    private static void CorrectXCodeProject(string pathToProject, bool writeOut)
    {
        string path = Path.Combine(Path.Combine(pathToProject, "Unity-iPhone.xcodeproj"), "project.pbxproj");
        string xcodeproj = File.ReadAllText(path);

        // 1. Make sure it can run on devices and emus!
        xcodeproj = SetValueOfXCodeGroup("SDKROOT", xcodeproj, "\"iphoneos\"");

        // 2. Enable Objective C exceptions
        xcodeproj = xcodeproj.Replace("GCC_ENABLE_OBJC_EXCEPTIONS = NO;", "GCC_ENABLE_OBJC_EXCEPTIONS = YES;");

        // 3. Remove Android content that gets injected in the XCode project
        xcodeproj = Regex.Replace(xcodeproj, @"^.*Libraries/Plugins/Android/SwrveSDKPushSupport.*$", "", RegexOptions.Multiline);

        // 4. Add required frameworks for Conversations
        PBXProject project = new PBXProject();
        project.ReadFromString(xcodeproj);
#if UNITY_2019_3_OR_NEWER
        string targetGuid = project.GetUnityFrameworkTargetGuid();
#else
        string targetGuid = project.TargetGuidByName ("Unity-iPhone");
#endif
        project.AddFrameworkToProject(targetGuid, "Webkit.framework", true /*weak*/);

        // 6. Add conversations resources to bundle
        if (!AddFolderToProject(project, targetGuid, "Assets/Plugins/iOS/SwrveConversationSDK/Resources", pathToProject, "Libraries/Plugins/iOS/SwrveConversationSDK/Resources"))
        {
            UnityEngine.Debug.LogError("Swrve SDK - Could not find the Conversation resources folder in your project. If you want to use Conversations please contact support@swrve.com");
        }

        // 7. Add the required frameworks for push notifications
        project.AddFrameworkToProject(targetGuid, "UserNotifications.framework", true /*weak*/);
        project.AddFrameworkToProject(targetGuid, "UserNotificationsUI.framework", true /*weak*/);

        // 8. Add framework required for SwrveCommmonSDK for SwrveUtils. It needs CoreTelephony.framework
        project.AddFrameworkToProject(targetGuid, "CoreTelephony.framework", false /*weak*/);

        string appGroupIndentifier = SwrveBuildComponent.GetPostProcessString(SwrveBuildComponent.APP_GROUP_ID_KEY);

        if (string.IsNullOrEmpty(appGroupIndentifier))
        {
            SwrveLog.Log("Swrve iOS Rich Push requires an iOSAppGroupIdentifier set in the postprocess.json file. Without it there will be no influence tracking and potential errors.");
        }
        else
        {
            // 8. Add Extension Target for Push
            project = AddExtensionToProject(project, pathToProject);

            // 9. Add Entitlements to project
            project.AddCapability(targetGuid, PBXCapabilityType.AppGroups, null, false);
            project.AddCapability(targetGuid, PBXCapabilityType.PushNotifications, null, false);
        }

        // Write changes to the Xcode project
        xcodeproj = project.WriteToString();
        if (writeOut)
        {
            File.WriteAllText(path, xcodeproj);
        }
    }

    // Enables Advanced Push Capabilities
    private static PBXProject AddExtensionToProject(PBXProject project, string pathToProject)
    {
        PBXProject proj = project;
#if UNITY_2019_3_OR_NEWER
        string mainTarget = proj.GetUnityMainTargetGuid();
#else
        string mainTarget = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif

        // Add Push files to the extension
        CopyFolder("Assets/Plugins/iOS/SwrvePushExtension", pathToProject + "/SwrvePushExtension");

        string extensionTarget = proj.AddAppExtension(mainTarget, "SwrvePushExtension", PlayerSettings.applicationIdentifier + ".ServiceExtension", "SwrvePushExtension/Info.plist");

        // Ensure Service Files are part of the Build Phases
        proj.AddFile(pathToProject + "/SwrvePushExtension/NotificationService.h", "SwrvePushExtension/NotificationService.h");
        proj.AddFileToBuild(extensionTarget, proj.AddFile(pathToProject + "/SwrvePushExtension/NotificationService.m", "SwrvePushExtension/NotificationService.m"));

        // Add TeamID from Player Settings to project
        proj.SetTeamId(extensionTarget, PlayerSettings.iOS.appleDeveloperTeamID);

        // Add Extension Common
        if (!AddFolderToProject(proj, extensionTarget, "Assets/Plugins/iOS/SwrveSDKCommon", pathToProject, "SwrvePushExtension/SwrveSDKCommon"))
        {
            UnityEngine.Debug.LogError("Swrve SDK - Could not find the Common folder in the extension. If you want to use Rich Push please contact support@swrve.com");
        }

        // Add Frameworks needed for SwrveSDKCommon
        proj.AddFrameworkToProject(extensionTarget, "AudioToolbox.framework", false /*weak*/);
        proj.AddFrameworkToProject(extensionTarget, "AVFoundation.framework", true /*weak*/);
        proj.AddFrameworkToProject(extensionTarget, "CFNetwork.framework", false /*weak*/);
        proj.AddFrameworkToProject(extensionTarget, "CoreGraphics.framework", false /*weak*/);
        proj.AddFrameworkToProject(extensionTarget, "CoreMedia.framework", false /*weak*/);
        proj.AddFrameworkToProject(extensionTarget, "CoreMotion.framework", false /*weak*/);
        proj.AddFrameworkToProject(extensionTarget, "CoreVideo.framework", false /*weak*/);
        proj.AddFrameworkToProject(extensionTarget, "CoreTelephony.framework", false /*weak*/);
        proj.AddFrameworkToProject(extensionTarget, "Foundation.framework", false /*not weak*/);
        proj.AddFrameworkToProject(extensionTarget, "iAd.framework", false /*weak*/);
        proj.AddFrameworkToProject(extensionTarget, "MediaPlayer.framework", true /*weak*/);
        proj.AddFrameworkToProject(extensionTarget, "UIKit.framework", true /*weak*/);
        proj.AddFrameworkToProject(extensionTarget, "AdSupport.framework", false /*not weak*/);

        // Add Notification Frameworks
        proj.AddFrameworkToProject(extensionTarget, "UserNotifications.framework", false /*not weak*/);
        proj.AddFrameworkToProject(extensionTarget, "UserNotificationsUI.framework", false /*not weak*/);

        // Update Build Settings for Compatibility
        proj.AddBuildProperty(extensionTarget, "CLANG_ENABLE_OBJC_ARC", "YES");
        proj.AddBuildProperty(extensionTarget, "IPHONEOS_DEPLOYMENT_TARGET", "10.0");
        proj.AddBuildProperty(extensionTarget, "TARGETED_DEVICE_FAMILY", "1,2");
        proj.AddBuildProperty(extensionTarget, "ARCHS", "$(ARCHS_STANDARD)");

        // Add appgroupconfig.json to XCode project
        string appGroupIndentifier = SwrveBuildComponent.GetPostProcessString(SwrveBuildComponent.APP_GROUP_ID_KEY);
        string appGroupConfig = "appgroupconfig.json";
        // Add the app group config so it can be read at run-time by the main app and the service extension
        SwrveBuildComponent.SetAppGroupConfigKey("ios", Path.Combine(pathToProject + "/SwrvePushExtension", appGroupConfig));
        proj.AddFileToBuild(extensionTarget, proj.AddFile(pathToProject + "/SwrvePushExtension/" + appGroupConfig, "SwrvePushExtension/" + appGroupConfig));
        proj.AddFileToBuild(mainTarget, proj.AddFile(pathToProject + "/SwrvePushExtension/" + appGroupConfig, "SwrvePushExtension/" + appGroupConfig));

        // Edit template entitlements file
        string entitlementContents = File.ReadAllText(pathToProject + "/SwrvePushExtension/SwrvePushExtension.entitlements");
        entitlementContents = entitlementContents.Replace("<string>APP_GROUP_TEMP</string>", "<string>" + appGroupIndentifier + "</string>");
        File.WriteAllText(pathToProject + "/SwrvePushExtension/SwrvePushExtension.entitlements", entitlementContents);

        // Add entitlements file to service extension
        proj.AddFileToBuild(extensionTarget, proj.AddFile(pathToProject + "/SwrvePushExtension/SwrvePushExtension.entitlements", "SwrvePushExtension/SwrvePushExtension.entitlements"));
        proj.AddCapability(extensionTarget, PBXCapabilityType.AppGroups, "SwrvePushExtension/SwrvePushExtension.entitlements", false);

        // Return edited project
        return proj;
    }

    // Enables silent push
    private static void SilentPushNotifications(string path)
    {
        // Apply only if the file SwrveSilentPushListener.h is found
        string fileName = "SwrveSilentPushListener.h";
        string hFilePath = "Assets/Plugins/iOS/" + fileName;
        if (File.Exists(hFilePath))
        {
            // Step 1. Add a silent push code to the AppDelegate
            // Inject our code inside the existing didReceiveRemoteNotification, fetchCompletionHandler
            List<string> allMMFiles = GetAllFiles(path, "*.mm");
            bool appliedChanges = false;

            // Inject before existing "AppController_SendNotificationWithArg(kUnityDidReceiveRemoteNotification, userInfo);"
            string searchText = "AppController_SendNotificationWithArg(kUnityDidReceiveRemoteNotification, userInfo);";
            for (int i = 0; i < allMMFiles.Count; i++)
            {
                string filePath = allMMFiles[i];
                string contents = File.ReadAllText(filePath);
                MatchCollection silentPushMethodMatches = Regex.Matches(contents, "application:(.)*didReceiveRemoteNotification:(.)*fetchCompletionHandler:");

                if (silentPushMethodMatches.Count > 0)
                {
                    bool alreadyApplied = contents.IndexOf("[SwrveSilentPushListener onSilentPush:userInfo]") > 0;
                    if (alreadyApplied)
                    {
                        UnityEngine.Debug.Log("SwrveSDK: Silent push custom code present or already injected, please make sure you are calling the Swrve SDK from: " + filePath);
                        break;
                    }


                    int hookPosition = contents.IndexOf(searchText, silentPushMethodMatches[0].Index);
                    if (hookPosition > 0)
                    {
                        string imports = "#import \"UnitySwrve.h\"\n#import \"SwrveSilentPushListener.h\"\n";
                        string injectedCode = "\t// Inform the Swrve SDK\n" +
                                              "\t[UnitySwrve didReceiveRemoteNotification:userInfo withBackgroundCompletionHandler:^ (UIBackgroundFetchResult fetchResult, NSDictionary* swrvePayload) {\n" +
                                              "\t\t[SwrveSilentPushListener onSilentPush:swrvePayload];\n" +
                                              "\t}];\n\t";
                        contents = imports + contents.Substring(0, hookPosition - 1) + injectedCode + contents.Substring(hookPosition, contents.Length - hookPosition);
                        File.WriteAllText(filePath, contents);
                        UnityEngine.Debug.Log("SwrveSDK: Injected silent push code into the app delegate: " + filePath);
                        appliedChanges = true;
                        break;
                    }
                }
            }

            if (!appliedChanges)
            {
                throw new Exception("SwrveSDK: " + fileName + " could not be injected into the AppDelegate. Contact support@swrve.com");
            }

            // Step 2. Add silent background mode the file Info.plist
            string plistPath = path + "/Info.plist";
            if (File.Exists(plistPath))
            {
                string plistContent = File.ReadAllText(plistPath);
                if (!plistContent.Contains("<key>UIBackgroundModes</key>") && !plistContent.Contains("<string>remote-notification</string>"))
                {
                    File.WriteAllText(plistPath, ReplaceFirst(plistContent, "<dict>", "<dict><key>UIBackgroundModes</key><array><string>remote-notification</string></array>"));
                    UnityEngine.Debug.Log("SwrveSDK: Injected silent UIBackgroundModes mode into Info.plist");
                }
                else
                {
                    UnityEngine.Debug.Log("SwrveSDK: Looks like silent UIBackgroundModes was already present in Info.plist");
                }
            }
        }
    }


    // Enables permissions support
    private static void PermissionsDelegateSupport(string path)
    {
        // Apply only if the file SwrvePermissionsDelegateImp.h is found
        string fileName = "SwrvePermissionsDelegateImp.h";
        string hFilePath = "Assets/Plugins/iOS/" + fileName;
        if (File.Exists(hFilePath))
        {
            // Inject initialisation code to set the permission delegate
            List<string> allMMFiles = GetAllFiles(path, "*.mm");
            bool appliedChanges = false;

            // Find hooks to modify the init code
            string importHook = "//importHookForSwrvePermissionsDelegate";
            string setDelegateHook = "//setSwrvePermissionsDelegate";
            string setDelegateCode = "[[UnitySwrve sharedInstance] setPermissionsDelegate:[[SwrvePermissionsDelegateImp alloc] init:self]];";
            for (int i = 0; i < allMMFiles.Count; i++)
            {
                string filePath = allMMFiles[i];
                string contents = File.ReadAllText(filePath);
                int hookPosition = contents.IndexOf(importHook);
                if (hookPosition > 0)
                {
                    contents = contents.Replace(importHook, "#import \"SwrvePermissionsDelegateImp.h\"");
                    contents = contents.Replace(setDelegateHook, setDelegateCode);
                    File.WriteAllText(filePath, contents);
                    UnityEngine.Debug.Log("SwrveSDK: Injected device permission code into Swrve native initialisation: " + filePath);
                    appliedChanges = true;
                    break;
                }
            }

            if (!appliedChanges)
            {
                throw new Exception("SwrveSDK: " + fileName + " could not be injected the SwrvePermissionsDelegateImp. Contact support@swrve.com");
            }
        }
    }

    private static bool AddFolderToProject(PBXProject project, string targetGuid, string folderToCopy, string pathToProject, string destPath)
    {
        if (!System.IO.Directory.Exists(folderToCopy))
        {
            return false;
        }
        // Create dest folder
        string fullDestPath = Path.Combine(pathToProject, destPath);
        if (!System.IO.Directory.Exists(fullDestPath))
        {
            System.IO.Directory.CreateDirectory(fullDestPath);
        }

        // Copy files in this folder
        string[] files = System.IO.Directory.GetFiles(folderToCopy);
        for (int i = 0; i < files.Length; i++)
        {
            string filePath = files[i];
            if (!filePath.EndsWith(".meta")
                    && !filePath.Contains("SwrveConversation-tvos")
                    && !filePath.Contains("LICENSE"))
            {
                string fileName = System.IO.Path.GetFileName(filePath);
                string newFilePath = Path.Combine(fullDestPath, fileName);

                if (!new System.IO.FileInfo(newFilePath).Exists)
                {   
                    System.IO.File.Copy(filePath, newFilePath);
                }

                // Add to the XCode project
                string relativeProjectPath = Path.Combine(destPath, fileName);
                string resourceGuid = project.AddFile(relativeProjectPath, relativeProjectPath, PBXSourceTree.Source);
                if (filePath.EndsWith(".m"))
                {
                    project.AddFileToBuild(targetGuid, resourceGuid);
                }
            }
        }

        // Copy folders and xcassets
        string[] folders = System.IO.Directory.GetDirectories(folderToCopy);
        for (int i = 0; i < folders.Length; i++)
        {
            string folderPath = folders[i];
            string dirName = System.IO.Path.GetFileName(folderPath);
            if (folderPath.EndsWith(".xcassets"))
            {
                // xcassets is a special case where it is treated as a file
                CopyFolder(folderPath, Path.Combine(fullDestPath, dirName));
                // Add to the XCode project
                string relativeProjectPath = Path.Combine(destPath, dirName);
                string resourceGuid = project.AddFile(relativeProjectPath, relativeProjectPath, PBXSourceTree.Source);
                project.AddFileToBuild(targetGuid, resourceGuid);
            }
            else
            {
                // Recursively copy files
                AddFolderToProject(project, targetGuid, folderPath, pathToProject, Path.Combine(destPath, dirName));
            }
        }

        return (folders.Length != 0 || files.Length != 0);
    }

    private static void CopyFolder(string orig, string dest)
    {
        if (!System.IO.Directory.Exists(dest))
        {
            System.IO.Directory.CreateDirectory(dest);
        }
        string[] files = System.IO.Directory.GetFiles(orig);
        for (int i = 0; i < files.Length; i++)
        {
            string filePath = files[i];
            string fileName = System.IO.Path.GetFileName(filePath);

            string newPath = Path.Combine(dest, fileName);
            if (!new System.IO.FileInfo(newPath).Exists)
            {   
                System.IO.File.Copy(filePath, newPath);
            }
        }
        string[] folders = System.IO.Directory.GetDirectories(orig);
        for (int i = 0; i < folders.Length; i++)
        {
            string folderPath = folders[i];
            string dirName = System.IO.Path.GetFileName(folderPath);
            CopyFolder(folderPath, Path.Combine(dest, dirName));
        }
    }

    private static string SetValueOfXCodeGroup(string grouping, string project, string replacewith)
    {
        string pattern = string.Format(@"{0} = .*;$", grouping);
        string replacement = string.Format(@"{0} = {1};", grouping, replacewith);
        return Regex.Replace(project, pattern, replacement, RegexOptions.Multiline);
    }

    private static List<string> GetAllFiles(string path, string fileExtension)
    {
        List<string> result = new List<string>();
        try
        {
            string[] files = System.IO.Directory.GetFiles(path, fileExtension);
            result.AddRange(files);
            string[] directories = Directory.GetDirectories(path);
            for (int i = 0; i < directories.Length; i++)
            {
                List<string> filesInDir = GetAllFiles(directories[i], fileExtension);
                result.AddRange(filesInDir);
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError(ex);
        }

        return result;
    }

    private static string ReplaceFirst(string str, string needle, string replacement)
    {
        int index = str.IndexOf(needle);
        if (index >= 0)
        {
            return str.Substring(0, index) + replacement + str.Substring(index + needle.Length);
        }
        return str;
    }
}
#endif
