using System;
using System.Collections.Generic;

namespace Arcor2.ClientSdk.ClientServices.Models.EventArguments {
    /// <summary>
    /// Event args for package action state before execution report.
    /// </summary>
    public class ActionStartingEventArgs : EventArgs {
        /// <summary>
        /// The action parameters.
        /// </summary>
        public IList<string> Parameters { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ActionStartingEventArgs"/> class.
        /// </summary>
        public ActionStartingEventArgs(IList<string> parameters) {
            Parameters = parameters;
        }
    }
}
