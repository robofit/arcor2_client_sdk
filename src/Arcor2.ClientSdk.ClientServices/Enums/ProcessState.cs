using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents state of a package.
    /// </summary>
    public enum ProcessState {
        Started = 0,
        Finished,
        Failed
    }

    internal static class ProcessStateEnumExtensions {
        public static ProcessState MapToCustomProcessStateEnum(this ProcessStateData.StateEnum state) {
            return state switch {
                ProcessStateData.StateEnum.Started => ProcessState.Started,
                ProcessStateData.StateEnum.Finished => ProcessState.Finished,
                ProcessStateData.StateEnum.Failed => ProcessState.Failed,
                _ => throw new InvalidOperationException("Invalid ProcessStateData.StateEnum value.")
            };
        }
    }
}
