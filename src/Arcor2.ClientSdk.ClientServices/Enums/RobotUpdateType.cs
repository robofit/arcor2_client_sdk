using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    public enum RobotUpdateType {
        Pose = 0,
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
