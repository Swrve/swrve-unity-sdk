using System;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Implement this interface to handle callbacks of clipboard buttons
    /// inside your in-app messages.
    /// </summary>
    public interface ISwrveClipboardButtonListener
    {
        /// <summary>
        /// This method is invoked when an clipboard button has been pressed on an in-app message.
        /// </summary>
        /// <param name="clipboardContents">
        /// Text contents that have been copied to the clipboard.
        /// </param>
        void OnAction(string clipboardContents);
    }
}
