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
            set
            {
                if (enabled)
                {
                    GUI.color = value;
                }
            }
        }

        public static int depth
        {
            set
            {
                if (enabled)
                {
                    GUI.depth = value;
                }
            }

            get
            {
                if (enabled)
                {
                    return GUI.depth;
                }
                return 0;
            }
        }

        public static Matrix4x4 matrix
        {
            set
            {
                if (enabled)
                {
                    GUI.matrix = value;
                }
            }

            get
            {
                if (enabled)
                {
                    return GUI.matrix;
                }
                return Matrix4x4.identity;
            }
        }

        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect)
        {
            if (enabled)
            {
                GUI.DrawTexture(position, image, scaleMode, alphaBlend, imageAspect);
            }
        }

        public static void Label(Rect position, GUIContent text, GUIStyle style)
        {
            if (enabled)
            {
                GUI.Label(position, text, style);
            }
        }

        public static void Box(Rect position, string text)
        {
            if (enabled)
            {
                GUI.Box(position, text);
            }
        }


        public static Vector2 ClickEventScrollView(Rect position, Vector2 currentScrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical)
        {
            if (enabled)
            {
                float heightDiff = viewRect.height - position.height;
                if (Event.current.type == EventType.MouseDrag)
                {
                    if (position.Contains(Event.current.mousePosition))
                    {
                        if (currentScrollPosition.y <= heightDiff && currentScrollPosition.y >= 0)
                        {
                            currentScrollPosition.y += Event.current.delta.y;
                        }

                        if (currentScrollPosition.y > heightDiff)
                        {
                            currentScrollPosition.y = heightDiff;
                        }

                        if (currentScrollPosition.y < 0)
                        {
                            currentScrollPosition.y = 0;
                        }
                    }
                }
                return BeginScrollView(position, currentScrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical);
            }

            return Vector2.zero;
        }


        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical)
        {
            if (enabled)
            {
                return GUI.BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical);
            }
            return Vector2.zero;
        }

        public static void EndScrollView()
        {
            if (enabled)
            {
                GUI.EndScrollView();
            }
        }
    }
}
