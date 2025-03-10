using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    public enum Pivot {
        Top,
        Middle,
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
