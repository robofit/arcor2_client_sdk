using System;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums
{
    /// <summary>
    /// Represents state of a scene.
    /// </summary>
    public enum OnlineState {
        /// <summary>
        /// The scene is starting. It is illegal to invoke RPCs requiring started scene in this state. 
        /// </summary>
        Starting,
        /// <summary>
        /// The scene is started. 
        /// </summary>
        Started,
        /// <summary>
        /// The scene is starting. It is illegal to invoke RPCs requiring stopped scene in this state. 
        /// </summary>
        Stopping,
        /// <summary>
        /// The scene is stopped. 
        /// </summary>
        Stopped
    }

    internal static class SceneStateExtensions {
        public static SceneOnlineState MapToCustomSceneStateEnum(this SceneStateData data) {
            return new SceneOnlineState(data.State switch {
                SceneStateData.StateEnum.Starting => OnlineState.Starting,
                SceneStateData.StateEnum.Started => OnlineState.Started,
                SceneStateData.StateEnum.Stopping => OnlineState.Stopping,
                SceneStateData.StateEnum.Stopped => OnlineState.Stopped,
                _ => throw new InvalidOperationException("Invalid SceneState value.")
            }, data.Message);
        }
    }
}
