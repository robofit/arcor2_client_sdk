using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents a pivot of an object.
    /// </summary>
    public enum Pivot {
        /// <summary>
        /// The pivot is on top of the object.
        /// </summary>
        Top,
        /// <summary>
        /// The pivot is in middle of the object.
        /// </summary>
        Middle,
        /// <summary>
        /// The pivot is on bottom of the object.
        /// </summary>
        Bottom
    }

    internal static class PivotExtensions {
        public static UpdateObjectPoseUsingRobotRequestArgs.PivotEnum MapToOpenApiPivotEnum(this Pivot pivot) {
            return pivot switch {
                Pivot.Top => UpdateObjectPoseUsingRobotRequestArgs.PivotEnum.Top,
                Pivot.Middle => UpdateObjectPoseUsingRobotRequestArgs.PivotEnum.Middle,
                Pivot.Bottom => UpdateObjectPoseUsingRobotRequestArgs.PivotEnum.Bottom,
                _ => throw new InvalidOperationException("Invalid Pivot value.")
            };
        }
    }
}
