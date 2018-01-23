using System;
using UnityEditor;
using System.Diagnostics;
using UnityEngine;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using SwrveUnityMiniJSON;
using System.Linq;

public class SwrveCommonBuildComponent
{

    protected static string GetProjectPath ()
    {
        string path = Application.dataPath;
        DirectoryInfo parentDir = Directory.GetParent (path.EndsWith ("\\") ? path : string.Concat (path, "\\"));
        return parentDir.FullName;
    }

    protected static void BuildIOS (string fileName, BuildOptions opt, string[] mainScenes, string applicationIdentifier)
    {
        fileName = Path.GetFullPath (fileName);
        UnityEngine.Debug.Log ("[####] Building " + fileName);
		UnityEngine.Debug.Log ("With: " + PlayerSettings.iOS.sdkVersion + ", opt: " + opt + ", scenes: " + mainScenes + ", id: " + applicationIdentifier);

#if UNITY_5_6_OR_NEWER || UNITY_2017_1_OR_NEWER
		PlayerSettings.applicationIdentifier = applicationIdentifier;
#else
		PlayerSettings.bundleIdentifier = applicationIdentifier;
#endif

#if UNITY_5 || UNITY_2017_1_OR_NEWER
#if UNITY_2017_1_OR_NEWER
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTargetGroup.iOS, BuildTarget.iOS);
#else
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iOS);
#endif
        string error = BuildPipeline.BuildPlayer (mainScenes, fileName, BuildTarget.iOS, opt);
#else
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.iPhone);
        string error = BuildPipeline.BuildPlayer (mainScenes, fileName, BuildTarget.iPhone, opt);
#endif
        if (error != null && !error.Equals (string.Empty)) {
            throw new Exception (error);
        }
        UnityEngine.Debug.Log ("Built " + fileName);
    }

	protected static void BuildAndroid (string fileName, BuildOptions opt, string[] mainScenes, string applicationIdentifier)
    {
        UnityEngine.Debug.Log ("[####] Building " + fileName);
#if UNITY_2017_1_OR_NEWER
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTargetGroup.Android, BuildTarget.Android);
#else
        EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTarget.Android);
#endif

#if UNITY_5_6_OR_NEWER || UNITY_2017_1_OR_NEWER
		PlayerSettings.applicationIdentifier = applicationIdentifier;
#else
		PlayerSettings.bundleIdentifier = applicationIdentifier;
