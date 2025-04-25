using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System;
using System.Threading.Tasks;

namespace Arcor2.ClientSdk.ClientServices.Managers {
    /// <summary>
    ///     Base class for manager classes. Manager classes usually take care of a lifecycle of some object (such as Scene,
    ///     ObjectType, etc...).
    ///     The corresponding <see cref="Arcor2Session" /> instance should be always injected, providing access for
    ///     communication with the server.
    /// </summary>
    /// <typeparam name="TData">The data type managed by this instance. Notifications will be raised on its change.</typeparam>
    public abstract class Arcor2ObjectManager<TData> : IArcor2ObjectManager<TData> {
        private bool disposed;

        /// <summary>
        ///     The session used for communication with the server.
        /// </summary>
        protected Arcor2Session Session;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LockableArcor2ObjectManager{TData}" /> class.
        /// </summary>
        /// <param name="session">The session used for communication with the server. Should generally inject only itself.</param>
        /// <param name="data">The data object.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="session" /> is null.</exception>
        protected Arcor2ObjectManager(Arcor2Session session, TData data) {
            Data = data;
            Session = session ?? throw new ArgumentNullException(nameof(session));
            // This is fine, we are just registering handlers. The construction order will not change anything
            // ...unless someone does anything more than registering handlers in the override
            // ReSharper disable once VirtualMemberCallInConstructor
            RegisterHandlers();
        }

        /// <summary>
        ///     The data managed by this instance.
        /// </summary>
        public TData Data { get; protected set; }

        /// <summary>
        ///     Disposes the manager and unregisters any event handlers.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Event raised when the data managed by this instance is updated.
        /// </summary>
        public event EventHandler? Updated;

        /// <summary>
        ///     Event raised when before the instance id deleted.
        /// </summary>
        public event EventHandler? Removing;

        ~Arcor2ObjectManager() {
            Dispose(false);
        }

        /// <summary>
        ///     Disposes the manager and unregisters any event handlers.
        /// </summary>
        /// <param name="disposing">True if called from <see cref="Dispose" />; false if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    UnregisterHandlers();
                }

                disposed = true;
            }
        }

        /// <summary>
        ///     Locks a resource if auto-lock mode is enabled.
        /// </summary>
        /// <param name="id">The ID of the resource.</param>
        /// <exception cref="Arcor2Exception"></exception>
        protected internal async Task LibraryLockAsync(string id) {
            if(Session.Settings.LockingMode == LockingMode.AutoLock) {
                var @lock = await Session.Client.WriteLockAsync(new WriteLockRequestArgs(id));
                if(!@lock.Result) {
                    throw new Arcor2Exception($"Locking object {id} failed.", @lock.Messages);
                }
            }
        }

        /// <summary>
        ///     Unlocks a resource if auto-lock mode is enabled.
        /// </summary>
        /// <param name="id">The ID of the resource.</param>
        /// <exception cref="Arcor2Exception"></exception>
        protected internal async Task LibraryUnlockAsync(string id) {
            if(Session.Settings.LockingMode == LockingMode.AutoLock) {
                var @lock = await Session.Client.WriteUnlockAsync(new WriteUnlockRequestArgs(id));
                if(!@lock.Result) {
                    throw new Arcor2Exception($"Unlocking object {id} failed.", @lock.Messages);
                }
            }
        }

        /// <summary>
        ///     Unlocks a resource, but doesn't throw on failure.
        /// </summary>
        /// <param name="id">The ID of the resource.</param>
        protected internal async Task TryUnlockAsync(string id) =>
            await Session.Client.WriteUnlockAsync(new WriteUnlockRequestArgs(id));

        /// <summary>
        ///     Raises notifications before removal.
        /// </summary>
        protected virtual void RemoveData() => OnRemove();

        /// <summary>
        ///     Updates the data according to the new instance and raises notification.
        /// </summary>
        /// <param name="data">The new data.</param>
        protected virtual void UpdateData(TData data) {
            Data = data;
            OnUpdated();
        }

        /// <summary>
        ///     Raises the DataUpdated event.
        /// </summary>
        protected virtual void OnUpdated() => Updated?.Invoke(this, EventArgs.Empty);

        /// <summary>
        ///     Raises the DataUpdated event.
        /// </summary>
        protected virtual void OnRemove() => Removing?.Invoke(this, EventArgs.Empty);

        /// <summary>
        ///     Registers event handlers from session/client. Derived classes should override this method to register their
        ///     specific handlers and invoke the base method.
        /// </summary>
        protected virtual void RegisterHandlers() { }

        /// <summary>
        ///     Unregisters event handlers from session/client. Derived classes should override this method to unregister their
        ///     specific handlers and invoke the base method.
        /// </summary>
        protected virtual void UnregisterHandlers() { }
    }
}