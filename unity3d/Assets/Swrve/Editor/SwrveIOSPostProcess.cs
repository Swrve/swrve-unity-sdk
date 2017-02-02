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
            UnityEngine.Debug.LogError ("Swrve SDK - Could not find any resources. If you want to use Conversations please contact support@swrve.com");
        }
        xcodeproj = project.WriteToString ();

        // Write changes to the Xcode project
        if (writeOut) {
            File.WriteAllText (path, xcodeproj);
        }

    }

    private static bool AddFolderToProject(PBXProject project, string targetGuid, string folderToCopy, string pathToProject, string destPath)
    {
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
}
#endif
