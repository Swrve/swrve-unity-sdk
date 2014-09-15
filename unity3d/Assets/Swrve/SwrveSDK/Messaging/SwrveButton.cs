using UnityEngine;

namespace Swrve.Messaging
{
/// <summary>
/// In-app message button.
/// </summary>
public class SwrveButton : SwrveWidget
{
    /// <summary>
    /// Name of the background image
    /// </summary>
    public string Image;

    /// <summary>
    /// Custom action string for the button
    /// </summary>
    public string Action;

    /// <summary>
    /// Button action type
    /// </summary>
    public SwrveActionType ActionType;

    /// <summary>
    /// Parent message associated with this button
    /// </summary>
    public SwrveMessage Message;

    /// <summary>
    /// ID of the target installation game
    /// </summary>
    public int GameId;

    /// <summary>
    /// Name of the button
    /// </summary>
    public string Name;

    /// <summary>
    /// Has it been pressed
    /// </summary>
    public bool Pressed = false;
}
}
