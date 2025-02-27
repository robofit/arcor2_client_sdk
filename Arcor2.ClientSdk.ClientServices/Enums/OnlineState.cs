using System;
using Arcor2.ClientSdk.ClientServices.Models.Extras;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents state of a scene.
    /// </summary>
    public enum OnlineState {
        Starting,
        Started,
        Stopping,
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
