#if UNITY_WSA

using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class SwrveUWPPostProcess : SwrveCommonBuildComponent
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        SwrveLog.Log ("SwrveQAPostProcess (UWP)");
        DirectoryCopy("extras", Path.Combine(pathToBuiltProject, "extras"), false, true);
        CopyFile(Path.Combine(pathToBuiltProject, "UnityCommon.props"), "UWP/Assembly-CSharp/UnityCommon.props");
        CopyFile(Path.Combine(pathToBuiltProject, "UnityCommon.props"), "UWP/Assembly-CSharp-firstpass/UnityCommon.props");
    }
}
#endif
