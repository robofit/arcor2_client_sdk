namespace Arcor2.ClientSdk.Communication {
    /// <summary>
    /// An interface for <see cref="Arcor2Client"/> loggers.
    /// </summary>
    /// <remarks>
    /// There is currently no public implementation.
    /// You should implement your own or create an adapter for logger of your choice.
    /// </remarks>
    public interface IArcor2Logger {
        /// <summary>
        /// Used for logging informational and debug messages.
        /// </summary>
        /// <param name="message">The log message.</param>
        void LogInfo(string message);
        /// <summary>
        /// Used for logging ARCOR2 protocol violations, connection errors, and other errors resulting in abortion of an operation.
        /// </summary>
        /// <param name="message">The message.</param>
        void LogError(string message);
        /// <summary>
        /// Used for logging errors that can be recovered and result in a successful operation.
        /// </summary>
        /// <param name="message">The message.</param>
        void LogWarning(string message);
    }
}