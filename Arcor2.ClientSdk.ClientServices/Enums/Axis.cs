using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    public enum Axis {
        X = 0,
        Y,
        Z
    }

    internal static class AxisExtensions {
        public static StepRobotEefRequestArgs.AxisEnum MapToOpenApiAxisEnum(this Axis axis) {
            return axis switch {
                Axis.X => StepRobotEefRequestArgs.AxisEnum.X,
                Axis.Y => StepRobotEefRequestArgs.AxisEnum.Y,
                Axis.Z => StepRobotEefRequestArgs.AxisEnum.Z,
                _ => throw new InvalidOperationException("Invalid Axis value.")
            };
        }
    }
}
