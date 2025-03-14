using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    ///     Represents a spatial axis.
    /// </summary>
    public enum Axis {
        /// <summary>
        ///     The X axis.
        /// </summary>
        X = 0,

        /// <summary>
        ///     The Y axis.
        /// </summary>
        Y,

        /// <summary>
        ///     The Z axis.
        /// </summary>
        Z
    }

    internal static class AxisExtensions {
        public static StepRobotEefRequestArgs.AxisEnum MapToOpenApiAxisEnum(this Axis axis) =>
            axis switch {
                Axis.X => StepRobotEefRequestArgs.AxisEnum.X,
                Axis.Y => StepRobotEefRequestArgs.AxisEnum.Y,
                Axis.Z => StepRobotEefRequestArgs.AxisEnum.Z,
                _ => throw new InvalidOperationException("Invalid Axis value.")
            };
    }
}