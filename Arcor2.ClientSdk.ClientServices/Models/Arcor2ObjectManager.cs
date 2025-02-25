using System;
using System.Threading.Tasks;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// Base class for manager classes. Manager classes usually take care of a lifecycle of some object (such as Scene, ObjectType, etc...).
    /// The corresponding <see cref="Arcor2Session"/> instance should be always injected, providing access for communication with the server.
    /// </summary>
    public abstract class Arcor2ObjectManager : IDisposable {

        /// <summary>
        /// Unique identifier for the object.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Is the object write-locked?
        /// </summary>
        /// <seealso cref="LockOwner"/>
        public bool Locked { get; private set; }

        /// <summary>
        /// The owner of a write-lock on this object.
        /// </summary>
        /// <value>
        /// The owner username, <c>null</c> if unlocked.
        /// </value>
        public string? LockOwner { get; private set; }

        /// <summary>
        /// The session used for communication with the server.
        /// </summary>
        protected readonly Arcor2Session Session;

        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Arcor2ObjectManager"/> class.
        /// </summary>
        /// <param name="session">The session used for communication with the server. Should generally inject only itself.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="session"/> is null.</exception>
        protected Arcor2ObjectManager(Arcor2Session session, string id) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Session = session ?? throw new ArgumentNullException(nameof(session));
            // This is fine, we are just registering handlers. The construction order will not change anything
            // ...unless someone does anything more than registering handlers in the override
            // ReSharper disable once VirtualMemberCallInConstructor
            RegisterHandlers();
        }

        ~Arcor2ObjectManager() {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the manager and unregisters any event handlers.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the manager and unregisters any event handlers.
        /// </summary>
        /// <param name="disposing">True if called from <see cref="Dispose()"/>; false if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    UnregisterHandlers();
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Registers event handlers from session/client. Derived classes should override this method to register their specific handlers and invoke the base method.
        /// </summary>
        protected virtual void RegisterHandlers() {
            Session.client.OnObjectsLocked += OnObjectsLocked;
            Session.client.OnObjectsUnlocked += OnObjectsUnlocked;
        }

        /// <summary>
        /// Unregisters event handlers from session/client. Derived classes should override this method to unregister their specific handlers and invoke the base method.
        /// </summary>
        protected virtual void UnregisterHandlers() {
            Session.client.OnObjectsLocked -= OnObjectsLocked;
            Session.client.OnObjectsUnlocked -= OnObjectsUnlocked;
        }

        /// <summary>
        /// Locks the resource represented by this instance.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        protected internal async Task LockAsync() {
            var @lock = await Session.client.WriteLockAsync(new WriteLockRequestArgs(Id));
            if(!@lock.Result) {
                throw new Arcor2Exception($"Locking object {Id} failed.", @lock.Messages);
            }
        }

        /// <summary>
        /// Unlocks the resource represented by this instance.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        protected internal async Task UnlockAsync() {
            var @lock = await Session.client.WriteUnlockAsync(new WriteUnlockRequestArgs(Id));
            if(!@lock.Result) {
                throw new Arcor2Exception($"Unlocking object {Id} failed.", @lock.Messages);
            }
        }

        private void OnObjectsLocked(object sender, ObjectsLockEventArgs e) {
            if (e.Data.ObjectIds.Contains(Id)) {
                if (Locked) {
                    Session.logger?.LogWarning($"The object {Id} received lock event message while already locked.");
                }
                Locked = true;
                LockOwner = e.Data.Owner;
            }
        }

        private void OnObjectsUnlocked(object sender, ObjectsLockEventArgs e) {
            if(e.Data.ObjectIds.Contains(Id)) {
                if(!Locked) {
                    Session.logger?.LogWarning($"The object {Id} received unlock event message while already unlocked.");
                }
                Locked = false;
                LockOwner = null;
            }
        }
    }
}