#if UNITY_IPHONE
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

/// <summary>
/// Integrates the native code required for Conversations and Location campaigns support on iOS.
/// </summary>
public class SwrveIOSPostProcess : SwrveCommonBuildComponent
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
#if UNITY_5
        if (target == BuildTarget.iOS)
#else
        if(target == BuildTarget.iPhone)
#endif
        {
            SwrveLog.Log("SwrveIOSPostProcess");
            CorrectXCodeProject (pathToBuiltProject, true);
            SilentPushNotifications (pathToBuiltProject);
        }
    }

    private static void CorrectXCodeProject (string pathToProject, bool writeOut)
    {
        string path = Path.Combine (Path.Combine (pathToProject, "Unity-iPhone.xcodeproj"), "project.pbxproj");
        string xcodeproj = File.ReadAllText (path);

        // 1. Make sure it can run on devices and emus!
        xcodeproj = SetValueOfXCodeGroup ("SDKROOT", xcodeproj, "\"iphoneos\"");

        // 2. Enable Objective C exceptions
        xcodeproj = xcodeproj.Replace("GCC_ENABLE_OBJC_EXCEPTIONS = NO;", "GCC_ENABLE_OBJC_EXCEPTIONS = YES;");

        // 3. Remove Android content that gets injected in the XCode project
        xcodeproj = Regex.Replace (xcodeproj, @"^.*Libraries/Plugins/Android/SwrveSDKPushSupport.*$", "", RegexOptions.Multiline);

        // 4. Add required framewroks for Conversations
        PBXProject project = new PBXProject();
        project.ReadFromString (xcodeproj);
        string targetGuid = project.TargetGuidByName ("Unity-iPhone");
        project.AddFrameworkToProject (targetGuid, "AddressBook.framework", false);
        project.AddFrameworkToProject (targetGuid, "AssetsLibrary.framework", false);
        project.AddFrameworkToProject (targetGuid, "AdSupport.framework", false);
        project.AddFrameworkToProject (targetGuid, "Contacts.framework", true);
        project.AddFrameworkToProject (targetGuid, "Photos.framework", true);

        // 5. Add conversations resources to bundle
        if (!AddFolderToProject (project, targetGuid, "Assets/Plugins/iOS/SwrveConversationSDK/Resources", pathToProject, "Libraries/Plugins/iOS/SwrveConversationSDK/Resources")) {
            UnityEngine.Debug.LogError ("Swrve SDK - Could not find the Conversation resources folder in your project. If you want to use Conversations please contact support@swrve.com");
        }
        xcodeproj = project.WriteToString ();

        // Write changes to the Xcode project
        if (writeOut) {
            File.WriteAllText (path, xcodeproj);
        }
    }

    // Enables silent push
    private static void SilentPushNotifications(string path)
    {
        // Apply only if the file SwrveSilentPushListener.mm is found
        string fileName = "SwrveSilentPushListener.mm";
        string mmFilePath = "Assets/Plugins/iOS/" + fileName;
        if (File.Exists (mmFilePath)) {
            // Step 1. Add a silent push code to the AppDelegate
            // Inject our code inside the existing didReceiveRemoteNotification, fetchCompletionHandler
            List<string> allMMFiles = GetAllFiles (path, "*.mm");
            bool appliedChanges = false;

            // Inject before existing "UnitySendRemoteNotification(userInfo);"
            string searchText = "UnitySendRemoteNotification(userInfo);";
            for (int i = 0; i < allMMFiles.Count; i++) {
                string filePath = allMMFiles[i];
                string contents = File.ReadAllText (filePath);
                MatchCollection silentPushMethodMatches = Regex.Matches(contents, "application:(.)*didReceiveRemoteNotification:(.)*fetchCompletionHandler:");

                if (silentPushMethodMatches.Count > 0) {
                    bool alreadyApplied = contents.IndexOf("[SwrveSilentPushListener onSilentPush:userInfo]") > 0;
                    if (alreadyApplied) {
                        UnityEngine.Debug.Log("SwrveSDK: Silent push custom code present or already injected, please make sure you are calling the Swrve SDK from: " + filePath);
                        break;
                    }


                    int hookPosition = contents.IndexOf (searchText, silentPushMethodMatches[0].Index);
                    if (hookPosition > 0) {
                        string imports = "#import \"UnitySwrveCommon.h\"\n#import \"SwrveSilentPushListener.h\"\n";
                        string injectedCode = "\t// Inform the Swrve SDK\n" +
                                              "\t[UnitySwrveCommonDelegate silentPushNotificationReceived:userInfo withCompletionHandler:^ (UIBackgroundFetchResult fetchResult, NSDictionary* swrvePayload) {\n" +
                                              "\t\t[SwrveSilentPushListener onSilentPush:swrvePayload];\n" +
                                              "\t}];\n\t";
                        contents = imports + contents.Substring (0, hookPosition - 1) + injectedCode + contents.Substring (hookPosition, contents.Length - hookPosition);
                        File.WriteAllText (filePath, contents);
                        UnityEngine.Debug.Log("SwrveSDK: Injected silent push code into the app delegate: " + filePath);
                        appliedChanges = true;
                        break;
                    }
                }
            }

            if (!appliedChanges) {
                throw new Exception("SwrveSDK: " + fileName + " could not be injected into the AppDelegate. Contact support@swrve.com");
            }

            // Step 2. Add silent background mode the file Info.plist
            string plistPath = path + "/Info.plist";
            if (File.Exists (plistPath)) {
                string plistContent = File.ReadAllText (plistPath);
                if (!plistContent.Contains("<key>UIBackgroundModes</key>") && !plistContent.Contains("<string>remote-notification</string>")) {
                    File.WriteAllText(plistPath, ReplaceFirst(plistContent, "<dict>", "<dict><key>UIBackgroundModes</key><array><string>remote-notification</string></array>"));
                    UnityEngine.Debug.Log("SwrveSDK: Injected silent UIBackgroundModes mode into Info.plist");
                } else {
                    UnityEngine.Debug.Log("SwrveSDK: Looks like silent UIBackgroundModes was already present in Info.plist");
                }
            }
        }
    }


    private static bool AddFolderToProject(PBXProject project, string targetGuid, string folderToCopy, string pathToProject, string destPath)
    {
        if (!System.IO.Directory.Exists(folderToCopy)) {
            return false;
        }
        // Create dest folder
        string fullDestPath = Path.Combine(pathToProject, destPath);
        if (!System.IO.Directory.Exists(fullDestPath)) {
            System.IO.Directory.CreateDirectory (fullDestPath);
        }

        // Copy files in this folder
        string[] files = System.IO.Directory.GetFiles (folderToCopy);
        for (int i = 0; i < files.Length; i++) {
            string filePath = files [i];
            if (!filePath.EndsWith (".meta")) {
                string fileName = System.IO.Path.GetFileName (filePath);
                string newFilePath = Path.Combine(fullDestPath, fileName);
                System.IO.File.Copy (filePath, newFilePath);
                // Add to the XCode project
                string relativeProjectPath = Path.Combine (destPath, fileName);
                string resourceGuid = project.AddFile (relativeProjectPath, relativeProjectPath, PBXSourceTree.Source);
                project.AddFileToBuild (targetGuid, resourceGuid);
            }
        }

        // Copy folders and xcassets
        string[] folders = System.IO.Directory.GetDirectories (folderToCopy);
        for (int i = 0; i < folders.Length; i++) {
            string folderPath = folders [i];
            string dirName = System.IO.Path.GetFileName (folderPath);
            if  (folderPath.EndsWith(".xcassets")) {
                // xcassets is a special case where it is treated as a file
                CopyFolder(folderPath, Path.Combine(fullDestPath, dirName));
                // Add to the XCode project
                string relativeProjectPath = Path.Combine (destPath, dirName);
                string resourceGuid = project.AddFile (relativeProjectPath, relativeProjectPath, PBXSourceTree.Source);
                project.AddFileToBuild (targetGuid, resourceGuid);
            } else {
                // Recursively copy files
                AddFolderToProject (project, targetGuid, folderPath, pathToProject, Path.Combine(destPath, dirName));
            }
        }

        return (folders.Length != 0 || files.Length != 0);
    }

    private static void CopyFolder(string orig, string dest)
    {
        if (!System.IO.Directory.Exists(dest)) {
            System.IO.Directory.CreateDirectory (dest);
        }
        string[] files = System.IO.Directory.GetFiles (orig);
        for (int i = 0; i < files.Length; i++) {
            string filePath = files [i];
            string fileName = System.IO.Path.GetFileName (filePath);
            System.IO.File.Copy (filePath, Path.Combine(dest, fileName));
        }
        string[] folders = System.IO.Directory.GetDirectories (orig);
        for (int i = 0; i < folders.Length; i++) {
            string folderPath = folders[i];
            string dirName = System.IO.Path.GetFileName (folderPath);
            CopyFolder(folderPath, Path.Combine(dest, dirName));
        }
    }

    private static string SetValueOfXCodeGroup (string grouping, string project, string replacewith)
    {
        string pattern = string.Format (@"{0} = .*;$", grouping);
        string replacement = string.Format (@"{0} = {1};", grouping, replacewith);
        return Regex.Replace (project, pattern, replacement, RegexOptions.Multiline);
    }

    private static List<string> GetAllFiles (string path, string fileExtension)
    {
        List<string> result = new List<string> ();
        try {
            string[] files = System.IO.Directory.GetFiles (path, fileExtension);
            result.AddRange (files);
            string[] directories = Directory.GetDirectories(path);
            for (int i = 0; i < directories.Length; i++) {
                List<string> filesInDir = GetAllFiles (directories[i], fileExtension);
                result.AddRange (filesInDir);
            }
        } catch (System.Exception ex) {
            UnityEngine.Debug.LogError (ex);
        }

        return result;
    }

    private static string ReplaceFirst(string str, string needle, string replacement)
    {
        int index = str.IndexOf(needle);
        if (index >= 0) {
            return str.Substring(0, index) + replacement + str.Substring(index + needle.Length);
        }
        return str;
    }
}
#endif
