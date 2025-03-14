using Arcor2.ClientSdk.ClientServices.Enums;
using System;

namespace Arcor2.ClientSdk.ClientServices.EventArguments {
    /// <summary>
    ///     Event args for package state changes.
    /// </summary>
    public class PackageStateEventArgs : EventArgs {
        /// <summary>
        ///     Initializes a new instance of <see cref="PackageStateEventArgs" /> class.
        /// </summary>
        public PackageStateEventArgs(PackageState state) {
            State = state;
        }

        /// <summary>
        ///     The state of the package.
        /// </summary>
        public PackageState State { get; }
    }
}