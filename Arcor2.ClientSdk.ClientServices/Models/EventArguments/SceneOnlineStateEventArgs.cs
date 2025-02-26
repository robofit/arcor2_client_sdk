using System;
using Arcor2.ClientSdk.ClientServices.Models.Extras;

namespace Arcor2.ClientSdk.ClientServices.Models.EventArguments
{
    /// <summary>
    /// Event args for scene state changes.
    /// </summary>
    public class SceneOnlineStateEventArgs : EventArgs
    {
        /// <summary>
        /// The state of the scene.
        /// </summary>
        public SceneOnlineState State { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="SceneOnlineStateEventArgs"/> class.
        /// </summary>
        public SceneOnlineStateEventArgs(SceneOnlineState state)
        {
            State = state;
        }
    }
}