#endif
        SwrveBuildComponent.AndroidPreBuild ();

        // Fix for ANDROID_HOME Unity bug
        FixAndroidHomeNotFound ();

        // Build Android
        string error = BuildPipeline.BuildPlayer (mainScenes, fileName, BuildTarget.Android, opt);
        if (error != null && !error.Equals (string.Empty)) {
            throw new Exception (error);
        }
        UnityEngine.Debug.Log ("Built " + fileName);
    }

    protected static string FixAndroidHomeNotFound ()
    {
        // Get the Android SDK location
        string androidSDKLocation = EditorPrefs.GetString ("AndroidSdkRoot");
        if (string.IsNullOrEmpty (androidSDKLocation)) {
            androidSDKLocation = Environment.GetEnvironmentVariable ("UNITY_ANDROID_HOME");
            if (string.IsNullOrEmpty (androidSDKLocation)) {
                androidSDKLocation = Environment.GetEnvironmentVariable ("ANDROID_HOME");
            }
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
        info.Arguments = "scripts/transporter_chief.rb " + filePath;
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
        info.Arguments = string.Format("install -r {0}", filePath);
        System.Diagnostics.Process proc = System.Diagnostics.Process.Start (info);

        string errorOutput = string.Empty;
        while (!proc.HasExited) {
            errorOutput += proc.StandardError.ReadToEnd ();
        }

        if (proc.ExitCode != 0) {
            EditorUtility.DisplayDialog (projectName + " Install", "Could not install the " + projectName + " on the device. Error code: " + proc.ExitCode, "Accept");
            throw new Exception (errorOutput);
        } else {
            info = new ProcessStartInfo ();
            info.RedirectStandardError = true;
            info.UseShellExecute = false;
            info.WorkingDirectory = workingDirectory;
            info.FileName = androidSDKLocation + "/platform-tools/adb";
			string applicationIdentifier;
#if UNITY_5_6_OR_NEWER || UNITY_2017_1_OR_NEWER
			applicationIdentifier = PlayerSettings.applicationIdentifier;
#else
			applicationIdentifier = PlayerSettings.bundleIdentifier;
#endif

			info.Arguments = string.Format("shell monkey -p {0} -c android.intent.category.LAUNCHER 1", applicationIdentifier);
            proc = System.Diagnostics.Process.Start (info);
            while (!proc.HasExited) {
                errorOutput += proc.StandardError.ReadToEnd ();
            }

            UnityEngine.Debug.Log ("Android build installed successfully");
        }
    }

    public static void SetDependenciesForProjectJSON(string projRoot, Dictionary<string, string> dependencies, string filename="project.json")
    {
        string filePath = Path.Combine(projRoot, filename);
        string projectJson = File.ReadAllText(filePath);
        Dictionary<string, object> json = (Dictionary<string, object>)Json.Deserialize(projectJson);
        Dictionary<string, object> _dependencies = (Dictionary<string, object>)json["dependencies"];

        Dictionary<string, string>.Enumerator it = dependencies.GetEnumerator();
        while (it.MoveNext ()) {
            _dependencies [it.Current.Key] = it.Current.Value;
        }
        File.WriteAllText(filePath, Json.Serialize(json));
    }

    public static void AddCompilerFlagToCSProj(string projRoot, string proj, string flag)
    {
        string csprojPath = Path.Combine (projRoot, Path.Combine (proj, string.Format ("{0}.csproj", proj)));

        XmlDocument doc = new XmlDocument();
        doc.Load(csprojPath);
        XmlNode root = doc.DocumentElement;
        bool save = false;

        for (int i = 0; i < root.ChildNodes.Count; i++) {
            XmlNode parent = root.ChildNodes [i];
            if (parent.Name == "PropertyGroup") {
                for (int j = 0; j < parent.ChildNodes.Count; j++) {
                    XmlNode child = parent.ChildNodes [j];
                    if (child.Name == "DefineConstants") {
                        string text = child.InnerText;
                        if (!text.Contains (flag)) {
                            save = true;
                            child.InnerText = text + string.Format("{0}{1};", (text.EndsWith (";") ? "" : ";"), flag);
                        }
                    }
                }
            }
        }

        if (save) {
            doc.Save (csprojPath);
        }
    }

    public static void AddWindowsPushCallback(string path)
    {
        int STATE_BEGIN = 0;
        int STATE_IN_FUNC = 1;
        int STATE_FOUND_LINE = 2;
        int STATE_FINISHED = 9999;

        int curState = STATE_BEGIN;
        string foundLine = null;
        string toAdd = "SwrveUnityWindows.SwrveUnityBridge.OnActivated(args);";
        string needle = "InitializeUnity(appArgs);";
        string filePath = Path.Combine (path, "App.xaml.cs");
        string[] lines = File.ReadAllLines (filePath);
        List<string> newLines = new List<string> ();

        for (int i = 0; i < lines.Count(); i++) {
            string line = lines[i];
            if (curState == STATE_BEGIN && line.Contains ("void OnActivated(IActivatedEventArgs")) {
                curState = STATE_IN_FUNC;
            } else if (curState == STATE_IN_FUNC && line.Contains (needle)) {
                curState = STATE_FOUND_LINE;
                foundLine = line;
            } else if (curState == STATE_FOUND_LINE) {
                if (line.Contains(toAdd)) {
                    curState = STATE_FINISHED;
                } else if(line.Trim() == "}") {
                    curState = STATE_FINISHED;
                    newLines.Add(foundLine.Replace(needle, toAdd));
                }
            }

            newLines.Add (line);
        }
        File.WriteAllLines (filePath, newLines.ToArray());
    }

    protected static void CopyFile(string src, string dst, bool dstIsPath=false)
    {
        if (dstIsPath) {
            dst = Path.Combine (dst, Path.GetFileName (src));
        }
        if (!File.Exists(src)) {
            throw new Exception("File " + src + " does not exist");
        }
        File.Copy(src, dst, true);
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
        ExecuteCommand (workingDirectory, "/bin/bash", arguments);
    }

    protected static string ExecuteCommand (string workingDirectory, string filename, string arguments)
    {
        string resp = "";
        if ("." == workingDirectory) {
            workingDirectory = new DirectoryInfo (".").FullName;
        }

        Process proc = new Process ();
        proc.StartInfo.WorkingDirectory = workingDirectory;
        proc.StartInfo.FileName = filename;
        proc.StartInfo.Arguments = arguments;
        SwrveLog.Log (string.Format ("Executing {0} command: {1} (in: {2} )\n(cd {2}; {0} {1})",
                                     filename, arguments, workingDirectory
                                    ));

        proc.StartInfo.CreateNoWindow = true;
        proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardError = true;
        proc.StartInfo.RedirectStandardOutput = true;

        try {
            proc.Start ();
            proc.StandardError.ReadToEnd ();
            resp = proc.StandardOutput.ReadToEnd ();
            string errorOutput = proc.StandardError.ReadToEnd ();

            if ("" != resp) {
                SwrveLog.Log (resp);
            }
            if ("" != errorOutput) {
                SwrveLog.LogError (errorOutput);
            }

            if (proc.ExitCode == 0) {
                UnityEngine.Debug.Log (filename + " " + arguments + " successfull");
            }
        } catch (Exception e) {
            throw new Exception (string.Format ("Encountered unexpected error while running {0}", filename), e);
        }

        return resp;
    }

}
