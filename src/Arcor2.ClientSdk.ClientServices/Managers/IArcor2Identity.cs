using Arcor2.ClientSdk.ClientServices.EventArguments;
using System;
using System.Threading.Tasks;

namespace Arcor2.ClientSdk.ClientServices.Managers {

    /// <summary>
    ///     Defines required functionality for ARCOR2 object with unique identity.
    /// </summary>
    public interface IArcor2Identity {
        /// <summary>
        ///     Unique identifier for the object.
        /// </summary>
        string Id { get; }

        /// <summary>
        ///     Is the object write-locked?
        /// </summary>
        /// <seealso cref="LockOwner" />
        bool IsLocked { get; }

        /// <summary>
        ///     The owner of a write-lock on this object.
        /// </summary>
        /// <value>
        ///     The owner username, <c>null</c> if unlocked.
        /// </value>
        string? LockOwner { get; }

        /// <summary>
        ///     Pauses automatic locking on all RPCs offered by this object.
        ///     All locks must be acquired manually by using the provided locking method.
        /// </summary>
        bool PauseAutoLock { get; set; }

        /// <summary>
        ///     Raised when this object gets locked.
        /// </summary>
        event EventHandler<LockEventArgs>? Locked;

        /// <summary>
        ///     Raised when this object gets unlocked.
        /// </summary>
        event EventHandler<LockEventArgs>? Unlocked;

        /// <summary>
        ///     Locks the resource represented by this instance.
        /// </summary>
        /// <remarks>
        ///     Before using locking methods, make sure AutoLock mode is disabled or paused. See <see cref="Arcor2SessionSettings" /> or
        ///     <see cref="InvalidOperationException" />.
        /// </remarks>
        /// <exception cref="LockableArcor2ObjectManager{TData}.PauseAutoLock"></exception>
        /// <exception cref="LockableArcor2ObjectManager{TData}">When invoked with AutoLock mode enabled.</exception>
        Task LockAsync(bool lockTree = false);

        /// <summary>
        ///     Unlocks the resource represented by this instance.
        /// </summary>
        /// <remarks>
        ///     Before using locking methods, make sure AutoLock mode is disabled or paused. See <see cref="Arcor2SessionSettings" /> or
        ///     <see cref="InvalidOperationException" />.
        /// </remarks>
        /// <exception cref="LockableArcor2ObjectManager{TData}.PauseAutoLock"></exception>
        /// <exception cref="LockableArcor2ObjectManager{TData}">When invoked with AutoLock mode enabled.</exception>
        Task UnlockAsync();

        /// <summary>
        ///     Unlocks the resource represented by this instance. Doesn't throw on failure.
        /// </summary>
        /// <remarks>
        ///     Before using locking methods, make sure AutoLock mode is disabled or paused. See <see cref="Arcor2SessionSettings" /> or
        ///     <see cref="LockableArcor2ObjectManager{TData}.PauseAutoLock" />.
        /// </remarks>
        /// <exception cref="LockableArcor2ObjectManager{TData}">When invoked with AutoLock mode enabled.</exception>
        Task TryUnlockAsync();
    }
}