using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.ClientServices.Models.Extras;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using SceneState = Arcor2.ClientSdk.ClientServices.Models.Extras.SceneState;

namespace Arcor2.ClientSdk.ClientServices.Models
{
    /// <summary>
    /// Manages a lifetime of a scene.
    /// </summary>
    public class SceneManager : Arcor2ObjectManager {

        /// <summary>
        /// The metadata of the scene.
        /// </summary>
        public BareScene Meta { get; private set; }

        /// <summary>
        /// Collection of existing action objects.
        /// </summary>
        /// <value>
        /// A list of <see cref="ActionObjectManager"/>, <c>null</c> if not loaded.
        /// 
        /// </value>
        public IList<ActionObjectManager>? ActionObjects { get; private set; }

        /// <summary>
        /// Gets if the scene is open.
        /// </summary>
        /// <returns> <c>true</c> if the current scene is open, <c>false</c> otherwise.</returns>
        public bool IsOpen => Session.NavigationState == NavigationState.Scene && Session.NavigationId == Id;

        /// <summary>
        /// Gets the scene online state (start state).
        /// </summary>
        public SceneState State { get; private set; } = new SceneState(SceneOnlineState.Stopped);

        /// <summary>
        /// Raised when scene is saved by the server.
        /// </summary>
        public EventHandler? OnSaved;

