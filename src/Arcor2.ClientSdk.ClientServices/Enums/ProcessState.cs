using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents a state of a long-running task (e.g., calibration).
    /// </summary>
    public enum ProcessState {
        /// <summary>
        /// The task has started and is currently executing.
        /// </summary>
        Started = 0,
        /// <summary>
        /// The task has successfully finished.
        /// </summary>
        Finished,
        /// <summary>
        /// The task failed.
        /// </summary>
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
