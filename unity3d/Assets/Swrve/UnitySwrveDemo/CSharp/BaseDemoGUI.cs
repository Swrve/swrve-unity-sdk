using System;
using UnityEngine;

/// <summary>
/// Base class for the Swrve demo. Please have a look at DemoGUI instead.
/// </summary>
public class BaseDemoGUI : MonoBehaviour
{
    protected const int virtualWidth = 640;
    protected const int virtualHeight = 480;
    public bool UIVisible = true;

    protected struct ButtonDef {
        public Rect VirtualRect
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public ButtonDef (Rect virtualRect, string name) : this()
        {
            VirtualRect = virtualRect;
            Name = name;
        }
    };

    public BaseDemoGUI ()
    {
        ButtonDefs = new ButtonDef[] {
            new ButtonDef (new Rect (230, 45, 180, 32), "Named Event"),
            new ButtonDef (new Rect (230, 85, 180, 32), "User update"),
            new ButtonDef (new Rect (230, 125, 180, 32), "Purchase Item"),
            new ButtonDef (new Rect (230, 165, 180, 32), "IAP: Item"),
            new ButtonDef (new Rect (230, 205, 180, 32), "IAP: Virtual Currency"),
            new ButtonDef (new Rect (230, 245, 180, 32), "Apple IAP"),
            new ButtonDef (new Rect (230, 285, 180, 32), "Currency Given"),
            new ButtonDef (new Rect (230, 325, 180, 32), "AB Test Resources"),
            new ButtonDef (new Rect (110, 405, 112, 32), "Send To Swrve"),
            new ButtonDef (new Rect (418, 405, 112, 32), "Save To Disk"),
            new ButtonDef (new Rect (230, 405, 180, 32), "Trigger message")
        };
        buttonPressed = new bool[System.Enum.GetNames (typeof(Buttons)).Length];
    }

    void Awake ()
    {
        useGUILayout = false;
    }

    protected static Rect ActualRect (Rect virtualRect)
    {
        float scale = (float)Screen.height / virtualHeight;
        float offset = (Screen.width - virtualWidth * scale) * 0.5f;

        float x = virtualRect.x * scale + offset;
        float y = virtualRect.y * scale;
        float w = virtualRect.width * scale;
        float h = virtualRect.height * scale;

        return new Rect (x, y, w, h);
    }

    protected bool[] buttonPressed;
    protected static ButtonDef[] ButtonDefs;

    public void ClearButtons ()
    {
        System.Array.Clear (buttonPressed, 0, buttonPressed.Length);
    }

    void OnGUI ()
    {
        if (!UIVisible) {
            return;
        }
        for (int i = 0; i < ButtonDefs.Length; i++) {
            if (GUI.Button (ActualRect (ButtonDefs [i].VirtualRect), ButtonDefs [i].Name))
                buttonPressed [i] = true;
        }
    }

    protected enum Buttons {
        SendEvent,
        SendUserAttributes,
        PurchaseItem,
        InAppItemPurchase,
        InAppCurrencyPurchase,
        RealIap,
        CurrencyGiven,
        UserResources,
        SendToSwrve,
        SaveToDisk,
        TriggerMessage
    };
}

