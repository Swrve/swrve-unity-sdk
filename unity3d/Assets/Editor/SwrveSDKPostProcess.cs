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
#if UNITY_IPHONE
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6)
        if (target == BuildTarget.iPhone && pathToBuiltProject != null) {
#else
        if (target == BuildTarget.iOS && pathToBuiltProject != null) {
#endif
            iOSOnlySendRemoteNotificationsIfInBackground (pathToBuiltProject);
        }
#endif
    }

#if UNITY_IPHONE
    public static void iOSOnlySendRemoteNotificationsIfInBackground (string path)
    {
        List<string> allMMFiles = GetAllFiles (path, "*.mm");
        foreach (string filePath in allMMFiles) {
            string contents = File.ReadAllText (filePath);
            int alreadyApplied = contents.IndexOf("swrveState");
            int hookPosition = contents.IndexOf ("didReceiveRemoteNotification");
            if (hookPosition > 0 && alreadyApplied < 0) {
                // File contains notification hook
                // Replace the first appearance of UnitySendRemoteNotification
                // after the hook
                string pattern = @"UnitySendRemoteNotification[\w\s]*\(userInfo\)[\w\s]*;";
                Match closestInstance = null;
                int closestInstanceIndex = Int32.MaxValue;
                foreach (Match match in Regex.Matches(contents, pattern)) {
                    if (match.Index > hookPosition && closestInstanceIndex > match.Index) {
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
                }
            }
        }
    }
#endif

    static List<string> GetAllFiles (string path, string fileExtension)
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
}
}