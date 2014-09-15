using System;
using UnityEngine;
using System.Collections.Generic;

namespace Swrve.Messaging
{
/// <summary>
/// Custom message animator. Implement and use this interface with your
/// instance of the SDK to provide an in-game look to your messages:
/// SwrveMessageRenderer.Animator = new YourAnimator();
/// </summary>
public interface ISwrveMessageAnimator
{
    /// <summary>
    /// Called once per message when getting ready to render.
    /// </summary>
    /// <param name="format">
    /// In-app message information such as position, size and content.
    /// </param>
    void InitMessage (SwrveMessageFormat format);

    /// <summary>
    /// Called on every frame when a message is being rendered.
    /// </summary>
    /// <param name="format">
    /// In-app message information such as position, size and content.
    /// </param>
    void AnimateMessage (SwrveMessageFormat format);

    /// <summary>
    /// Called on every frame when a message button is being rendered.
    /// </summary>
    /// <param name="button">
    /// In-app message button information such as position, size and content.
    /// </param>
    void AnimateButton (SwrveButton button);

    /// <summary>
    /// Called on every frame when a pressed message button is being rendered.
    /// </summary>
    /// <param name="button">
    /// In-app message button information such as position, size and content.
    /// </param>
    void AnimateButtonPressed (SwrveButton button);

    /// <summary>
    /// Called on every frame when a message is being closed.
    /// </summary>
    /// <param name="format">
    /// In-app message information such as position, size and content.
    /// </param>
    /// <returns>
    /// Return true when the message dismiss animation is complete.
    /// </returns>
    bool IsMessageDismissed (SwrveMessageFormat format);
}
}
