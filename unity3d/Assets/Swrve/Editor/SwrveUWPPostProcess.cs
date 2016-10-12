#if UNITY_WSA

using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using SwrveUnityMiniJSON;

public class SwrveUWPPostProcess : SwrveCommonBuildComponent
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        SwrveLog.Log ("SwrvePostProcess (UWP)");

        SwrveCommonBuildComponent.AddCompilerFlagToCSProj (
            pathToBuiltProject,
            PlayerSettings.productName,
            "SWRVE_WINDOWS_SDK");

        SwrveCommonBuildComponent.SetDependenciesForProjectJSON (
            Path.Combine (pathToBuiltProject, PlayerSettings.productName),
            new Dictionary<string, string> {
                {"SwrveConversationsSDK", "4.6.0"},
                {"SwrveSDKCommon", "4.6.0"},
                {"SwrveUnityBridge", "4.6.0"},
                {"Microsoft.NETCore.UniversalWindowsPlatform", "5.1.0"}
            }
        );
    }
}

#endif
