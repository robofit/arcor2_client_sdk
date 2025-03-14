using System;
using System.Collections.Generic;

namespace Arcor2.ClientSdk.ClientServices.EventArguments {
    /// <summary>
    ///     Event args for project action execution result.
    /// </summary>
    public class ActionExecutedEventArgs : EventArgs {
        /// <summary>
        ///     Initializes a new instance of <see cref="ActionStartingEventArgs" /> class.
        /// </summary>
        public ActionExecutedEventArgs(IList<string> results, string? error = null) {
            Results = results;
            Error = error;
        }

        /// <summary>
        ///     The error message, if applicable.
        /// </summary>
        public string? Error { get; }

        /// <summary>
        ///     The error message.
        /// </summary>
        public IList<string> Results { get; }
    }
}