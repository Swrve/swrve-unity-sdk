/*
 * SWRVE CONFIDENTIAL
 * 
 * (c) Copyright 2010-2014 Swrve New Media, Inc. and its licensors.
 * All Rights Reserved.
 *
 * NOTICE: All information contained herein is and remains the property of Swrve
 * New Media, Inc or its licensors.  The intellectual property and technical
 * concepts contained herein are proprietary to Swrve New Media, Inc. or its
 * licensors and are protected by trade secret and/or copyright law.
 * Dissemination of this information or reproduction of this material is
 * strictly forbidden unless prior written permission is obtained from Swrve.
 */

/* NETFX_CORE is set only when building Windows Store applications.
 * It is assumed that versions of Unity earlier than 4.2 will not set the
 * NETFX_CORE directive.
 *
 * There are code two paths for the Windows Store.
 * One for unity 4.2 using the suggested legacy code found here:
 * http://unity3d.com/pages/windows/porting
 * The other for 4.3+ using the built-in Unity Windows.File API:
 * http://docs.unity3d.com/Documentation/ScriptReference/Windows.File.html
 * The final code path covers the remaining platforms including
 * Windows Phone 8.
 */

// Setup defines for the legacy and built in code paths
#if (NETFX_CORE && UNITY_4_2)
#error "Windows Store and Windows Phone not supported on Unity 4.2.*"
#elif NETFX_CORE
#define SWRVE_METRO_BUILT_IN
#endif

#if SWRVE_METRO_BUILT_IN
using UnityEngine.Windows;
#else
using System.IO;
#endif

namespace Swrve
{
/// <summary>
/// Used internally to support all platform IO methods.
/// </summary>
public static class CrossPlatformFile
{
    public static void Delete (string path)
    {
#if SWRVE_METRO_BUILT_IN
        UnityEngine.Windows.File.Delete(path);
#else
        File.Delete (path);
#endif
    }

    public static bool Exists (string path)
    {
#if SWRVE_METRO_BUILT_IN
        return UnityEngine.Windows.File.Exists(path);
#else
        return System.IO.File.Exists (path);
#endif
    }

    public static byte[] ReadAllBytes (string path)
    {
#if SWRVE_METRO_BUILT_IN
        // Windows.File isn't raising exceptions for missing files.
        // Since the missing file errors are so ugly we guard against
        // them here.
        if (UnityEngine.Windows.File.Exists(path)) {
            return UnityEngine.Windows.File.ReadAllBytes(path);
        }
        return null;
#else
        byte[] buffer = null;
        using (FileStream fs = new FileStream(path, FileMode.Open)) {
            buffer = new byte[fs.Length];
            using (BinaryReader reader = new BinaryReader(fs)) {
                reader.Read (buffer, 0, (int)fs.Length);
            }
        }
        return buffer;
#endif
    }

    public static string LoadText (string path)
    {
#if SWRVE_METRO_BUILT_IN
        // Windows.File isn't raising exceptions for missing files.
        // Since the missing file errors are so ugly we guard against
        // them here.
        if (UnityEngine.Windows.File.Exists(path)) {
            byte[] bytes = UnityEngine.Windows.File.ReadAllBytes(path);
            if (bytes != null) {
                return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            }
        }
        return null;
#else
        string result = null;
        using (FileStream fs = new FileStream(path, FileMode.Open)) {
            using (StreamReader sr = new StreamReader(fs)) {
                result = sr.ReadToEnd ();
            }
        }
        return result;
#endif
    }

    public static void SaveBytes (string path, byte[] bytes)
    {
#if SWRVE_METRO_BUILT_IN
        UnityEngine.Windows.File.WriteAllBytes(path, bytes);
#else
        using (FileStream fs = File.Open(path, FileMode.Create)) {
            using (BinaryWriter binary = new BinaryWriter(fs)) {
                binary.Write (bytes);
            }
        }
#endif
    }

    public static void SaveText (string path, string data)
    {
#if SWRVE_METRO_BUILT_IN
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
        UnityEngine.Windows.File.WriteAllBytes(path, bytes);
#else
        using (FileStream fs = new FileStream(path, FileMode.Create)) {
            using (StreamWriter sw = new StreamWriter(fs)) {
                sw.Write (data);
            }
        }
#endif
    }
}
}
