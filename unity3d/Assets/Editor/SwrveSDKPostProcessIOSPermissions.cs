using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AssemblyCSharpEditor
{
public class SwrveSDKPostProcessIOSPermissions
{
    private static string[] requiredFrameworks = new string[] { "AddressBook", "AssetsLibrary", "CoreTelephony" };

    [PostProcessBuild]
    public static void OnPostprocessBuild (BuildTarget target, string pathToBuiltProject)
    {
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6)
        if (target == BuildTarget.iPhone && pathToBuiltProject != null) {
#else
        if (target == BuildTarget.iOS && pathToBuiltProject != null) {
#endif
            iOSAddRequiredFrameworks (pathToBuiltProject);
        }
    }

    public static void iOSAddRequiredFrameworks (string path)
    {
        List<string> allPbXproj = GetAllFiles (path, "project.pbxproj");
        foreach (string filePath in allPbXproj) {
            AddRequiredFrameworksToOtherFlags(filePath);
        }
    }

    /* Add the required frameworks to OTHER_LDFLAGS.
     * Example of OTHER_LDFLAGS:
     *
     *OTHER_LDFLAGS = (
          "-weak_framework",
          CoreMotion,
          "-weak-lSystem",
          "-framework",
          AddressBook,
          "-framework",
          AssetsLibrary,
          "-framework",
          AddressBook,
          "-framework",
          AssetsLibrary
        );
     */
    public static void AddRequiredFrameworksToOtherFlags(string pbxprojPath)
    {
        string[] lines = File.ReadAllLines (pbxprojPath);
        bool flagsOpened = false;
        for(int l = 0; l < lines.Length; l++) {
            string line = lines[l];
            if (flagsOpened && line.Contains(");")) {
                // Flags closed
                flagsOpened = false;
                // Replace line with the required frameworks
                string frameworkAppend = GetFrameworkAppend();
                lines[l] = frameworkAppend + lines[l];
            } else if (line.Contains("OTHER_LDFLAGS = (")) {
                flagsOpened = true;
            } else if (line.Contains("OTHER_LDFLAGS = \"\";")) {
                // No flags, we must add ours
                lines[l] = "OTHER_LDFLAGS = (" + GetFrameworkAppend() + ");";
            }
        }
        File.WriteAllLines (pbxprojPath, lines);
    }

    static string GetFrameworkAppend() {
        string extraFrameworkLines = "";
        for(int f = 0; f < requiredFrameworks.Length; f++) {
            extraFrameworkLines += "\"-framework\"," + requiredFrameworks[f] + ",";
        }
        return extraFrameworkLines;
    }

    static List<string> GetAllFiles (string path, string fileExtension)
    {
        List<string> result = new List<string> ();
        try {
            string[] files = System.IO.Directory.GetFiles (path, fileExtension);
            result.AddRange (files);
            foreach (string d in Directory.GetDirectories(path)) {
                List<string> filesInDir = GetAllFiles (d, fileExtension);
                result.AddRange (filesInDir);
            }
        } catch (System.Exception ex) {
            Debug.LogError (ex);
        }
        
        return result;
    }
}
}