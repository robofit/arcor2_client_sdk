namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    ///     Determines the locking behavior of the library.
    /// </summary>
    public enum LockingMode {
        /// <summary>
        ///     The library does not lock any objects. All locks must be acquired and released by the user.
        /// </summary>
        NoLocks,

        /// <summary>
        ///     The library automatically acquires and releases locks for most objects (notable exception being object aiming
        ///     process RPCs).
        /// </summary>
        AutoLock
    }
}