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
        DirectoryCopy("extras", Path.Combine(pathToBuiltProject, "extras"), false, true);
        CopyFile(Path.Combine(pathToBuiltProject, "UnityCommon.props"), "UWP/Assembly-CSharp/UnityCommon.props");
        CopyFile(Path.Combine(pathToBuiltProject, "UnityCommon.props"), "UWP/Assembly-CSharp-firstpass/UnityCommon.props");

        string filePath = "UWP/Assembly-CSharp/project.json";
        string projectJson = File.ReadAllText(filePath);
        Dictionary<string, object> json = (Dictionary<string, object>)Json.Deserialize(projectJson);
        Dictionary<string, object> dependencies = (Dictionary<string, object>)json["dependencies"];
        dependencies["SwrveConversationsSDK"] = "4.6.0";
        dependencies["SwrveSDKCommon"] = "4.6.0";
        dependencies["SwrveUnityBridge"] = "4.6.0";
        File.WriteAllText(filePath, Json.Serialize(json));
    }
}

#endif
