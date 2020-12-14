using System;
using SwrveUnity.Messaging;
using UnityEngine;

namespace SwrveUnity
{
/// <summary>
/// Configuration for the Swrve SDK in-app messages.
/// </summary>
public class SwrveInAppMessageConfig
{
    /// <summary>
    /// The in-app message button tint color when a button is clicked.
    /// </summary>
    public Color ButtonClickTintColor = new Color (0.5f, 0.5f, 0.5f);

    /// <summary>
    /// The in-app message personalized text background color.
    /// </summary>
    public Color PersonalizedTextBackgroundColor = Color.clear;

    /// <summary>
    /// The in-app message personalized text foreground color.
    /// </summary>
    public Color PersonalizedTextForegroundColor = Color.black;

    /// <summary>
    /// The in-app message personalized text font.
    /// </summary>
    public Font PersonalizedTextFont;

    /// <summary>
    /// Listener for push notifications received in the app.
    /// </summary>
    public ISwrveMessagePersonalizationProvider PersonalizationProvider = null;

    /// <summary>
    /// Used to animate in-app messages.
    /// </summary>
    public ISwrveMessageAnimator Animator;
}
}
