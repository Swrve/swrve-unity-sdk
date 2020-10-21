using System;
using UnityEngine;

namespace SwrveUnity.Messaging
{
/// <summary>
/// Used internally to render in-app message widgets using Unity IMGUI.
/// </summary>
public interface SwrveWidgetView
{
    void Render(float scale, int centerx, int centery, bool rotatedFormat, ISwrveMessageAnimator animator);
}
}
