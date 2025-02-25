using System;
using System.Threading.Tasks;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// Base class for manager classes. Manager classes usually take care of a lifecycle of some object (such as Scene, ObjectType, etc...).
    /// The corresponding <see cref="Arcor2Session"/> instance should be always injected, providing access for communication with the server.
    /// </summary>
    public abstract class Arcor2ObjectManager : IDisposable {
        /// <summary>
        /// The session used for communication with the server.
        /// </summary>
        protected Arcor2Session Session;

        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockableArcor2ObjectManager"/> class.
        /// </summary>
        /// <param name="session">The session used for communication with the server. Should generally inject only itself.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="session"/> is null.</exception>
        protected Arcor2ObjectManager(Arcor2Session session) {
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
        /// <param name="disposing">True if called from <see cref="Dispose"/>; false if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    UnregisterHandlers();
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Locks a resource.
        /// </summary>
        /// <param name="id">The ID of the resource.</param>
        /// <exception cref="Arcor2Exception"></exception>
        protected internal async Task LockAsync(string id) {
            var @lock = await Session.client.WriteLockAsync(new WriteLockRequestArgs(id));
            if(!@lock.Result) {
                throw new Arcor2Exception($"Locking object {id} failed.", @lock.Messages);
            }
        }

        /// <summary>
        /// Unlocks a resource.
        /// </summary>
        /// <param name="id">The ID of the resource.</param>
        /// <exception cref="Arcor2Exception"></exception>
        protected internal async Task UnlockAsync(string id) {
            var @lock = await Session.client.WriteUnlockAsync(new WriteUnlockRequestArgs(id));
            if(!@lock.Result) {
                throw new Arcor2Exception($"Unlocking object {id} failed.", @lock.Messages);
            }
        }

        /// <summary>
        /// Registers event handlers from session/client. Derived classes should override this method to register their specific handlers and invoke the base method.
        /// </summary>
        protected virtual void RegisterHandlers() { }

        /// <summary>
        /// Unregisters event handlers from session/client. Derived classes should override this method to unregister their specific handlers and invoke the base method.
        /// </summary>
        protected virtual void UnregisterHandlers() { }
    }
}