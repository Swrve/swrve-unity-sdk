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

namespace Swrve.Input
{
/// <summary
/// Used internally to react to input.
/// </summary>
public class NativeInputManager : IInputManager
{
    private static NativeInputManager instance;

    public static NativeInputManager Instance
    {
        get {
            if (instance == null) {
                instance = new NativeInputManager ();
            }
            return instance;
        }
    }

    private NativeInputManager ()
    {
    }

    bool IInputManager.GetMouseButtonUp (int buttonId)
    {
        return !UnityEngine.Input.GetMouseButton (buttonId);
    }

    bool IInputManager.GetMouseButtonDown (int buttonId)
    {
        return UnityEngine.Input.GetMouseButton (buttonId);
    }

    Vector3 IInputManager.GetMousePosition ()
    {
        Vector3 mousePosition = UnityEngine.Input.mousePosition;
        mousePosition.y = Screen.height - mousePosition.y;
        return mousePosition;
    }
}
}