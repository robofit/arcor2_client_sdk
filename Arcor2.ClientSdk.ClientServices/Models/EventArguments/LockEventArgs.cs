using System;

namespace Arcor2.ClientSdk.ClientServices.Models.EventArguments {
    /// <summary>
    /// Event args for lock events.
    /// </summary>
    public class LockEventArgs : EventArgs {
        /// <summary>
        /// The user which locked corresponding object.
        /// </summary>
        public string Owner { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="LockEventArgs"/> class.
        /// </summary>
        public LockEventArgs(string owner ) {
            Owner = owner;
        }
    }
}