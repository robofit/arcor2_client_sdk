using System;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Models.Extras;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Extensions
{
    internal static class SceneStateExtensions {
        /// <summary>
        /// Maps OpenApi model to custom scene state model.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static SceneOnlineState ToCustomSceneStateObject(this SceneStateData data) {
            return new SceneOnlineState(data.State switch {
                SceneStateData.StateEnum.Starting => OnlineState.Starting,
                SceneStateData.StateEnum.Started => OnlineState.Started,
                SceneStateData.StateEnum.Stopping => OnlineState.Stopping,
                SceneStateData.StateEnum.Stopped => OnlineState.Stopped,
                _ => throw new InvalidOperationException("Invalid SceneState enum value.")
            }, data.Message);
        }
    }
}
