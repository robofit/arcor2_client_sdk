using System;
using System.Collections.Generic;

namespace Arcor2.ClientSdk.ClientServices.EventArguments
{
    /// <summary>
    /// Event args for package action state before execution report.
    /// </summary>
    public class ActionFinishedEventArgs : EventArgs
    {
        /// <summary>
        /// The action parameters.
        /// </summary>
        public IList<string> Results { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ActionFinishedEventArgs"/> class.
        /// </summary>
        public ActionFinishedEventArgs(IList<string> results)
        {
            Results = results;
        }
    }
}