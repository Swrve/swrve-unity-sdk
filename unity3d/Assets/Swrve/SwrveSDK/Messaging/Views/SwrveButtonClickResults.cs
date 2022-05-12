using System;
using UnityEngine;

namespace SwrveUnity.Messaging
{
    public class SwrveButtonClickResult
    {
        public readonly SwrveButton Button;

        public readonly string ResolvedAction;

        public SwrveButtonClickResult(SwrveButton button, string resolvedAction)
        {
            this.Button = button;
            this.ResolvedAction = resolvedAction;
        }
    }
}
