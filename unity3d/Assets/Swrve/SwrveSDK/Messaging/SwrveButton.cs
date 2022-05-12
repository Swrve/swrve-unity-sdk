using UnityEngine;

namespace SwrveUnity.Messaging
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
        /// ID of the target installation app
        /// </summary>
        public int AppId;

        /// <summary>
        /// Name of the button
        /// </summary>
        public string Name;

        /// <summary>
        /// Button Id of the button¬
        /// </summary>
        public long ButtonId;
    }
}
