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

        string projectPath = Path.Combine (pathToBuiltProject, PlayerSettings.productName);
        SwrveCommonBuildComponent.SetDependenciesForProjectJSON (
            projectPath,
        new Dictionary<string, string> {
            {"SwrveConversationsSDK", "4.10"},
            {"SwrveSDKCommon", "4.10"},
            {"SwrveUnityBridge", "4.10"},
            {"Microsoft.NETCore.UniversalWindowsPlatform", "5.1.0"}
        }
        );

        CorrectUWPProject ("", pathToBuiltProject);
        CorrectUWPProject ("-firstpass", pathToBuiltProject);

        SwrveCommonBuildComponent.SetDependenciesForProjectJSON (
            "UWP/Assembly-CSharp",
        new Dictionary<string, string> {
            {"SwrveConversationsSDK", "4.10"},
            {"SwrveSDKCommon", "4.10"},
            {"SwrveUnityBridge", "4.10"}
        }
        );

        SwrveCommonBuildComponent.AddCompilerFlagToCSProj ("UWP", "Assembly-CSharp", "SWRVE_WINDOWS_SDK");
        SwrveCommonBuildComponent.AddWindowsPushCallback (projectPath);
    }

    private static void CorrectUWPProject(string version, string pathToBuiltProject)
    {
        string path = string.Format ("UWP/Assembly-CSharp{0}", version);

        SwrveCommonBuildComponent.SetDependenciesForProjectJSON (
            path,
        new Dictionary<string, string> {
            {"Microsoft.NETCore.UniversalWindowsPlatform", "5.1.0"}
        }
        );

        CopyFile(Path.Combine(pathToBuiltProject, "UnityCommon.props"), Path.Combine(path, "UnityCommon.props"));
    }
}

#endif
