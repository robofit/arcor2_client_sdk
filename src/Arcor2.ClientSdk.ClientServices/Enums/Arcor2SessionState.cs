namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents a state of ARCOR2 session.
    /// </summary>
    public enum Arcor2SessionState {
        /// <summary>
        /// The default initial state.
        /// </summary>
        None,
        /// <summary>
        /// A connection with the server has been established.
        /// </summary>
        Open,
        /// <summary>
        /// The required set of ARCOR2 objects has been loaded.
        /// Null annotations are now valid and the library is usable in generally read-only manner.
        /// </summary>
        Initialized,
        /// <summary>
        /// The user has been successfully registered.
        /// The library is now fully usable.
        /// </summary>
        Registered,
        /// <summary>
        /// The connection has been closed. All method invocations are now illegal on the session's objects.
        /// </summary>
        Closed
    }
}
