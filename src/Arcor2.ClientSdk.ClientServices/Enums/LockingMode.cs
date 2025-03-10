namespace Arcor2.ClientSdk.ClientServices.Enums
{
    /// <summary>
    /// Determines the locking behavior of the library.
    /// </summary>
    public enum LockingMode
    {
        /// <summary>
        /// The library does not lock any objects and all locks must be acquired nd released by the user.
        /// </summary>
        NoLocks,
        /// <summary>
        /// The library automatically acquires and releases locks for most objects (notable exception being object aiming process RPCs). 
        /// </summary>
        AutoLock
    }
}