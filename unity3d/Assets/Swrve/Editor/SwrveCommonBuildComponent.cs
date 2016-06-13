using System;
using UnityEditor;
using System.Diagnostics;
using UnityEngine;
using System.IO;

public class SwrveCommonBuildComponent
{

    protected static string GetProjectPath ()
    {
        string path = Application.dataPath;
        DirectoryInfo parentDir = Directory.GetParent (path.EndsWith ("\\") ? path : string.Concat (path, "\\"));
        return parentDir.FullName;
    }

    protected static void BuildIOS (string fileName, BuildOptions opt, string[] mainScenes, string bundleIdentifier)
    {
        UnityEngine.Debug.Log ("[####] Building " + fileName);

        PlayerSettings.bundleIdentifier = bundleIdentifier;
#if UNITY_5
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iOS);
        string error = BuildPipeline.BuildPlayer (mainScenes, fileName, BuildTarget.iOS, opt);
#else
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iPhone);
        string error = BuildPipeline.BuildPlayer (mainScenes, fileName, BuildTarget.iPhone, opt);
#endif

        if (error != null && !error.Equals (string.Empty)) {
            throw new Exception (error);
        }
    }

    protected static void BuildAndroid (string fileName, BuildOptions opt, string[] mainScenes, string packageName)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.Android);
        PlayerSettings.bundleIdentifier = packageName;
        SwrveBuildComponent.CorrectApplicationId ();

        // Fix for ANDROID_HOME Unity bug
        FixAndroidHomeNotFound ();

        // Build Android
        string error = BuildPipeline.BuildPlayer (mainScenes, fileName, BuildTarget.Android, opt);
        if (error != null && !error.Equals (string.Empty)) {
            throw new Exception (error);
        }
    }

    protected static string FixAndroidHomeNotFound ()
    {
        // Get the Android SDK location
        string androidSDKLocation = EditorPrefs.GetString ("AndroidSdkRoot");
        if (string.IsNullOrEmpty (androidSDKLocation)) {
            androidSDKLocation = Environment.GetEnvironmentVariable ("ANDROID_HOME");
            if (string.IsNullOrEmpty (androidSDKLocation)) {
                // Build machine default location
                androidSDKLocation = "/Users/Shared/android-sdk/sdk/";
            }
            EditorPrefs.SetString ("AndroidSdkRoot", androidSDKLocation);
        }

        return androidSDKLocation;
    }

    protected static void InstallIOSBuild (string workingDirectory, string filePath)
    {
        ProcessStartInfo info = new ProcessStartInfo ();
        info.RedirectStandardError = true;
        info.UseShellExecute = false;
        info.WorkingDirectory = workingDirectory;
        info.FileName = "ruby";
        info.Arguments = "scripts/trainsporter_chef.rb " + filePath;
        System.Diagnostics.Process proc = System.Diagnostics.Process.Start (info);

        string errorOutput = string.Empty;
        while (!proc.HasExited) {
            errorOutput += proc.StandardError.ReadToEnd ();
        }

        if (proc.ExitCode != 0) {
            EditorUtility.DisplayDialog ("Demo Install", "Could not install the Demo on the device. Error code: " + proc.ExitCode, "Accept");
            throw new Exception (errorOutput);
        }
    }

    protected static void InstallAndroidBuild (string workingDirectory, string filePath, string projectName)
    {
        string androidSDKLocation = EditorPrefs.GetString ("AndroidSdkRoot");
        ProcessStartInfo info = new ProcessStartInfo ();
        info.RedirectStandardError = true;
        info.UseShellExecute = false;
        info.WorkingDirectory = workingDirectory;
        info.FileName = androidSDKLocation + "/platform-tools/adb";
        info.Arguments = "install -r " + filePath;
        System.Diagnostics.Process proc = System.Diagnostics.Process.Start (info);

        string errorOutput = string.Empty;
        while (!proc.HasExited) {
            errorOutput += proc.StandardError.ReadToEnd ();
        }

        if (proc.ExitCode != 0) {
            EditorUtility.DisplayDialog (projectName + " Install", "Could not install the " + projectName + " on the device. Error code: " + proc.ExitCode, "Accept");
            throw new Exception (errorOutput);
        } else {
            UnityEngine.Debug.Log ("Android build installed successfully");
        }
    }

    protected static void DirectoryCopy (string sourceDirName, string destDirName, bool copySubDirs, bool overrideFiles)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo (sourceDirName);
        DirectoryInfo[] dirs = dir.GetDirectories ();

        if (!dir.Exists) {
            throw new DirectoryNotFoundException (
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists (destDirName)) {
            Directory.CreateDirectory (destDirName);
        }

        int i;

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles ();
        for(i = 0; i < files.Length; i++) {
            FileInfo file = files [i];
            string temppath = Path.Combine (destDirName, file.Name);
            file.CopyTo (temppath, overrideFiles);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs) {
            for(i = 0; i < dirs.Length; i++) {
                DirectoryInfo subdir = dirs [i];
                string temppath = Path.Combine (destDirName, subdir.Name);
                DirectoryCopy (subdir.FullName, temppath, copySubDirs, overrideFiles);
            }
        }
    }

    protected static void RunSh (string workingDirectory, string arguments)
    {
        ProcessStartInfo info = new ProcessStartInfo ();
        info.RedirectStandardError = true;
        info.UseShellExecute = false;
        info.WorkingDirectory = workingDirectory;
        info.FileName = "/bin/bash";
        info.Arguments = arguments;
        System.Diagnostics.Process proc = System.Diagnostics.Process.Start (info);

        string errorOutput = string.Empty;
        while (!proc.HasExited) {
            errorOutput += proc.StandardError.ReadToEnd ();
        }

        if (proc.ExitCode != 0) {
            EditorUtility.DisplayDialog ("Bash sh", "Could run bash sh. Error code: " + proc.ExitCode, "Accept");
            throw new Exception (errorOutput);
        } else {
            UnityEngine.Debug.Log ("Bash sh " + arguments + " successfull");
        }
    }
}

