using System;

namespace Arcor2.ClientSdk.ClientServices.EventArguments
{
    /// <summary>
    /// Event args for package exceptions.
    /// </summary>
    public class PackageExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// The type of the exception.
        /// </summary>
        public string Type { get; }
        /// <summary>
        /// The message of the exception.
        /// </summary>
        public string Message { get; }
        /// <summary>
        /// Was the exception handled?
        /// </summary>
        public bool Handled { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="PackageExceptionEventArgs"/> class.
        /// </summary>
        public PackageExceptionEventArgs(string type, string message, bool handled)
        {
            Type = type;
            Message = message;
            Handled = handled;
        }
    }
}
