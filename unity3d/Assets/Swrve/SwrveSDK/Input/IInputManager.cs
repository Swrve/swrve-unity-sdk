using System;
using UnityEngine;

namespace Swrve.Input
{
/// <summary
/// Used internally to react to input.
/// </summary>
public interface IInputManager
{
    bool GetMouseButtonUp (int buttonId);

    bool GetMouseButtonDown (int buttonId);

    Vector3 GetMousePosition ();
}
}

