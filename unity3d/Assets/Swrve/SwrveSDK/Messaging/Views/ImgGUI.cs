using System;
using UnityEngine;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Used internally to redirect calls to GUI class and make it testable.
/// </summary>
public class ImgGUI
{
    // Visible for tests (in cmdline)
    public static bool enabled = true;

    public static Color color
    {
        set {
            if (enabled) {
                GUI.color = value;
            }
        }
    }

    public static int depth
    {
        set {
            if (enabled) {
                GUI.depth = value;
            }
        }

        get {
            if (enabled) {
                return GUI.depth;
            }
            return 0;
        }
    }

    public static Matrix4x4 matrix
    {
        set {
            if (enabled) {
                GUI.matrix = value;
            }
        }

        get {
            if (enabled) {
                return GUI.matrix;
            }
            return Matrix4x4.identity;
        }
    }

    public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect)
    {
        if (enabled) {
            GUI.DrawTexture(position, image, scaleMode, alphaBlend, imageAspect);
        }
    }

    public static void Label(Rect position, GUIContent text, GUIStyle style)
    {
        if (enabled) {
            GUI.Label(position, text, style);
        }
    }

    public static void Box(Rect position, string text)
    {
        if (enabled) {
            GUI.Box(position, text);
        }
    }
}
}
