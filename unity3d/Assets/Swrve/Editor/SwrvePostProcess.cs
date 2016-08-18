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

/// <summary>
/// Integrates the native code required for Conversations and Location campaigns support.
/// </summary>
public class SwrvePostProcess : SwrveCommonBuildComponent
{
    public static string PODFILE_LOC = "Assets/Swrve/Editor/Podfile.txt";
    public static bool OPEN_WORKSPACE = false;

    [PostProcessBuild(100)]
  	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
  	{
#if UNITY_5
        if (target == BuildTarget.iOS)
#else
        if(target == BuildTarget.iPhone)
#endif
        {
            SwrveLog.Log (string.Format ("SwrvePostProcess (iOS) - {0}", PODFILE_LOC));
            CorrectXCodeProject (pathToBuiltProject, true);

            //Copy the podfile into the project.
            CopyFile (PODFILE_LOC, Path.Combine (pathToBuiltProject, "Podfile"));

            List<string> podPaths = new List<string>{ "/usr/local/bin/pod", "/usr/bin/pod" };
            string podPath = null;

            for (int i = 0; i < podPaths.Count; i++) {
                if (File.Exists (podPaths [i])) {
                    podPath = podPaths [i];
                    break;
                }
            }

            if (null == podPath) {
                UnityEngine.Debug.LogError ("pod executable not found: " + podPath);
                return;
            }

            ExecuteCommand (pathToBuiltProject, podPath, "install");
            if (OPEN_WORKSPACE && !UnityEditorInternal.InternalEditorUtility.inBatchMode) {
                Process.Start (string.Format ("file://{0}/Unity-iPhone.xcworkspace", pathToBuiltProject));
            }
        }
  	}

    private static void CorrectXCodeProject (string pathToProject, bool writeOut)
    {
        string path = Path.Combine (Path.Combine (pathToProject, "Unity-iPhone.xcodeproj"), "project.pbxproj");
        string xcodeproj = File.ReadAllText (path);

        xcodeproj = EnsureInheritedInXCodeList ("OTHER_CFLAGS", xcodeproj);
        xcodeproj = EnsureInheritedInXCodeList ("OTHER_LDFLAGS", xcodeproj);
        xcodeproj = EnsureInheritedInXCodeList ("HEADER_SEARCH_PATHS", xcodeproj);
        xcodeproj = EnsureInheritedInXCodeList ("GCC_PREPROCESSOR_DEFINITIONS", xcodeproj);
        xcodeproj = SetValueOfXCodeGroup ("SDKROOT", xcodeproj, "\"iphoneos\"");

        if (writeOut) {
            File.WriteAllText (path, xcodeproj);
        }
    }

    private static string SetValueOfXCodeGroup (string grouping, string project, string replacewith)
    {
        string pattern = string.Format (@"{0} = .*;$", grouping);
        string replacement = string.Format (@"{0} = {1};", grouping, replacewith);

        Match match = Regex.Match (project, pattern, RegexOptions.Multiline);
        log (string.Format ("searching for {0}, found: {1}", pattern, match.Success));

        project = Regex.Replace (project, pattern, replacement, RegexOptions.Multiline);

        return project;
    }

    private static string EnsureInheritedInXCodeList (string grouping, string project)
    {
        string pattern = string.Format (@"{0} = \($", grouping);
        string replacement = string.Format (@"{0} = (""$(inherited)"",", grouping);

        Match match = Regex.Match (project, pattern, RegexOptions.Multiline);
        log (string.Format ("searching for {0}, found: {1}", pattern, match.Success));

        project = Regex.Replace (project, pattern, replacement, RegexOptions.Multiline);

        return project;
    }

    private static void log(string msg)
    {
        UnityEngine.Debug.Log(msg);
    }
}
