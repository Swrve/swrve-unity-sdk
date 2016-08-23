using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;

public class SwrvePostProcess : SwrveCommonBuildComponent
{
    public static string PODFILE_LOC = "Assets/Swrve/Editor/Podfile.txt";
    public static bool OPEN_WORKSPACE = false;

    [PostProcessBuild(100)]
	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
	{
    #if UNITY_5
    	if(target == BuildTarget.iOS)
    #else
        if(target == BuildTarget.iPhone)
    #endif
        {
            SwrveLog.Log (string.Format ("SwrvePostProcess (iOS) - {0}", PODFILE_LOC));
            //Copy the podfile into the project.
          
            CopyFile(PODFILE_LOC, Path.Combine (pathToBuiltProject, "Podfile"));

            List<string> podPaths = new List<string>{ "/usr/local/bin/pod", "/usr/bin/pod" };
            string podPath = null;

            for (int i = 0; i < podPaths.Count; i++) {
                if(File.Exists(podPaths[i])) {
                    podPath = podPaths[i];
                    break;
                }
            }

            if (null == podPath)
            {
                UnityEngine.Debug.LogError("pod executable not found: " + podPath);
                return;
            }

            ExecuteCommand(pathToBuiltProject, podPath, "install");
            if (OPEN_WORKSPACE && !UnityEditorInternal.InternalEditorUtility.inBatchMode) {
                Process.Start (string.Format ("file://{0}/Unity-iPhone.xcworkspace", pathToBuiltProject));
            }
		}
		else if(target == BuildTarget.WSAPlayer)
        {
            SwrveLog.Log (string.Format ("SwrvePostProcess (Windows Store) - {0}", pathToBuiltProject));
	        List<string> uwpProjs = new List<string>() { "UWP\\Assembly-CSharp-firstpass", "UWP\\Assembly-CSharp" };

	        for (int i = 0; i < uwpProjs.Count; i++)
	        {
	            File.Copy(Path.Combine(pathToBuiltProject, "UnityCommon.props"), Path.Combine(uwpProjs[i], "UnityCommon.props"), true);
	        }
        }
        else
		{
			SwrveLog.Log("post process for " + target);
		}
	}
}
