using System;
using Arcor2.ClientSdk.ClientServices.Enums;

namespace Arcor2.ClientSdk.ClientServices.EventArguments
{
    /// <summary>
    /// Event args for package state changes.
    /// </summary>
    public class PackageStateEventArgs : EventArgs
    {
        /// <summary>
        /// The state of the package.
        /// </summary>
        public PackageState State { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="PackageStateEventArgs"/> class.
        /// </summary>
        public PackageStateEventArgs(PackageState state)
        {
            State = state;
        }
    }
}
