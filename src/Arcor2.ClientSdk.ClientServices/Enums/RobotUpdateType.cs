using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents a requested type of update event.
    /// </summary>
    public enum RobotUpdateType {
        /// <summary>
        /// Updates for end effector poses.
        /// </summary>
        Pose = 0,
        /// <summary>
        /// Updates for joint values.
        /// </summary>
        Joints
    }

    internal static class RobotUpdateTypeExtensions {
        public static RegisterForRobotEventRequestArgs.WhatEnum MapToOpenApiWhatEnum(this RobotUpdateType type) {
            return type switch {
                RobotUpdateType.Pose => RegisterForRobotEventRequestArgs.WhatEnum.EefPose,
                RobotUpdateType.Joints => RegisterForRobotEventRequestArgs.WhatEnum.Joints,
                _ => throw new InvalidOperationException("Invalid RobotUpdateType value.")
            };
        }
    }
}