        /// <summary>
        /// Initializes a new instance of <see cref="SceneManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="meta">Scene meta object.</param>
        public SceneManager(Arcor2Session session, BareScene meta) : base(session, meta.Id) {
            Meta = meta;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SceneManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="scene">Scene object.</param>
        public SceneManager(Arcor2Session session, Scene scene) : base(session, scene.Id) {
            Meta = scene.MapToBareScene();
            ActionObjects = scene.Objects.Select(o => new ActionObjectManager(session, this, o)).ToList();
        }

        /// <summary>
        /// Adds a new action object to the scene.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="parameters">A collection of parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddNewActionObjectAsync(ObjectTypeManager objectType, string name, Pose pose, ICollection<Parameter> parameters) {
            var response = await Session.client.AddActionObjectToSceneAsync(new AddObjectToSceneRequestArgs(name, objectType.Id, pose, parameters.ToList()));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new action object {objectType.Id} to scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new action object to the scene.
        /// </summary>
        /// <param name="type">The object type.</param>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="parameters">A collection of parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddNewActionObjectAsync(string type, string name, Pose pose, ICollection<Parameter> parameters) {
            var response = await Session.client.AddActionObjectToSceneAsync(new AddObjectToSceneRequestArgs(name, type, pose, parameters.ToList()));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new action object {type} to scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new virtual collision object to the scene.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="box">The box parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddVirtualCollisionBoxAsync(string name, Pose pose, BoxCollisionModel box) {
            // We use custom models for this, due to very confusing ID parameter, which has to match name.
            var response = await Session.client.AddVirtualCollisionObjectToSceneAsync(new AddVirtualCollisionObjectToSceneRequestArgs(name, pose, box.ToOpenApiObjectModel(name)));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new box virtual collision action object to scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new virtual collision object to the scene.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="cylinder">The cylinder parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddVirtualCollisionCylinderAsync(string name, Pose pose, CylinderCollisionModel cylinder) {
            var response = await Session.client.AddVirtualCollisionObjectToSceneAsync(new AddVirtualCollisionObjectToSceneRequestArgs(name, pose, cylinder.ToOpenApiObjectModel(name)));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new cylinder virtual collision action object to scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new virtual collision object to the scene.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="sphere">The sphere parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddVirtualCollisionSphereAsync(string name, Pose pose, SphereCollisionModel sphere) { ;
            var response = await Session.client.AddVirtualCollisionObjectToSceneAsync(new AddVirtualCollisionObjectToSceneRequestArgs(name, pose, sphere.ToOpenApiObjectModel(name)));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new sphere virtual collision action object to scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new virtual collision object to the scene.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="mesh">The mesh parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddVirtualCollisionMeshAsync(string name, Pose pose, MeshCollisionModel mesh) {
            var response = await Session.client.AddVirtualCollisionObjectToSceneAsync(new AddVirtualCollisionObjectToSceneRequestArgs(name, pose, mesh.ToOpenApiObjectModel(name)));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new mesh virtual collision action object to scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Renames the scene.
        /// </summary>
        /// <param name="newName">New name for the scene.</param>
        /// <exception cref="Arcor2Exception" />
        public async Task RenameAsync(string newName) {
            await LockAsync();
            var response = await Session.client.RenameSceneAsync(new RenameArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming scene {Id} failed.", response.Messages);
            }
            // Unlocked by the server.
        }

        /// <summary>
        /// Opens the scene.
        /// </summary>
        /// <remarks>
        /// The session must be in a menu.
        ///
        /// For extra caution, make sure that the scene is actually opened by invoking <see cref="IsOpen"/>
        /// or checking if the <see cref="Arcor2Session.NavigationState"/> is in the <see cref="NavigationState.Scene"/> state with corresponding scene ID.
        /// 
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task OpenAsync() {
            var response = await Session.client.OpenSceneAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Opening scene {Id} failed.", response.Messages);
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
                throw new Arcor2Exception($"Closing scene {Id} failed.", response.Messages);
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
                throw new Arcor2Exception($"Saving scene {Id} failed.", response.Messages);
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
            var response = await Session.client.RemoveSceneAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Updates the description of the scene.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateDescriptionAsync(string newDescription) {
            var response = await Session.client.UpdateSceneDescriptionAsync(new UpdateSceneDescriptionRequestArgs(Id, newDescription));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating description of scene {Id} failed.", response.Messages);
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
                throw new Arcor2Exception($"Starting scene {Id} failed.", response.Messages);
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
                throw new Arcor2Exception($"Stopping of scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Loads the scene fully without opening it (including action objects).
        /// </summary>
        public async Task LoadAsync() {
            var response = await Session.client.GetSceneAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Loading scene {Id} failed.", response.Messages);
            }

            Meta = response.Data.MapToBareScene();
            ActionObjects = response.Data.Objects.Select(o => new ActionObjectManager(Session, this, o)).ToList();
        }

        /// <summary>
        /// Duplicates the scene.
        /// </summary>
        public async Task DuplicateAsync(string newName) {
            var response = await Session.client.DuplicateSceneAsync(new CopySceneRequestArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Duplicating scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Updates the scene according to the <paramref name="scene"/> instance.
        /// </summary>
        /// <param name="scene">Newer version of the scene.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(Scene scene) {
            if (Id != scene.Id) {
                throw new InvalidOperationException($"Can't update a SceneManager ({Id}) using a scene data object ({scene.Id}) with different ID.");
            }

            Meta = scene.MapToBareScene();
            if (ActionObjects != null) {
                foreach(var actionObject in ActionObjects) {
                    actionObject.Dispose();
                }
            }
            ActionObjects = scene.Objects.Select(o => new ActionObjectManager(Session, this, o)).ToList();
        }

        /// <summary>
        /// Registers eligible action objects for joints and pose updates.
        /// </summary>
        internal async Task RegisterForEndEffectorUpdatesAsync() {
            if (ActionObjects != null) {
                foreach(var actionObject in ActionObjects) {
                    var objectType = actionObject.GetObjectType()!;
                    if (objectType.IsRobot() && objectType.Meta is { Abstract: false, Disabled: false }) {
                        if(objectType.Meta.HasPose) {
                            // TODO: Is this needed?
                            await actionObject.RegisterForUpdatesAsync(RobotUpdateType.Pose);
                        }
                        await actionObject.RegisterForUpdatesAsync(RobotUpdateType.Joints);
                    }
                }
            }
        }

        protected override void RegisterHandlers() {
            Session.client.OnSceneSaved += OnSaved;
            Session.client.OnSceneRemoved += OnSceneRemoved;
            Session.client.OnSceneBaseUpdated += OnSceneBaseUpdated;
            Session.client.OnSceneState += OnSceneState;
            Session.client.OnSceneActionObjectAdded += OnSceneActionObjectAdded;
        }

        private void OnSceneActionObjectAdded(object sender, SceneActionObjectEventArgs e) {
            if (IsOpen) {
                if (ActionObjects == null) {
                    Session.logger?.LogError($"While adding new action object, the currently opened scene ({Id}) had non-initialized (null) action object collection. Possible inconsistent state.");
                }
                ActionObjects?.Add(new ActionObjectManager(Session, this, e.SceneObject));
            }
        }

        protected override void UnregisterHandlers() {
            Session.client.OnSceneSaved -= OnSaved;
            Session.client.OnSceneRemoved -= OnSceneRemoved;
            Session.client.OnSceneBaseUpdated -= OnSceneBaseUpdated;
            Session.client.OnSceneState -= OnSceneState;
            Session.client.OnSceneActionObjectAdded -= OnSceneActionObjectAdded;
        }

        private void OnSceneBaseUpdated(object sender, BareSceneEventArgs e) {
            if(e.Scene.Id == Id) {
                Meta = e.Scene;
            }
        }

        private void OnSceneRemoved(object sender, BareSceneEventArgs e) {
            if(e.Scene.Id == Id) {
                Session.Scenes.Remove(this);
                Dispose();
            }
        }

        private async void OnSceneState(object sender, SceneStateEventArgs e) {
            State = e.Data.ToCustomSceneState();
            if (State.OnlineState == SceneOnlineState.Started && Session.ConnectionState == Arcor2SessionState.Initialized) {
                await RegisterForEndEffectorUpdatesAsync();
            }
        }
    }
}