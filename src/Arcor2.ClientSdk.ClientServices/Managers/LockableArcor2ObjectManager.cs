using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System;
using System.Threading.Tasks;

namespace Arcor2.ClientSdk.ClientServices.Managers {
    /// <summary>
    ///     Base class for manager classes with identity. Provides locking features.
    /// </summary>
    /// <remarks>
    ///     If the object does not have distinct identity and does not need locking, use
    ///     <see cref="Arcor2ObjectManager{TData}" />.
    /// </remarks>
    /// <typeparam name="TData">The data type managed by this instance. Notifications will be raised on its change.</typeparam>
    public abstract class LockableArcor2ObjectManager<TData> : Arcor2ObjectManager<TData> {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LockableArcor2ObjectManager{TData}" /> class.
        /// </summary>
        /// <param name="session">The session used for communication with the server. Should generally inject only itself.</param>
        /// <param name="data">The data object.</param>
        /// <param name="id">Unique identifier for this object.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="session" /> is null.</exception>
        protected LockableArcor2ObjectManager(Arcor2Session session, TData data, string id) : base(session, data) {
            Id = Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        /// <summary>
        ///     Unique identifier for the object.
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Is the object write-locked?
        /// </summary>
        /// <seealso cref="LockOwner" />
        public bool IsLocked { get; private set; }

        /// <summary>
        ///     The owner of a write-lock on this object.
        /// </summary>
        /// <value>
        ///     The owner username, <c>null</c> if unlocked.
        /// </value>
        public string? LockOwner { get; private set; }

        /// <summary>
        ///     Pauses automatic locking on all RPCs offered by this object.
        ///     All locks must be acquired manually by using the provided locking method.
        /// </summary>
        public bool PauseAutoLock { get; set; } = false;

        /// <summary>
        ///     Raised when this object gets locked.
        /// </summary>
        public event EventHandler<LockEventArgs>? Locked;

        /// <summary>
        ///     Raised when this object gets unlocked.
        /// </summary>
        public event EventHandler<LockEventArgs>? Unlocked;

        /// <summary>
        ///     Locks the resource represented by this instance.
        /// </summary>
        /// <remarks>
        ///     Before using locking methods, make sure AutoLock mode is disabled or paused. See <see cref="PauseAutoLock" /> or
        ///     <see cref="Arcor2SessionSettings" />.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        /// <exception cref="InvalidOperationException">When invoked with AutoLock mode enabled.</exception>
        public async Task LockAsync(bool lockTree = false) {
            if(Session.Settings.LockingMode == LockingMode.AutoLock && !PauseAutoLock) {
                throw new InvalidOperationException(
                    $"Can't use locking methods on object {Id} without disabling or pausing AutoLock mode.");
            }

            var @lock = await Session.Client.WriteLockAsync(new WriteLockRequestArgs(Id, lockTree));
            if(!@lock.Result) {
                throw new Arcor2Exception($"Locking object {Id} failed.", @lock.Messages);
            }
        }

        /// <summary>
        ///     Locks the resource represented by this instance if auto-lock mode is enabled.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        internal async Task LibraryLockAsync(bool lockTree = false) {
            if(Session.Settings.LockingMode == LockingMode.AutoLock && !PauseAutoLock) {
                var @lock = await Session.Client.WriteLockAsync(new WriteLockRequestArgs(Id, lockTree));
                if(!@lock.Result) {
                    throw new Arcor2Exception($"Locking object {Id} failed.", @lock.Messages);
                }
            }
        }

        /// <summary>
        ///     Unlocks the resource represented by this instance.
        /// </summary>
        /// <remarks>
        ///     Before using locking methods, make sure AutoLock mode is disabled or paused. See <see cref="PauseAutoLock" /> or
        ///     <see cref="Arcor2SessionSettings" />.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        /// <exception cref="InvalidOperationException">When invoked with AutoLock mode enabled.</exception>
        internal async Task UnlockAsync() {
            if(Session.Settings.LockingMode == LockingMode.AutoLock && !PauseAutoLock) {
                throw new InvalidOperationException(
                    $"Can't use locking methods on object {Id} without disabling or pausing AutoLock mode.");
            }

            var @lock = await Session.Client.WriteUnlockAsync(new WriteUnlockRequestArgs(Id));
            if(!@lock.Result) {
                throw new Arcor2Exception($"Unlocking object {Id} failed.", @lock.Messages);
            }
        }

        /// <summary>
        ///     Unlocks the resource represented by this instance if auto-lock mode is enabled.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        internal async Task LibraryUnlockAsync() {
            if(Session.Settings.LockingMode == LockingMode.AutoLock && !PauseAutoLock) {
                var @lock = await Session.Client.WriteUnlockAsync(new WriteUnlockRequestArgs(Id));
                if(!@lock.Result) {
                    throw new Arcor2Exception($"Unlocking object {Id} failed.", @lock.Messages);
                }
            }
        }

        /// <summary>
        ///     Unlocks the resource represented by this instance. Doesn't throw on failure.
        /// </summary>
        /// <remarks>
        ///     Before using locking methods, make sure AutoLock mode is disabled or paused. See <see cref="PauseAutoLock" /> or
        ///     <see cref="Arcor2SessionSettings" />.
        /// </remarks>
        /// <exception cref="InvalidOperationException">When invoked with AutoLock mode enabled.</exception>
        internal async Task TryUnlockAsync() {
            if(Session.Settings.LockingMode == LockingMode.AutoLock && !PauseAutoLock) {
                throw new InvalidOperationException(
                    $"Can't use locking methods on object {Id} without disabling or pausing AutoLock mode.");
            }

            await Session.Client.WriteUnlockAsync(new WriteUnlockRequestArgs(Id));
        }

        /// <summary>
        ///     Unlocks the resource represented by this instance if auto-lock mode is enabled. Doesn't throw on failure.
        /// </summary>
        internal async Task TryLibraryUnlockAsync() {
            if(Session.Settings.LockingMode == LockingMode.AutoLock && !PauseAutoLock) {
                await Session.Client.WriteUnlockAsync(new WriteUnlockRequestArgs(Id));
            }
        }

        /// <summary>
        ///     Registers event handlers from session/client. Derived classes should override this method to register their
        ///     specific handlers and invoke the base method.
        /// </summary>
        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.Client.ObjectsLocked += OnObjectsLocked;
            Session.Client.ObjectsUnlocked += OnObjectsUnlocked;
        }

        /// <summary>
        ///     Unregisters event handlers from session/client. Derived classes should override this method to unregister their
        ///     specific handlers and invoke the base method.
        /// </summary>
        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.Client.ObjectsLocked -= OnObjectsLocked;
            Session.Client.ObjectsUnlocked -= OnObjectsUnlocked;
        }

        private void OnObjectsLocked(object sender, ObjectsLockEventArgs e) {
            if(e.Data.ObjectIds.Contains(Id)) {
                if(IsLocked) {
                    Session.Logger?.LogWarn($"The object {Id} received lock event message while already locked.");
                }

                IsLocked = true;
                LockOwner = e.Data.Owner;
                Locked?.Invoke(this, new LockEventArgs(e.Data.Owner));
            }
        }

        private void OnObjectsUnlocked(object sender, ObjectsLockEventArgs e) {
            if(e.Data.ObjectIds.Contains(Id)) {
                if(!IsLocked) {
                    Session.Logger?.LogWarn($"The object {Id} received unlock event message while already unlocked.");
                }

                IsLocked = false;
                LockOwner = null;
                Unlocked?.Invoke(this, new LockEventArgs(e.Data.Owner));
            }
        }
    }
}