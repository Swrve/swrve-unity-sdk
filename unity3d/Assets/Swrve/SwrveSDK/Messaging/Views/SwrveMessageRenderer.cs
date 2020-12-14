using System;
using UnityEngine;
using SwrveUnity.Input;

namespace SwrveUnity.Messaging
{
#pragma warning disable 0618
/// <summary>
/// Used internally to render in-app messages using Unity IMGUI.
/// </summary>
public class SwrveMessageRenderer
{
    protected static Rect WholeScreen = new Rect ();

    /// <summary>
    /// Used to animate in-app messages.
    /// </summary>
    [System.Obsolete("Use SwrveInAppMessageConfig instead. This will be removed in 8.0")]
    public static ISwrveMessageAnimator Animator;

    private readonly ISwrveMessageAnimator animator;

    private readonly SwrveMessageTextTemplatingResolver templatingResolver;

    private SwrveMessageFormat format;

    private bool renderBackgroundColor;

    // Visible only for testing
    public SwrveWidgetView[] widgetViews;

    public SwrveMessageRenderer(ISwrveMessageAnimator animator,
                                SwrveMessageTextTemplatingResolver templatingResolver)
    {
        if (animator == null) {
            this.animator = Animator; // default back to the static setter, will be removed in 8.0
        } else {
            this.animator = animator;
        }
        this.templatingResolver = templatingResolver;
    }

    public void InitMessage (SwrveMessageFormat format, SwrveInAppMessageConfig inAppConfig, SwrveOrientation deviceOrientation, bool afterRotation = false)
    {
        this.format = format;
        format.Init (deviceOrientation);
        renderBackgroundColor = format.BackgroundColor.HasValue;

        if (!afterRotation) {
            if (animator != null) {
                animator.InitMessage (format);
            } else {
                format.InitAnimation (new Point (0, 0), new Point (0, 0));
            }
        }

        // Create widgets to render and use the cached personalization values
        widgetViews = new SwrveWidgetView[format.Images.Count + format.Buttons.Count];
        int eindex = 0;
        for(int ii = 0; ii < format.Images.Count; ii++) {
            SwrveImage image = format.Images[ii];
            SwrveWidgetView renderer;
            if (image.Text != null) {
                // Get cached resolved template
                string resolvedTextTemplate = templatingResolver.TextResolution[image];
                renderer = new SwrveMessagePersonalizedWidgetView(image, resolvedTextTemplate, inAppConfig);
            } else {
                renderer = new SwrveImageView(image);
            }
            widgetViews[eindex++] = renderer;
        }
        for(int bi = 0; bi < format.Buttons.Count; bi++) {
            SwrveButton button = format.Buttons[bi];
            SwrveWidgetView renderer;
            if (button.Text != null) {
                string resolvedTextTemplate = templatingResolver.TextResolution[button];
                renderer = new SwrveMessagePersonalizedWidgetView(button, resolvedTextTemplate, inAppConfig);
            } else {
                renderer = new SwrveButtonView(button, inAppConfig.ButtonClickTintColor);
            }
            widgetViews[eindex++] = renderer;
        }
    }

    public void DrawMessage (int screenWidth, int screenHeight)
    {
        int centerx = (int)(Screen.width / 2) + format.Message.Position.X;
        int centery = (int)(Screen.height / 2) + format.Message.Position.Y;

        if (animator != null) {
            animator.AnimateMessage (format);
        }

        if (renderBackgroundColor) {
            Color backgroundColor = format.BackgroundColor.Value;
            backgroundColor.a = backgroundColor.a * format.Message.BackgroundAlpha;
            GUI.color = backgroundColor;
            WholeScreen.width = screenWidth;
            WholeScreen.height = screenHeight;
            GUI.DrawTexture (WholeScreen, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0.0f);
            GUI.color = Color.white;
        }

        bool rotatedFormat = format.Rotate;
        // Rotate the inner message if necessary
        if (rotatedFormat) {
            Vector2 pivotPoint = new Vector2 (centerx, centery);
            GUIUtility.RotateAroundPivot (90, pivotPoint);
        }

        float scale = format.Scale * format.Message.AnimationScale;
        for (int ii = 0; ii < widgetViews.Length; ii++) {
            widgetViews[ii].Render(scale, centerx, centery, rotatedFormat, animator);
        }

        // Do closing logic
        if ((animator == null && format.Closing) || (animator != null && animator.IsMessageDismissed (format))) {
            format.Dismissed = true;
            format.UnloadAssets ();
        }
    }

    public void ProcessButtonDown (IInputManager inputManager)
    {
        Vector3 mousePosition = inputManager.GetMousePosition ();
        for(int bi = 0; bi < format.Buttons.Count; bi++) {
            SwrveButton button = format.Buttons[bi];
            if (button.PointerRect.Contains (mousePosition)) {
                button.Pressed = true;
            }
        }
    }

    public SwrveButtonClickResult ProcessButtonUp (IInputManager inputManager)
    {
        SwrveButtonClickResult clickResult = null;

        // Capture last button clicked (last rendered, rendered on top)
        for (int i = format.Buttons.Count - 1; i >= 0 && clickResult == null; i--) {
            SwrveButton button = format.Buttons [i];
            Vector3 mousePosition = inputManager.GetMousePosition ();
            if (button.PointerRect.Contains (mousePosition) && button.Pressed) {
                string resolvedAction = templatingResolver.ActionResolution[button];
                clickResult = new SwrveButtonClickResult(button, resolvedAction);
            } else {
                button.Pressed = false;
            }
        }

        return clickResult;
    }
}
#pragma warning restore 0618
}
