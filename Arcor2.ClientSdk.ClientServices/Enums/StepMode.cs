using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    public enum StepMode {
        World = 0,
        Robot,
        User,
        Relative
    }

    internal static class StepModeExtensions {
        public static StepRobotEefRequestArgs.ModeEnum ToOpenApiModeEnum(this StepMode stepMode) {
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