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
              try {
                binary.Write (bytes);
              } catch (System.Exception ex) {
                UnityEngine.Debug.LogError (ex);
              }
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
              try {
                sw.Write (data);
              } catch (System.Exception ex) {
                UnityEngine.Debug.LogError (ex);
              }
            }
        }
#endif
    }
}
}
