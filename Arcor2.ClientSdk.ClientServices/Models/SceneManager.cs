using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    public class SceneManager : Arcor2ObjectManager {

        /// <summary>
        /// The metadata of the scene.
        /// </summary>
        public BareScene Meta { get; private set; } 
        public IList<SceneObject>? SceneObjects { get; private set; }

        /// <summary>
        /// Raised when scene is saved by the server.
        /// </summary>
        public EventHandler? OnSaved;

        public SceneManager(Arcor2Session session, BareScene meta) : base(session) {
            Meta = meta;
        }

        public SceneManager(Arcor2Session session, Scene scene) : base(session) {
            Meta = scene.MapToBareScene();
            SceneObjects = scene.Objects;
        }

        /// <summary>
        /// Renames the scene.
        /// </summary>
        /// <param name="newName">New name for the scene.</param>
        /// <exception cref="Arcor2Exception" />
        public async Task RenameAsync(string newName) {
            var @lock = await Session.client.WriteLockAsync(new WriteLockRequestArgs(Meta.Id));
            if(!@lock.Result) {
                throw new Arcor2Exception($"Renaming scene {Meta.Id} failed while locking.", @lock.Messages);
            }

            var response = await Session.client.RenameSceneAsync(new RenameArgs(Meta.Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming scene {Meta.Id} failed.", response.Messages);
            }
            // Auto-Unlock on server-side for some reason.
        }

        /// <summary>
        /// Opens the scene.
        /// </summary>
        /// <remarks>
        /// The session must be in a menu on invocation.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task OpenAsync() {
            var response = await Session.client.OpenSceneAsync(new IdArgs(Meta.Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Opening scene {Meta.Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Closes the scene.
        /// </summary>
        /// <remarks>
        /// Scene must be open on invocation. Will fail with unsaved changes and if unforced.
        /// </remarks>
        /// <param name="force">If true, the scene will be closed even with unsaved changes, etc... </param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CloseAsync(bool force = false) {
            var response = await Session.client.CloseSceneAsync(new CloseSceneRequestArgs(force));
            if(!response.Result) {
                throw new Arcor2Exception($"Closing scene {Meta.Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Saves the scene.
        /// </summary>
        /// <remarks>
        /// Scene must be open on invocation.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SaveAsync() {
            var response = await Session.client.SaveSceneAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Saving scene {Meta.Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Deletes the scene.
        /// </summary>
        /// <remarks>
        /// The scene must be closed and have no associated projects.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.client.RemoveSceneAsync(new IdArgs(Meta.Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing scene {Meta.Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Updates the description of the scene.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateDescriptionAsync(string newDescription) {
            var response = await Session.client.UpdateSceneDescriptionAsync(new UpdateSceneDescriptionRequestArgs(Meta.Id, newDescription));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating description of scene {Meta.Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Starts the scene.
        /// </summary>
        /// <remarks>
        /// The scene must be opened, in the stopped state, and all locks freed.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StartAsync() {
            var response = await Session.client.StartSceneAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Starting scene {Meta.Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Stops the scene
        /// </summary>
        /// <remarks>
        /// The scene must be opened, in the started state, and without running actions.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StopAsync() {
            var response = await Session.client.StopSceneAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Stopping of scene {Meta.Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Loads the scene fully without opening it (including action objects).
        /// </summary>
        public async Task LoadAsync() {
            var response = await Session.client.GetSceneAsync(new IdArgs(Meta.Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Loading scene {Meta.Id} failed.", response.Messages);
            }

            Meta = response.Data.MapToBareScene();
            SceneObjects = response.Data.Objects;
        }

        /// <summary>
        /// Duplicates the scene.
        /// </summary>
        public async Task DuplicateAsync(string newName) {
            var response = await Session.client.DuplicateSceneAsync(new CopySceneRequestArgs(Meta.Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Duplicating scene {Meta.Id} failed.", response.Messages);
            }
        }

        internal void UpdateAccordingToNewObject(Scene scene) {
            Meta = scene.MapToBareScene();
            SceneObjects = scene.Objects;
        }

        protected override void RegisterHandlers() {
            Session.client.OnSceneSaved += OnSaved;
            Session.client.OnSceneRemoved += OnSceneRemoved;
            Session.client.OnSceneBaseUpdated += OnSceneBaseUpdated;
        }

        protected override void UnregisterHandlers() {
            Session.client.OnSceneSaved -= OnSaved;
            Session.client.OnSceneRemoved -= OnSceneRemoved;
            Session.client.OnSceneBaseUpdated -= OnSceneBaseUpdated;
        }

        private void OnSceneBaseUpdated(object sender, BareSceneEventArgs e) {
            if(e.Scene.Id == Meta.Id) {
                Meta = e.Scene;
            }
        }

        private void OnSceneRemoved(object sender, BareSceneEventArgs e) {
            if (e.Scene.Id == Meta.Id) {
                Session.Scenes.Remove(this);
                Dispose();
            }
        }
    }
}