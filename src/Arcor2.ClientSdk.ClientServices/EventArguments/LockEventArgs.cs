using System;

namespace Arcor2.ClientSdk.ClientServices.EventArguments {
    /// <summary>
    ///     Event args for lock events.
    /// </summary>
    public class LockEventArgs : EventArgs {
        /// <summary>
        ///     Initializes a new instance of <see cref="LockEventArgs" /> class.
        /// </summary>
        public LockEventArgs(string owner) {
            Owner = owner;
        }

        /// <summary>
        ///     The user which locked corresponding object.
        /// </summary>
        public string Owner { get; }
    }
}