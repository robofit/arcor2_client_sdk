using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    ///     Represents state of a package.
    /// </summary>
    public enum PackageState {
        /// <summary>
        ///     The package is currently starting.
        /// </summary>
        /// <remarks>
        ///     The ARCOR2 server 1.5.0 currently fails to generate this state in its OpenAPI specification.
        ///     Due to this, this state won't be set until the issue is fixed.
        /// </remarks>
        Starting = 0,

        /// <summary>
        ///     The package is currently executing.
        /// </summary>
        Running,

        /// <summary>
        ///     The package is currently stopping.
        /// </summary>
        Stopping,

        /// <summary>
        ///     The package is stopped.
        /// </summary>
        /// <remarks>
        ///     A show main menu request usually follows this state.
        /// </remarks>
        Stopped,

        /// <summary>
        ///     The package is pausing.
        /// </summary>
        Pausing,

        /// <summary>
        ///     The package is paused.
        /// </summary>
        Paused,

        /// <summary>
        ///     The package is resuming execution.
        /// </summary>
        Resuming,

        /// <summary>
        ///     The state of the package is undefined.
        /// </summary>
        /// <remarks>
        ///     This is the default state when a package is not open for both the client and the server.
        ///     During active package usage, this state should never occur.
        /// </remarks>
        Undefined
    }

    internal static class PackageStateEnumExtensions {
        public static PackageState MapToCustomPackageModeEnum(this PackageStateData.StateEnum stepMode) =>
            stepMode switch {
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