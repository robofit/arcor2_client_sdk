using Arcor2.ClientSdk.ClientServices.Enums;

namespace Arcor2.ClientSdk.ClientServices.Models.Extras
{
    /// <summary>
    /// Represents the current state of a scene or a project.
    /// </summary>
    public class SceneOnlineState
    {
        /// <summary>
        /// The online/start state of a scene or a project.
        /// </summary>
        public OnlineState State { get; }
        /// <summary>
        /// A message included with the state change.
        /// </summary>
        /// <remarks>
        /// Message is often included when scene starts stopping due to a failure.
        /// </remarks>
        public string? Message { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="SceneOnlineState"/> class.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="message">The optional message.</param>
        public SceneOnlineState(OnlineState state, string? message = null)
        {
            State = state;
            Message = message;
        }
    }
}
