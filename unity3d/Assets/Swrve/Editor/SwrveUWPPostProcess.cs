//#if UNITY_WSA

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

        string pathToApp = Path.Combine (pathToBuiltProject, PlayerSettings.productName);

        SwrveCommonBuildComponent.SetDependenciesForProjectJSON (
            pathToApp,
            new Dictionary<string, string> {
                {"Microsoft.NETCore.UniversalWindowsPlatform", "5.1.0"}
            }
        );
    }
}

//#endif
