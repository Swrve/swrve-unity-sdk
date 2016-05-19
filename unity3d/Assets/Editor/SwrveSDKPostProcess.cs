using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AssemblyCSharpEditor
{
public class SwrveSDKPostProcess
{
    [PostProcessBuild]
    public static void OnPostprocessBuild (BuildTarget target, string pathToBuiltProject)
    {
#if UNITY_5
        if (target == BuildTarget.iOS && pathToBuiltProject != null) {
#else
        if (target == BuildTarget.iPhone && pathToBuiltProject != null) {
#endif
            iOSOnlySendRemoteNotificationsIfInBackground (pathToBuiltProject);
            iOSSilentPushNotifications(pathToBuiltProject);
        }
    }

    // Prevent Swrve normal pushes from being processed right away when the app is in the foreground.
    private static void iOSOnlySendRemoteNotificationsIfInBackground (string path)
    {
        List<string> allMMFiles = GetAllFiles (path, "*.mm");
        foreach (string filePath in allMMFiles) {
            string contents = File.ReadAllText (filePath);
            bool alreadyApplied = (contents.IndexOf("swrveState ==") > 0);
            int hookPosition = contents.IndexOf ("didReceiveRemoteNotification");
            if (hookPosition > 0) {
                if (alreadyApplied) {
                    Debug.Log("SwrveSDK: Modification to UnitySendRemoteNotification already applied");
                    break;
                }
                // File contains notification hook
                // Replace the first appearance of UnitySendRemoteNotification
                // after the hook
                string pattern = @"UnitySendRemoteNotification[\w\s]*\(userInfo\)[\w\s]*;";
                Match closestInstance = null;
                int closestInstanceIndex = Int32.MaxValue;
                foreach (Match match in Regex.Matches(contents, pattern)) {
                    if (Math.Abs(match.Index - hookPosition) < closestInstanceIndex) {
                        // Closest instance of UnitySendRemoteNotification to hook
                        closestInstanceIndex = match.Index;
                        closestInstance = match;
                    }
                }

                if (closestInstance != null) {
                    // Add a flag to the notification indicating if it was received while on the foreground
                    contents = contents.Substring (0, closestInstance.Index) + "UIApplicationState swrveState = [application applicationState];"
                                + "BOOL swrveInBackground = (swrveState == UIApplicationStateInactive || swrveState == UIApplicationStateBackground);"
                                + "if (!swrveInBackground) { NSMutableDictionary* mutableUserInfo = [userInfo mutableCopy]; userInfo = mutableUserInfo; [mutableUserInfo setValue:@\"YES\" forKey:@\"_swrveForeground\"]; } "
                                + closestInstance.Value + contents.Substring (closestInstance.Index + closestInstance.Length);
                    File.WriteAllText (filePath, contents);
                    Debug.Log("SwrveSDK: Applied modification to UnitySendRemoteNotification so Swrve pushes are only processed when opened from the background");
                } else {
                    Debug.Log("SwrveSDK: Push modification not successful. If you are using Swrve push notifications, please contact support@swrve.com");
                }
            }
        }
    }

    // Enables silent push
    private static void iOSSilentPushNotifications(string path)
    {
        // Step 1. Add a silent push receiver to the AppDelegate
        // Apply only if the file Swrve-DidReceiveSilentPush.mm is found
        string fileName = "Swrve-DidReceiveSilentPush.mm";
        string mmFilePath = "Assets/Plugins/iOS/" + fileName;
        bool silentPushEnabled = false;
        bool injectedCode = false;
        if (File.Exists (mmFilePath)) {
            silentPushEnabled = true;
            string mmSilentContents = File.ReadAllText (mmFilePath);
            // Find injection point (AppDelegate class)
            List<string> allMMFiles = GetAllFiles (path, "*.mm");
            foreach (string filePath in allMMFiles) {
                string contents = File.ReadAllText (filePath);
                bool alreadyApplied = (Regex.Matches(contents, "application:(.)*didReceiveRemoteNotification:(.)*fetchCompletionHandler:").Count > 0);
                int hookPosition = contents.IndexOf ("- (void)applicationWillEnterForeground:(UIApplication*)application");
                if (hookPosition > 0) {
                    if (alreadyApplied) {
                        break;
                    }
                    contents = contents.Substring (0, hookPosition - 1) + mmSilentContents + contents.Substring (hookPosition, contents.Length - hookPosition);
                    File.WriteAllText (filePath, contents);
                    injectedCode = true;
                    Debug.Log("SwrveSDK: Injected " + fileName + " code into the app delegate");
                }
            }
        }

        if (silentPushEnabled && !injectedCode) {
            throw new Exception("SwrveSDK: " + fileName + " could not be injected into the AppDelegate. Contact support@swrve.com");
        }
        // Remove file from project
        List<string> allProjFiles = GetAllFiles (path, "*.pbxproj");
        foreach (string filePath in allProjFiles) {
            List<string> finalLines = new List<string> ();
            bool foundFileReference = false;
            foreach (var strLine in File.ReadAllLines(filePath)) {
                if (strLine != null) {
                    if (!strLine.Contains ("Swrve-DidReceiveSilentPush.mm")) {
                        finalLines.Add (strLine);
                    } else {
                        foundFileReference = true;
                    }
                }
            }
            if (foundFileReference) {
                File.WriteAllLines(filePath, finalLines.ToArray());
            }
        }
        string originalMMPath = path + "/Libraries/Plugins/iOS/" + fileName;
        if (File.Exists (originalMMPath)) {
            File.Delete(originalMMPath);
        }

        // Step 2. Add silent background mode the file Info.plist
        string plistPath = path + "/Info.plist";
        if (silentPushEnabled && File.Exists (plistPath)) {
            string plistContent = File.ReadAllText (plistPath);
            if (!plistContent.Contains("<key>UIBackgroundModes</key>") && !plistContent.Contains("<string>remote-notification</string>")) {
                File.WriteAllText(plistPath, ReplaceFirst(plistContent, "<dict>", "<dict><key>UIBackgroundModes</key><array><string>remote-notification</string></array>"));
                Debug.Log("SwrveSDK: Injected silent UIBackgroundModes mode into Info.plist");
            } else {
                Debug.Log("SwrveSDK: Looks like silent UIBackgroundModes was already present in Info.plist");
            }
        }
    }

    private static List<string> GetAllFiles (string path, string fileExtension)
    {
        List<string> result = new List<string> ();
        try {
            string[] files = System.IO.Directory.GetFiles (path, fileExtension);
            result.AddRange (files);
            foreach (string d in Directory.GetDirectories(path)) {
                List<string> filesInDir = GetAllFiles (d, fileExtension);
                result.AddRange (filesInDir);
            }
        } catch (System.Exception ex) {
            Debug.LogError (ex);
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
}
