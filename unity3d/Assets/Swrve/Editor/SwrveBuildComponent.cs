using System;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SwrveUnityMiniJSON;

public class SwrveBuildComponent : SwrveCommonBuildComponent
{
    private static string[] assets = {
        "Assets/Plugins",
        "Assets/Swrve"
    };
    private static string[] mainScenes = new string[] {
        "Assets/Swrve/Demo/DemoMinimalIntegrationScene.unity"
    };
    private static BuildOptions opt = BuildOptions.None;
    private static string IOSDemoBundleIdentifier = "com.swrve.demo";
    private static string AndroidPackageName = "com.example.gcm";

    private const string POSTPROCESS_JSON = "Assets/Swrve/Editor/postprocess.json";
    private const string TEMPLATE_CONTENT = "REPLACEME";
    private const string APP_GROUP_ID_PUBLIC_KEY = "appGroupIdentifier";
    public const string APP_GROUP_ID_KEY = "iOSAppGroupIdentifier";

    private static Dictionary<string, object> postprocessJson = null;

    public static object GetPostProcessBit(string key)
    {
        if(postprocessJson == null) {
            postprocessJson = (Dictionary<string, object>)Json.Deserialize(File.ReadAllText(POSTPROCESS_JSON));
        }
        object retval;
        postprocessJson.TryGetValue(key, out retval);
        return retval;
    }

    public static string GetPostProcessString(string key)
    {
        object o = GetPostProcessBit(key);
        string retval = null;
        if(o is string) {
            string s = (string)o;
            if(s != TEMPLATE_CONTENT) {
                retval = s;
            }
        }
        return retval;
    }

    [MenuItem ("Swrve/Export unityPackage")]
    public static void ExportUnityPackage ()
    {
        AssetDatabase.ExportPackage (assets, "../../buildtemp/Swrve.unityPackage", ExportPackageOptions.Recurse);
    }

    [MenuItem ("Swrve/Export unityPackageAmazon")]
    public static void ExportUnityPackageAmazon ()
    {
        AssetDatabase.ExportPackage (assets, "../../buildtemp/SwrveAmazon.unityPackage", ExportPackageOptions.Recurse);
    }

    [MenuItem ("Swrve/Export unityPackageFirebase")]
    public static void ExportUnityPackageFirebase ()
    {
        AssetDatabase.ExportPackage (assets, "../../buildtemp/Swrve.unityPackage", ExportPackageOptions.Recurse);
    }

    [MenuItem ("Swrve/iOS/Build Demo (Xcode project)")]
    public static void BuildDemoiOS ()
    {
        BuildIOS ("../../SwrveDemo", opt, mainScenes, IOSDemoBundleIdentifier);
    }

    [MenuItem ("Swrve/iOS/Build and Install Demo")]
    public static void BuildAndInstallDemoiOS ()
    {
        // Build
        bool buildAgain = true;
        if (System.IO.File.Exists (GetProjectPath () + "/../../buildtemp/SwrveDemo")) {
            buildAgain = EditorUtility.DisplayDialog ("Swrve Demo", "The XCode project already exists. Do you want to build again?", "Yes", "Hell no");
        }

        if (buildAgain) {
            BuildDemoiOS ();
        }

        // Install
        RunSh (GetProjectPath() + "/../../", "scripts/archive_and_install_demo.sh");
        UnityEngine.Debug.Log("Demo installed on device");
    }

    [MenuItem ("Swrve/Android/Build Demo (.apk)")]
    public static void BuildDemoAndroid ()
    {
        BuildAndroid ("../buildtemp/SwrveDemo.apk", opt, mainScenes, AndroidPackageName);
    }

    [MenuItem ("Swrve/Android/Build and Install Demo")]
    public static void BuildAndInstallDemoAndroid ()
    {
        // Build
        bool buildAgain = true;
        if (System.IO.File.Exists (GetProjectPath () + "/../../buildtemp/SwrveDemo.apk")) {
            buildAgain = EditorUtility.DisplayDialog ("Swrve Demo", "The APK build already exists. Do you want to build again?", "Yes", "Hell no");
        }

        if (buildAgain) {
            BuildDemoAndroid ();
        }

        // Install
        InstallAndroidBuild (GetProjectPath (), "../../buildtemp/SwrveDemo.apk", "Demo");
    }

    // Tests
    [MenuItem ("Swrve/iOS/Test build with all stripping levels")]
    public static void TestBuildiOS ()
    {
        string outputPath = "../../buildtemp/tmp_iOS";

#if UNITY_2018_3_OR_NEWER
        // Build iOS
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Disabled);
        BuildIOS (outputPath, opt, mainScenes, IOSDemoBundleIdentifier);

        // Build with mscorlib
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Low);
        BuildIOS (outputPath, opt, mainScenes, IOSDemoBundleIdentifier);

        // Build with strip bytecode
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Medium);
        BuildIOS (outputPath, opt, mainScenes, IOSDemoBundleIdentifier);

        // Build with strip assemblies
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.High);
        BuildIOS (outputPath, opt, mainScenes, IOSDemoBundleIdentifier);
