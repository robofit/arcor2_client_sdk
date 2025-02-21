using Arcor2.ClientSdk.ClientServices.Enums;

namespace Arcor2.ClientSdk.ClientServices.Models.Extras
{
    /// <summary>
    /// Represents the current state of the scene.
    /// </summary>
    public struct SceneState
    {
        /// <summary>
        /// The online/start state of the scene.
        /// </summary>
        public SceneOnlineState OnlineState { get; }
        /// <summary>
        /// A message included with the state change.
        /// </summary>
        /// <remarks>
        /// Message is often included when scene starts stopping due to a failure.
        /// </remarks>
        public string? Message { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="SceneState"/> class.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="message">The optional message.</param>
        public SceneState(SceneOnlineState state, string? message = null)
        {
            OnlineState = state;
            Message = message;
        }
    }
}
