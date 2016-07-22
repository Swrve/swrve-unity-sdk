using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class SwrvePostProcess : SwrveCommonBuildComponent
{
    [PostProcessBuild(100)]
	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
	{
    #if UNITY_5
    	if(target == BuildTarget.iOS)
    #else
        if(target == BuildTarget.iPhone)
    #endif
        {
            SwrveLog.Log ("SwrvePostProcess (iOS)");
            //Copy the podfile into the project.
            string podfile = "Assets/Swrve/Editor/Podfile.txt";
          
            CopyFile(podfile, Path.Combine (pathToBuiltProject, "Podfile"));

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
		}
		else
		{
			UnityEngine.Debug.Log("post process for Android");
		}
	}
}
