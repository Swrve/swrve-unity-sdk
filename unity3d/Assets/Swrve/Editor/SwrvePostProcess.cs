using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using System.Collections;
using System.IO;

public class SwrvePostProcess : MonoBehaviour
{
	[PostProcessBuild]
	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
	{
		if(target == BuildTarget.iOS)
		{
			//Copy the podfile into the project.
			string podfile = "Assets/Swrve/Editor/Podfile.txt";
      string destpodfile = Path.Combine(pathToBuiltProject, "Podfile");
			if(!System.IO.File.Exists(destpodfile))
			{
				FileUtil.CopyFileOrDirectory(podfile, destpodfile);
			}
      
      string pbxprojpath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/project.pbxproj");
      string pbxproj = File.ReadAllText(pbxprojpath);
      pbxproj = pbxproj.Replace("SDKROOT = iphonesimulator", "SDKROOT = iphoneos");
      pbxproj = pbxproj.Replace("SUPPORTED_PLATFORMS = \"iphoneos iphonesimulator\";",
        @"SUPPORTED_PLATFORMS = (
          iphoneos,
          iphonesimulator,
        );"
      );
      File.WriteAllText (pbxprojpath, pbxproj);
      
			CocoaPodHelper.Update(pathToBuiltProject);
		}
		else
		{
			UnityEngine.Debug.Log("post process for Android");
		}
	}
}
