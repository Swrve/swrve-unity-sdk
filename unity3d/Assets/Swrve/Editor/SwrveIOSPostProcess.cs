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

        // 5. Add conversations resources to bundle (to project and to a new PBXResourcesBuildPhase)
        string resourcesProjectPath = "Libraries/Plugins/iOS/SwrveConversationSDK/Resources";
        string resourcesPath = Path.Combine(pathToProject, resourcesProjectPath);
        System.IO.Directory.CreateDirectory (resourcesPath);
        string[] resources = System.IO.Directory.GetFiles ("Assets/Plugins/iOS/SwrveConversationSDK/Resources");

        if (resources.Length == 0) {
            UnityEngine.Debug.LogError ("Swrve SDK - Could not find any resources. If you want to use Conversations please contact support@swrve.com");
        }

        for (int i = 0; i < resources.Length; i++) {
            string resourcePath = resources [i];
            if (!resourcesPath.EndsWith (".meta")) {
                string resourceFileName = System.IO.Path.GetFileName (resourcePath);
                string newPath = Path.Combine(resourcesPath, resourceFileName);
                System.IO.File.Copy (resourcePath, newPath);
                string resourceGuid = project.AddFile (Path.Combine(resourcesProjectPath, resourceFileName), Path.Combine(resourcesProjectPath, resourceFileName), PBXSourceTree.Source);
                project.AddFileToBuild (targetGuid, resourceGuid);
            }
        }
        xcodeproj = project.WriteToString ();

        // Write changes to the Xcode project
        if (writeOut) {
            File.WriteAllText (path, xcodeproj);
        }

    }

    private static string SetValueOfXCodeGroup (string grouping, string project, string replacewith)
    {
        string pattern = string.Format (@"{0} = .*;$", grouping);
        string replacement = string.Format (@"{0} = {1};", grouping, replacewith);
        Match match = Regex.Match (project, pattern, RegexOptions.Multiline);
        project = Regex.Replace (project, pattern, replacement, RegexOptions.Multiline);
        return project;
    }
}
#endif
