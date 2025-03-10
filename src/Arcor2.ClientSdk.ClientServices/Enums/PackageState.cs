using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents state of a package.
    /// </summary>
    public enum PackageState { 
        Running = 0,
        Stopping,
        Stopped,
        Pausing,
        Paused,
        Resuming,
        Undefined
    }

    internal static class PackageStateEnumExtensions {
        public static PackageState MapToCustomPackageModeEnum(this PackageStateData.StateEnum stepMode) {
            return stepMode switch {
                PackageStateData.StateEnum.Running => PackageState.Running,
                PackageStateData.StateEnum.Stopping => PackageState.Stopping,
                PackageStateData.StateEnum.Stopped => PackageState.Stopped,
                PackageStateData.StateEnum.Pausing => PackageState.Pausing,
                PackageStateData.StateEnum.Paused => PackageState.Paused,
                PackageStateData.StateEnum.Resuming => PackageState.Resuming,
                PackageStateData.StateEnum.Undefined => PackageState.Undefined,
                _ => throw new InvalidOperationException("Invalid PackageStateData.StateEnum value.")
            };
        }
    }
}
