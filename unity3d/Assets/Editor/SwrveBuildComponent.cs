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

    [MenuItem ("Swrve Demo/Export unityPackage")]
    public static void ExportUnityPackage ()
    {
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6)
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iPhone);
#else
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iOS);
#endif
        AssetDatabase.ExportPackage (assets, "../../buildtemp/Swrve.unityPackage", ExportPackageOptions.Recurse);
    }

    [MenuItem ("Swrve Demo/iOS/Build Demo (Xcode project)")]
    public static void BuildDemoiOS ()
    {
        BuildIOS ("../../SwrveDemo", opt, mainScenes, IOSDemoBundleIdentifier);
    }

    [MenuItem ("Swrve Demo/iOS/Build and Install Demo")]
    public static void BuildAndInstallDemoiOS ()
    {
        // Build
        bool buildAgain = true;
        if (System.IO.File.Exists (GetProjectPath () + "/../../SwrveDemo")) {
            buildAgain = EditorUtility.DisplayDialog ("Swrve Demo", "The XCode project already exists. Do you want to build again?", "Yes", "Hell no");
        }

        if (buildAgain) {
            BuildDemoiOS ();
        }

        // Install
        RunSh (GetProjectPath() + "/../../", "scripts/archive_and_install_demo.sh");
        UnityEngine.Debug.Log("Demo installed on device");
    }

    [MenuItem ("Swrve Demo/Android/Build Demo (.apk)")]
    public static void BuildDemoAndroid ()
    {
        BuildAndroid ("../../SwrveDemo.apk", opt, mainScenes, AndroidPackageName);
    }

    [MenuItem ("Swrve Demo/Android/Build and Install Demo")]
    public static void BuildAndInstallDemoAndroid ()
    {
        // Build
        bool buildAgain = true;
        if (System.IO.File.Exists (GetProjectPath () + "/../../SwrveDemo.apk")) {
            buildAgain = EditorUtility.DisplayDialog ("Swrve Demo", "The APK build already exists. Do you want to build again?", "Yes", "Hell no");
        }

        if (buildAgain) {
            BuildDemoAndroid ();
        }

        // Install
        InstallAndroidBuild (GetProjectPath (), "../../SwrveDemo.apk", "Demo");
    }

    // Tests
    [MenuItem ("Swrve Demo/iOS/Test build with all stripping levels")]
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

    [MenuItem ("Swrve Demo/Android/Test build with all stripping levels")]
    public static void TestBuildAndroid ()
    {
        string outputPath = "../../buildtemp/tmp_Android";

        // Build iOS
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
}

