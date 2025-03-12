using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents a mode of robot stepping.
    /// </summary>
    public enum StepMode {
        /// <summary>
        /// Step in the world coordinate system.
        /// </summary>
        World = 0,
        /// <summary>
        /// Step relative to the robot's current pose.
        /// </summary>
        Robot,
        /// <summary>
        /// Step in a user-defined coordinate system.
        /// </summary>
        /// <remarks>
        /// Not supported as of ARCOR2 server v1.5.0.
        /// </remarks> 
        User,
        /// <summary>
        /// Step relative to a specified pose.
        /// </summary>
        Relative
    }

    internal static class StepModeExtensions {
        public static StepRobotEefRequestArgs.ModeEnum MapToOpenApiModeEnum(this StepMode stepMode) {
            return stepMode switch {
                StepMode.World => StepRobotEefRequestArgs.ModeEnum.World,
                StepMode.Robot => StepRobotEefRequestArgs.ModeEnum.Robot,
                StepMode.User => StepRobotEefRequestArgs.ModeEnum.User,
                StepMode.Relative => StepRobotEefRequestArgs.ModeEnum.Relative,
                _ => throw new InvalidOperationException("Invalid StepMode value.")
            };
        }
    }
}