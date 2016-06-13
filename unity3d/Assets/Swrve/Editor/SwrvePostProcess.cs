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
#if UNITY_5
		if(target == BuildTarget.iOS)
#else
    if(target == BuildTarget.iPhone)
#endif
		{
            //Copy the podfile into the project.
            string podfile = "Assets/Swrve/Editor/Podfile.txt";
            string destpodfile = Path.Combine (pathToBuiltProject, "Podfile");
            if (!System.IO.File.Exists (destpodfile)) {
                FileUtil.CopyFileOrDirectory (podfile, destpodfile);
            }
      
            CorrectPBXProj (pathToBuiltProject);
            CorrectXCScheme (pathToBuiltProject);
      
			CocoaPodHelper.Update(pathToBuiltProject);
		}
		else
		{
			UnityEngine.Debug.Log("post process for Android");
		}
	}

    public static void CorrectPBXProj(string pathToBuiltProject)
    {
        string pbxprojpath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/project.pbxproj");
        string pbxproj = File.ReadAllText(pbxprojpath);
        pbxproj = pbxproj.Replace("SDKROOT = iphonesimulator", "SDKROOT = iphoneos");
        pbxproj = pbxproj.Replace("SUPPORTED_PLATFORMS = \"iphoneos iphonesimulator\";",
            @"SUPPORTED_PLATFORMS = (
                  iphoneos,
                  iphonesimulator,
                );"
        );
        pbxproj = pbxproj.Replace(
            @"OTHER_LDFLAGS = (",
            @"OTHER_LDFLAGS = (""$(inherited)"","
        );
        pbxproj = pbxproj.Replace(
            @"OTHER_CFLAGS = (",
            @"OTHER_CFLAGS = (""$(inherited)"","
        );
        pbxproj = pbxproj.Replace(
            @"HEADER_SEARCH_PATHS = (",
            @"HEADER_SEARCH_PATHS = (""$(inherited)"","
        );
        File.WriteAllText (pbxprojpath, pbxproj);
    }

    public static void CorrectXCScheme(string pathToBuiltProject)
    {
        string xcschemepath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/xcshareddata/xcschemes/Unity-iPhone.xcscheme");
        string xcscheme = File.ReadAllText (xcschemepath);
        xcscheme = xcscheme.Replace ("parallelizeBuildables=\"YES\"", "parallelizeBuildables=\"NO\"");
        xcscheme = xcscheme.Replace ("buildImplicitDependencies=\"YES\"", "buildImplicitDependencies=\"NO\"");
        xcscheme = xcscheme.Replace ("<BuildActionEntries>",
            @"<BuildActionEntries>
         <BuildActionEntry
            buildForTesting = ""YES""
            buildForRunning = ""YES""
            buildForProfiling = ""YES""
            buildForArchiving = ""YES""
            buildForAnalyzing = ""YES"">
            <BuildableReference
               BuildableIdentifier = ""primary""
               BlueprintIdentifier = ""CC02D2155E8203D080A08ED089AA2524""
               BuildableName = ""libPods-Unity-iPhone.a""
               BlueprintName = ""Pods-Unity-iPhone""
               ReferencedContainer = ""container:Pods/Pods.xcodeproj"">
            </BuildableReference>
         </BuildActionEntry>"
        );
        File.WriteAllText (xcschemepath, xcscheme);
    }
}
