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

using System;
using UnityEngine;

namespace Swrve.Messaging
{
/// <summary>
/// Used internally to render in-app messages using Unity GUI.
/// </summary>
public class SwrveMessageRenderer
{
    protected static readonly Color ButtonPressedColor = new Color (0.5f, 0.5f, 0.5f);
    protected static Texture2D blankTexture;
    protected static Rect WholeScreen = new Rect ();

    /// <summary>
    /// Used to animate in-app messages.
    /// </summary>
    public static ISwrveMessageAnimator Animator;

    protected static Texture2D GetBlankTexture ()
    {
        if (blankTexture == null) {
            // Create blank texture for background
            blankTexture = new Texture2D (2, 2, TextureFormat.ARGB32, false);
            blankTexture.SetPixel (0, 0, Color.white);
            blankTexture.SetPixel (1, 0, Color.white);
            blankTexture.SetPixel (0, 1, Color.white);
            blankTexture.SetPixel (1, 1, Color.white);
            blankTexture.Apply (false, true);
        }

        return blankTexture;
    }

    public static void InitMessage (SwrveMessageFormat format)
    {
        if (Animator != null) {
            Animator.InitMessage (format);
        } else {
            format.Init (new Point (0, 0), new Point (0, 0));
        }
    }

    public static void AnimateMessage (SwrveMessageFormat format)
    {
        if (Animator != null) {
            Animator.AnimateMessage (format);
        }
    }

    public static void DrawMessage (SwrveMessageFormat format, int centerx, int centery)
    {
        if (Animator != null) {
            AnimateMessage (format);
        }

        if (format.Message.BackgroundColor.HasValue && GetBlankTexture () != null) {
            Color backgroundColor = format.Message.BackgroundColor.Value;
            backgroundColor.a = format.Message.BackgroundAlpha;
            GUI.color = backgroundColor;
            WholeScreen.width = Screen.width;
            WholeScreen.height = Screen.height;
            GUI.DrawTexture (WholeScreen, blankTexture, ScaleMode.StretchToFill, true, 10.0f);
            GUI.color = Color.white;
        }

        // Rotate the inner message if necessary
        if (format.Rotate) {
            Vector2 pivotPoint = new Vector2 (Screen.width / 2, Screen.height / 2);
            GUIUtility.RotateAroundPivot (90, pivotPoint);
        }

        float scale = format.Scale * format.Message.AnimationScale;
        GUI.color = Color.white;
        // Draw images
        foreach (SwrveImage image in format.Images) {
            if (image.Texture != null) {
                float computedSize = scale * image.AnimationScale;
                Point centerPoint = image.GetCenteredPosition (image.Texture.width, image.Texture.height, computedSize, scale);
                centerPoint.X += centerx;
                centerPoint.Y += centery;
                image.Rect.x = centerPoint.X;
                image.Rect.y = centerPoint.Y;
                image.Rect.width = image.Texture.width * computedSize;
                image.Rect.height = image.Texture.height * computedSize;

                GUI.DrawTexture (image.Rect, image.Texture, ScaleMode.StretchToFill, true, 10.0f);
            } else {
                GUI.Box (image.Rect, image.File);
            }
        }

        // Draw buttons
        foreach (SwrveButton button in format.Buttons) {
            if (button.Texture != null) {
                float computedSize = scale * button.AnimationScale;
                Point centerPoint = button.GetCenteredPosition (button.Texture.width, button.Texture.height, computedSize, scale);
                centerPoint.X += centerx;
                centerPoint.Y += centery;
                button.Rect.x = centerPoint.X;
                button.Rect.y = centerPoint.Y;
                button.Rect.width = button.Texture.width * computedSize;
                button.Rect.height = button.Texture.height * computedSize;
                if (Animator != null) {
                    Animator.AnimateButtonPressed (button);
                } else {
                    GUI.color = (button.Pressed) ? ButtonPressedColor : Color.white;
                }
                GUI.DrawTexture (button.Rect, button.Texture, ScaleMode.StretchToFill, true, 10.0f);
            } else {
                GUI.Box (button.Rect, button.Image);
            }
            GUI.color = Color.white;
        }

        // Do closing logic
        if ((Animator == null && format.Closing) || (Animator != null && Animator.IsMessageDismissed (format))) {
            format.Dismissed = true;
            format.UnloadAssets ();
        }
    }
}
}

