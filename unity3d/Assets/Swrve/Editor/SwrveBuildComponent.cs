using System;
using UnityEditor;
using System.Diagnostics;
using UnityEngine;
using System.IO;

public class SwrveBuildComponent : SwrveCommonBuildComponent
{
    private static string[] assets = {
        "Assets/Plugins",
        "Assets/Swrve",
        "Assets/Editor/SwrveSDKPostProcess.cs"
    };
    private static string[] mainScenes = new string[] {
        "Assets/Swrve/UnitySwrveDemo/DemoScene.unity"
    };
    private static BuildOptions opt = BuildOptions.None;
    private static string IOSDemoBundleIdentifier = "com.swrve.demo";
    private static string AndroidPackageName = "com.example.gcm";

    [MenuItem ("Swrve/Export unityPackage")]
    public static void ExportUnityPackage ()
    {
#if UNITY_5
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iOS);
#else
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iPhone);
#endif
        AssetDatabase.ExportPackage (assets, "../../buildtemp/Swrve.unityPackage", ExportPackageOptions.Recurse);
    }

    [MenuItem ("Swrve/Export unityPackageGoogle")]
    public static void ExportUnityPackageGoogle ()
    {
#if UNITY_5
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iOS);
#else
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iPhone);
#endif
        AssetDatabase.ExportPackage (assets, "../../buildtemp/Swrve.unityPackage", ExportPackageOptions.Recurse);
    }

    [MenuItem ("Swrve/Export unityPackageAmazon")]
    public static void ExportUnityPackageAmazon ()
    {
#if UNITY_5
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iOS);
#else
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iPhone);
#endif
        AssetDatabase.ExportPackage (assets, "../../buildtemp/SwrveAmazon.unityPackage", ExportPackageOptions.Recurse);
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
    }

    [MenuItem ("Swrve/Android/Test build with all stripping levels")]
    public static void TestBuildAndroid ()
    {
        string outputPath = "../../buildtemp/tmp_Android.apk";

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
    }

    public static void TestBuildWebPlayer ()
    {
        string outputPath = "../../buildtemp/tmp_WebPlayer";
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.WebPlayer);
        string error = BuildPipeline.BuildPlayer (mainScenes, outputPath, BuildTarget.WebPlayer, opt);
        if (error != null && !error.Equals (string.Empty)) {
            throw new Exception (error);
        }
    }

    [MenuItem ("Swrve/Correct ${applicationId} in AndroidManifests")]
    public static void CorrectApplicationId()
    {
        string androidDir = Path.Combine (Directory.GetCurrentDirectory (), "Assets/Plugins/Android");
        string[] dirs = Directory.GetDirectories (androidDir);
        for (int i = 0; i < dirs.Length; i++) {
            string project = dirs [i];
            string amFile = Path.Combine(project, "_AndroidManifest.xml");
            if (File.Exists (amFile)) {
                File.WriteAllText (Path.Combine (project, "AndroidManifest.xml"),
                    File.ReadAllText (amFile).Replace ("${applicationId}", PlayerSettings.bundleIdentifier)
                );
            }
        }
        AssetDatabase.Refresh ();
    }
}

