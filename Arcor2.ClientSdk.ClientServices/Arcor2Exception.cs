using System;
using System.Collections.Generic;

namespace Arcor2.ClientSdk.ClientServices {
    /// <summary>
    /// Represents an error returned by the ARCOR2 server.
    /// </summary>
    public class Arcor2Exception : Exception {
        /// <summary>
        /// Initializes new instance of the <see cref="Arcor2Exception"/> class.
        /// </summary>
        public Arcor2Exception() { }

        /// <summary>
        /// Initializes new instance of the <see cref="Arcor2Exception"/> class with the specified error message.
        /// </summary>
        /// <param name="message">The error message</param>
        public Arcor2Exception(string message) : base(message) { }

        /// <summary>
        /// Initializes new instance of the <see cref="Arcor2Exception"/> class with the specified error message and a list of error messages returned by the server.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="serverErrorMessages">The error messages returned by the server.</param>
        public Arcor2Exception(string message, List<string> serverErrorMessages)
            : base(FormatMessage(message, serverErrorMessages)) { }

        /// <summary>
        /// Initializes new instance of the <see cref="Arcor2Exception"/> class with the specified error message and a reference
        /// to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception.</param>
        public Arcor2Exception(string message, Exception innerException) : base(message, innerException) { }

        private static string FormatMessage(string message, List<string> serverErrorMessages) {
            if(serverErrorMessages == null! || serverErrorMessages.Count == 0) {
                return message + " No server errors were provided.";
            }

            return message + " Got the following server errors: " + string.Join(", ", serverErrorMessages);
        }
    }
}
