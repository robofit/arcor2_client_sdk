using System;
using System.Collections.Generic;

namespace Arcor2.ClientSdk.ClientServices {
    public class Arcor2Exception : Exception {
        public Arcor2Exception() { }
        public Arcor2Exception(string message) : base(message) { }

        public Arcor2Exception(string message, List<string> serverErrorMessages)
            : base(FormatMessage(message, serverErrorMessages)) { }
        public Arcor2Exception(string message, Exception innerException) : base(message, innerException) { }

        private static string FormatMessage(string message, List<string> serverErrorMessages) {
            if(serverErrorMessages == null! || serverErrorMessages.Count == 0) {
                return message + " No server errors were provided.";
            }

            return message + " Got the following server errors: " + string.Join(", ", serverErrorMessages);
        }
    }
}
