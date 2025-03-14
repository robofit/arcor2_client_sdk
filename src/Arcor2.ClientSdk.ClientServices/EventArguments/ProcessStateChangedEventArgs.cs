using Arcor2.ClientSdk.ClientServices.Enums;
using System;

namespace Arcor2.ClientSdk.ClientServices.EventArguments {
    /// <summary>
    ///     Event args for process state changes.
    /// </summary>
    public class ProcessStateChangedEventArgs : EventArgs {
        /// <summary>
        ///     Initializes a new instance of <see cref="ProcessStateChangedEventArgs" /> class.
        /// </summary>
        public ProcessStateChangedEventArgs(ProcessState state, string? message = null) {
            State = state;
            Message = message;
        }

        /// <summary>
        ///     The state of the process.
        /// </summary>
        public ProcessState State { get; }

        /// <summary>
        ///     The message.
        /// </summary>
        public string? Message { get; }
    }
}