using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Arcor2.ClientSdk.ClientServices.Managers {
    /// <summary>
    ///     Manages lifetime of a scene.
    /// </summary>
    public class SceneManager : LockableArcor2ObjectManager<BareScene> {
        /// <summary>
        ///     Raised when scene state changes.
        /// </summary>
        public EventHandler<SceneOnlineStateEventArgs>? OnlineStateChanged;

        /// <summary>
        ///     Raised when scene is saved by the server.
        /// </summary>
        public EventHandler? Saved;

        /// <summary>
        ///     Initializes a new instance of <see cref="SceneManager" /> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="meta">Scene meta object.</param>
        internal SceneManager(Arcor2Session session, BareScene meta) : base(session, meta, meta.Id) { }

        /// <summary>
        ///     Initializes a new instance of <see cref="SceneManager" /> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="scene">Scene object.</param>
        internal SceneManager(Arcor2Session session, Scene scene) : base(session, scene.MapToBareScene(), scene.Id) {
            actionObjects =
                new ObservableCollection<ActionObjectManager>(scene.Objects.Select(o =>
                    new ActionObjectManager(session, this, o)));
            ActionObjects = new Arcor2IndexableReadOnlyObservableCollection<ActionObjectManager>(actionObjects);
        }

        internal ObservableCollection<ActionObjectManager>? actionObjects { get; }

        /// <summary>
        ///     Collection of existing action objects.
        /// </summary>
        /// <value>
        ///     A list of <see cref="ActionObjectManager" />, <c>null</c> if not loaded.
        /// </value>
        public IndexableReadOnlyObservableCollection<ActionObjectManager>? ActionObjects { get; }

        /// <summary>
        ///     Gets if the scene is open.
        /// </summary>
        /// <returns> <c>true</c> if this scene is open, <c>false</c> otherwise.</returns>
        public bool IsOpen => Session.NavigationState == NavigationState.Scene && Session.NavigationId == Id;

        /// <summary>
        ///     Gets the scene online state (start state).
        /// </summary>
        public SceneOnlineState State { get; private set; } = new SceneOnlineState(OnlineState.Stopped);

        /// <summary>
        ///     Returns a collection of projects based of this scene.
        /// </summary>
        public IList<ProjectManager> GetProjects() => Session.Projects.Where(p => p.Data.SceneId == Id).ToList();

        /// <summary>
        ///     Adds a new action object to the scene with the default parameter values.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionObjectAsync(ObjectTypeManager objectType, string name, Pose pose) {
            var parameters = objectType.Data.Meta.Settings.Select(parameterMeta => parameterMeta.ToParameter())
                .ToList();
            var response =
                await Session.Client.AddActionObjectToSceneAsync(
                    new AddObjectToSceneRequestArgs(name, objectType.Id, pose, parameters));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new action object {objectType.Id} to scene {Id} failed.",
                    response.Messages);
            }
        }

        /// <summary>
        ///     Adds a new action object to the scene with the default parameter values.
        /// </summary>
        /// <param name="type">The object type.</param>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionObjectAsync(string type, string name, Pose pose) {
            var response =
                await Session.Client.AddActionObjectToSceneAsync(new AddObjectToSceneRequestArgs(name, type, pose));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new action object {type} to scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Adds a new action object to the scene with the default parameter values.
        /// </summary>
        /// <param name="type">The object type.</param>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <exception cref="Arcor2Exception"></exception>
        /// <exception cref="InvalidOperationException">When the object type does not exist.</exception>
        public async Task AddActionObjectWithDefaultParametersAsync(string type, string name, Pose pose) {
            var parameters = Session.ObjectTypes.First(o => o.Id == type).Data.Meta.Settings
                .Select(parameterMeta => parameterMeta.ToParameter()).ToList();
            var response =
                await Session.Client.AddActionObjectToSceneAsync(
                    new AddObjectToSceneRequestArgs(name, type, pose, parameters));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new action object {type} to scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Adds a new action object to the scene.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="parameters">A collection of parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionObjectAsync(ObjectTypeManager objectType, string name, Pose pose,
            ICollection<Parameter> parameters) {
            var response =
                await Session.Client.AddActionObjectToSceneAsync(
                    new AddObjectToSceneRequestArgs(name, objectType.Id, pose, parameters.ToList()));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new action object {objectType.Id} to scene {Id} failed.",
                    response.Messages);
            }
        }

        /// <summary>
        ///     Adds a new action object to the scene.
        /// </summary>
        /// <param name="type">The object type.</param>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="parameters">A collection of parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionObjectAsync(string type, string name, Pose pose, ICollection<Parameter> parameters) {
            var response =
                await Session.Client.AddActionObjectToSceneAsync(
                    new AddObjectToSceneRequestArgs(name, type, pose, parameters.ToList()));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new action object {type} to scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Adds a new virtual collision object to the scene.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="box">The box parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddVirtualCollisionBoxAsync(string name, Pose pose, BoxCollisionModel box) {
            // We use custom models for this, due to very confusing ID parameter, which has to match name.
            var response = await Session.Client.AddVirtualCollisionObjectToSceneAsync(
                new AddVirtualCollisionObjectToSceneRequestArgs(name, pose, box.ToObjectModel(name)));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new box virtual collision action object to scene {Id} failed.",
                    response.Messages);
            }
        }

        /// <summary>
        ///     Adds a new virtual collision object to the scene.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="cylinder">The cylinder parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddVirtualCollisionCylinderAsync(string name, Pose pose, CylinderCollisionModel cylinder) {
            var response = await Session.Client.AddVirtualCollisionObjectToSceneAsync(
                new AddVirtualCollisionObjectToSceneRequestArgs(name, pose, cylinder.ToObjectModel(name)));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new cylinder virtual collision action object to scene {Id} failed.",
                    response.Messages);
            }
        }

        /// <summary>
        ///     Adds a new virtual collision object to the scene.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="sphere">The sphere parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddVirtualCollisionSphereAsync(string name, Pose pose, SphereCollisionModel sphere) {
            var response = await Session.Client.AddVirtualCollisionObjectToSceneAsync(
                new AddVirtualCollisionObjectToSceneRequestArgs(name, pose, sphere.ToObjectModel(name)));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new sphere virtual collision action object to scene {Id} failed.",
                    response.Messages);
            }
        }

        /// <summary>
        ///     Adds a new virtual collision object to the scene.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pose">The pose.</param>
        /// <param name="mesh">The mesh parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddVirtualCollisionMeshAsync(string name, Pose pose, MeshCollisionModel mesh) {
            var response = await Session.Client.AddVirtualCollisionObjectToSceneAsync(
                new AddVirtualCollisionObjectToSceneRequestArgs(name, pose, mesh.ToObjectModel(name)));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding new mesh virtual collision action object to scene {Id} failed.",
                    response.Messages);
            }
        }

        /// <summary>
        ///     Renames the scene.
        /// </summary>
        /// <param name="newName">New name for the scene.</param>
        /// <exception cref="Arcor2Exception" />
        public async Task RenameAsync(string newName) {
            await LibraryLockAsync();
            var response = await Session.Client.RenameSceneAsync(new RenameArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming scene {Id} failed.", response.Messages);
            }
            // Unlocked by the server.
        }

        /// <summary>
        ///     Opens the scene.
        /// </summary>
        /// <remarks>
        ///     The session must be in a menu.
        ///     For extra caution, make sure that the scene is actually opened by invoking <see cref="IsOpen" />
        ///     or checking if the <see cref="Arcor2Session.NavigationState" /> is in the <see cref="NavigationState.Scene" />
        ///     state with corresponding scene ID.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task OpenAsync() {
            var response = await Session.Client.OpenSceneAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Opening scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Closes the scene.
        /// </summary>
        /// <remarks>
        ///     Scene must be open on invocation. Will fail with unsaved changes and if unforced.
        /// </remarks>
        /// <param name="force">If true, the scene will be closed even with unsaved changes, etc... </param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CloseAsync(bool force = false) {
            var response = await Session.Client.CloseSceneAsync(new CloseSceneRequestArgs(force));
            if(!response.Result) {
                throw new Arcor2Exception($"Closing scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Saves the scene.
        /// </summary>
        /// <remarks>
        ///     Scene must be open on invocation.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SaveAsync() {
            var response = await Session.Client.SaveSceneAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Saving scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Check if the scene has unsaved changes.
        /// </summary>
        /// <remarks>
        ///     Scene must be open on invocation.
        /// </remarks>
        public async Task<bool> HasUnsavedChangesAsync() {
            var response = await Session.Client.SaveSceneAsync(true);
            return !response.Result;
        }

        /// <summary>
        ///     Deletes the scene.
        /// </summary>
        /// <remarks>
        ///     The scene must be closed and have no associated projects.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.Client.RemoveSceneAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Updates the description of the scene.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateDescriptionAsync(string newDescription) {
            var response =
                await Session.Client.UpdateSceneDescriptionAsync(
                    new UpdateSceneDescriptionRequestArgs(Id, newDescription));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating description of scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Starts the scene.
        /// </summary>
        /// <remarks>
        ///     The scene must be opened, in the stopped state, and all locks freed.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StartAsync() {
            var response = await Session.Client.StartSceneAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Starting scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Stops the scene
        /// </summary>
        /// <remarks>
        ///     The scene must be opened, in the started state, and without running actions.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StopAsync() {
            var response = await Session.Client.StopSceneAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Stopping of scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Fully loads a scene
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task LoadAsync() {
            var response = await Session.Client.GetSceneAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Loading scene {Id} failed.", response.Messages);
            }

            UpdateAccordingToNewObject(response.Data);
        }

        /// <summary>
        ///     Duplicates the scene.
        /// </summary>
        public async Task DuplicateAsync(string newName) {
            var response = await Session.Client.DuplicateSceneAsync(new CopySceneRequestArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Duplicating scene {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Updates the scene according to the <paramref name="scene" /> instance.
        /// </summary>
        /// <param name="scene">Newer version of the scene.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// >
        internal void UpdateAccordingToNewObject(Scene scene) {
            if(Id != scene.Id) {
                throw new InvalidOperationException(
                    $"Can't update a SceneManager ({Id}) using a scene data object ({scene.Id}) with different ID.");
            }

            UpdateData(scene.MapToBareScene());

            ActionObjects.UpdateListOfLockableArcor2Objects<ActionObjectManager, SceneObject, ActionObject>(
                scene.Objects,
                o => o.Id,
                (manager, o) => manager.UpdateAccordingToNewObject(o),
                o => new ActionObjectManager(Session, this, o));
        }

        /// <summary>
        ///     Updates the scene according to the <paramref name="scene" /> instance.
        /// </summary>
        /// <param name="scene">Newer version of the scene.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// >
        internal void UpdateAccordingToNewObject(BareScene scene) {
            if(Id != scene.Id) {
                throw new InvalidOperationException(
                    $"Can't update a SceneManager ({Id}) using a scene data object ({scene.Id}) with different ID.");
            }

            UpdateData(scene);
        }

        /// <summary>
        ///     Gets and registers eligible action objects for joints and eef updates.
        /// </summary>
        /// <returns>
        ///     Collections of action object where Joints or Eef could not be subscribed to or obtained (probably due to user
        ///     holding a prolonged lock).
        /// </returns>
        internal async Task<List<ActionObjectManager>> GetRobotInfoAndUpdatesAsync() {
            var failed = new List<ActionObjectManager>();

            if(ActionObjects != null) {
                foreach(var actionObject in ActionObjects) {
                    var objectType = actionObject.ObjectType;
                    if(objectType.Data.RobotMeta != null) {
                        try {
                            if(objectType.Data.RobotMeta.Features.MoveToPose) {
                                await actionObject.ReloadRobotArmsAndEefPoseAsync();
                                await actionObject.RegisterForUpdatesAsync(RobotUpdateType.Pose);
                            }

                            if(objectType.Data.RobotMeta.Features.MoveToJoints) {
                                await actionObject.ReloadRobotJointsAsync();
                                await actionObject.RegisterForUpdatesAsync(RobotUpdateType.Joints);
                            }
                        }
                        catch(Arcor2Exception) {
                            failed.Add(actionObject);
                        }
                    }
                }
            }

            return failed;
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.Client.SceneSaved += Saved;
            Session.Client.SceneRemoved += OnSceneRemoved;
            Session.Client.SceneBaseUpdated += OnSceneBaseUpdated;
            Session.Client.SceneState += OnSceneState;
            Session.Client.ActionObjectAdded += OnActionObjectAdded;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.Client.SceneSaved -= Saved;
            Session.Client.SceneRemoved -= OnSceneRemoved;
            Session.Client.SceneBaseUpdated -= OnSceneBaseUpdated;
            Session.Client.SceneState -= OnSceneState;
            Session.Client.ActionObjectAdded -= OnActionObjectAdded;
        }

        public new void Dispose() {
            base.Dispose();
            if(ActionObjects != null) {
                foreach(var actionObject in ActionObjects) {
                    actionObject.Dispose();
                }
            }
        }

        private void OnSceneBaseUpdated(object sender, BareSceneEventArgs e) {
            if(e.Data.Id == Id) {
                UpdateData(e.Data);
            }
        }

        private void OnSceneRemoved(object sender, BareSceneEventArgs e) {
            if(e.Data.Id == Id) {
                RemoveData();
                Session.scenes.Remove(this);
                Dispose();
            }
        }

        private async void OnSceneState(object sender, SceneStateEventArgs e) {
            if(IsOpen ||
               GetProjects().Any(project => project.IsOpen ||
                                            project.GetPackages().Any(package => package.IsOpen))) {
                State = e.Data.MapToCustomSceneStateEnum();
                if(State.State == OnlineState.Started && Session.ConnectionState == Arcor2SessionState.Registered) {
                    await GetRobotInfoAndUpdatesAsync();
                }

                OnlineStateChanged?.Invoke(this, new SceneOnlineStateEventArgs(State));
            }
        }

        private void OnActionObjectAdded(object sender, ActionObjectEventArgs e) {
            if(IsOpen) {
                if(ActionObjects == null) {
                    Session.Logger?.LogWarn(
                        $"While adding new action object, the currently opened scene ({Id}) had non-initialized (null) action object collection. Possible inconsistent state.");
                }

                actionObjects?.Add(new ActionObjectManager(Session, this, e.Data));
            }
        }
    }
}