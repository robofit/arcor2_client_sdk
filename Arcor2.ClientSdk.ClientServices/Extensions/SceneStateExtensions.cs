using System;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using SceneState = Arcor2.ClientSdk.ClientServices.Models.Extras.SceneState;

namespace Arcor2.ClientSdk.ClientServices.Extensions
{
    internal static class SceneStateExtensions {
        /// <summary>
        /// Maps OpenApi model to custom scene state model.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static SceneState ToCustomSceneState(this SceneStateData data) {
            return new SceneState(data.State switch {
                SceneStateData.StateEnum.Starting => SceneOnlineState.Starting,
                SceneStateData.StateEnum.Started => SceneOnlineState.Started,
                SceneStateData.StateEnum.Stopping => SceneOnlineState.Stopping,
                SceneStateData.StateEnum.Stopped => SceneOnlineState.Stopped,
                _ => throw new InvalidOperationException("Invalid SceneState enum value.")
            }, data.Message);
        }
    }
}