#else
        // Build iOS
        PlayerSettings.strippingLevel = StrippingLevel.Disabled;
        BuildIOS (outputPath, opt, mainScenes, IOSDemoBundleIdentifier);

        // Build with mscorlib
        PlayerSettings.strippingLevel = StrippingLevel.UseMicroMSCorlib;
        BuildIOS (outputPath, opt, mainScenes, IOSDemoBundleIdentifier);

        // Build with strip bytecode
        PlayerSettings.strippingLevel = StrippingLevel.StripByteCode;
        BuildIOS (outputPath, opt, mainScenes, IOSDemoBundleIdentifier);

        // Build with strip assemblies
        PlayerSettings.strippingLevel = StrippingLevel.StripAssemblies;
        BuildIOS (outputPath, opt, mainScenes, IOSDemoBundleIdentifier);
#endif
    }

    [MenuItem ("Swrve/Android/Test build with all stripping levels")]
    public static void TestBuildAndroid ()
    {
        string outputPath = "../../buildtemp/tmp_Android.apk";

#if UNITY_2018_3_OR_NEWER
        // Build Android
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Disabled);
        BuildAndroid (outputPath, opt, mainScenes, AndroidPackageName);

        // Build with mscorlib
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
        BuildAndroid (outputPath, opt, mainScenes, AndroidPackageName);

        // Build with strip bytecode
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Medium);
        BuildAndroid (outputPath, opt, mainScenes, AndroidPackageName);

        // Build with strip assemblies
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.High);
        BuildAndroid (outputPath, opt, mainScenes, AndroidPackageName);
#else
        // Build Android
        PlayerSettings.strippingLevel = StrippingLevel.Disabled;
        BuildAndroid (outputPath, opt, mainScenes, AndroidPackageName);

        // Build with mscorlib
        PlayerSettings.strippingLevel = StrippingLevel.UseMicroMSCorlib;
        BuildAndroid (outputPath, opt, mainScenes, AndroidPackageName);

        // Build with strip bytecode
        PlayerSettings.strippingLevel = StrippingLevel.StripByteCode;
        BuildAndroid (outputPath, opt, mainScenes, AndroidPackageName);

        // Build with strip assemblies
        PlayerSettings.strippingLevel = StrippingLevel.StripAssemblies;
        BuildAndroid (outputPath, opt, mainScenes, AndroidPackageName);
#endif
    }

    [MenuItem ("Swrve/Android Prebuild")]
    public static void AndroidPreBuild()
    {
        AndroidCheckAndAddManifest();
        AndroidCorrectApplicationId();
    }


    private static void AndroidCheckAndAddManifest()
    {
        string androidDir = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Plugins/Android");
        string amFile = Path.Combine(androidDir, "AndroidManifest.xml");

        if (!File.Exists(amFile)) {
            // Prompt to confirm with the User to generate a new Android Manifest
            bool generateManifest = EditorUtility.DisplayDialog("Swrve Android Prebuild", "No AndroidManifest.xml file was been found in Assets/Plugins/Android. Generate a basic one?", "Yes", "No");

            if (generateManifest) {
                StringBuilder manifestSB = new StringBuilder();
                manifestSB.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                manifestSB.AppendLine("<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\">");
                manifestSB.AppendLine("    <application android:icon=\"@drawable/app_icon\" android:label=\"@string/app_name\" android:theme=\"@style/UnityThemeSelector\" android:debuggable=\"true\" android:isGame=\"true\" android:name=\"com.swrve.sdk.SwrveUnityApplication\">");
                manifestSB.AppendLine("    </application>");
                manifestSB.AppendLine("</manifest>");
                File.WriteAllText(amFile, manifestSB.ToString());
            }
        }
    }

    private static void AndroidCorrectApplicationId()
    {
        string androidDir = Path.Combine (Directory.GetCurrentDirectory (), "Assets/Plugins/Android");
        string[] dirs = Directory.GetDirectories (androidDir);
        for (int i = 0; i < dirs.Length; i++) {
            string project = dirs [i];
            string amFile = Path.Combine(project, "_AndroidManifest.xml");
            if (File.Exists (amFile)) {
                string applicationIdentifier;
                applicationIdentifier = PlayerSettings.applicationIdentifier;
                File.WriteAllText (Path.Combine (project, "AndroidManifest.xml"),
                                   File.ReadAllText (amFile).Replace ("${applicationId}", applicationIdentifier)
                                  );
            }
        }
        AssetDatabase.Refresh ();
    }

    public static void SetAppGroupConfigKey(string platform, string writePath=null)
    {
        platform = platform.ToLower();

        string readPath = null;
        if("ios" == platform) {
            readPath = "Assets/Plugins/iOS/SwrvePushExtension";
        } else {
            SwrveLog.Log(string.Format("{0} is an unknown platform, returning", platform));
            return;
        }
        if(!Directory.Exists(readPath)) {
            return;
        }
        readPath = Path.Combine(readPath, "appgroupconfig.json");

        string appGroupIdItem = SwrveBuildComponent.GetPostProcessString(SwrveBuildComponent.APP_GROUP_ID_KEY);
        if(string.IsNullOrEmpty(appGroupIdItem)) {
            SwrveLog.Log(string.Format("No App Group Id Key was set in postprocess file, not adding appgroupconfig.json for {0}", platform));
            return;
        }

        if(string.IsNullOrEmpty(writePath)) {
            writePath = readPath;
        }

        Dictionary<string, object> groupconfig =
            (Dictionary<string, object>)Json.Deserialize(File.ReadAllText(readPath));
        groupconfig[APP_GROUP_ID_PUBLIC_KEY] = appGroupIdItem;
        File.WriteAllText(writePath, Json.Serialize(groupconfig));
    }
}
