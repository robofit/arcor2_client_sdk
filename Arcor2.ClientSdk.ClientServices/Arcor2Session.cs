using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.Design;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

// Some information about architecture of this package:
//
// 1. The class, Arcor2Session, serves as the main object of the session and generally provides method similar to things you would find in an actual menu including connection management.
// 2. It also holds a collection of different Arcor2ObjectManagers, which represent another functional entities (Scene, ActionPoint, etc...), which then manage themselves through their lifetime.
//  2.1 The Arcor2Session class should only care about events and RPCs not delegable to other, more specific, Manager.
//      For example, server creating new instances of Scene or ObjectType is not delegable to any other Manager.
//      But an existing Manager is capable of updating its data, updating its own "sub-Managers" (e.g. SceneManager adding ActionObjectManager to its collection),
//      or even removing itself from the parent collection, and disposing itself. Imagine this whole structure as a tree, where parents add nodes, and the nodes modify and delete themselves.
//      This is not a dogma though, for example, if we can't statically deduct if event is addition or update to our internal DB
//      (this is the case for OpenScene and such [Handled here], as it can be sent before or after Scenes are loaded). Similarly, SceneBaseUpdate may be both rename (update) or for some reason
//      the result of duplication (addition) [Handled in both SceneManager and here].
//  2.2 The Arcor2Session instance is injected into ALL Managers. It provides them with the ability to use the client, logger, or verify some global state.
//  2.3 Always make sure Manager instances are properly disposed, they will otherwise at best leak a lot of memory (exponentially). At worst, they will cause unwanted behavior.
//  2.4 Managers (see abstract type Arcor2ObjectManager) all have a distinct ID, corresponding to the managed ARCOR2 resource, injected during construction. This ID is immutable (= don't reuse instances).

namespace Arcor2.ClientSdk.ClientServices {

    /// <summary>
    /// Class used for session management and communication with ARCOR2 server.
    /// This class mostly offers actions you could do from the main screen.
    /// </summary>
    public class Arcor2Session {
        internal readonly Arcor2Client client;
        internal readonly IArcor2Logger? logger;

        /// <summary>
        /// The registered username. 
        /// </summary>
        public string? Username { get; }

        /// <summary>
        /// Represents a client view that is expected by the server.
        /// Dynamically updated.
        ///
        /// The <see cref="NavigationId"/> contains the ID of the highlighted item, or another related object such as a scene.
        /// </summary>
        /// <remarks>
        /// The ARCOR2 server is written with a specific UI design in mind.
        /// Some states are purely informational and do not disallow any operations (e.g. there is no functional difference in all the menu states).
        /// </remarks>
        public NavigationState NavigationState { get; set; } = NavigationState.None;


        /// <summary>
        /// The ID of a highlighted object or object needed for client view that is expected by the server
        /// (e.g., a scene or project ID).
        /// </summary>
        /// <value>
        /// The ID of an object, <c>null</c> if not applicable.
        /// </value>
        public string? NavigationId = null;

        /// <summary>
        /// Collection of available object types.
        /// </summary>
        /// <remarks>Loaded on initialization and automatically maintained.</remarks>
        public IList<ObjectTypeManager> ObjectTypes { get; } = new List<ObjectTypeManager>();

        /// <summary>
        /// Collection of available scenes.
        /// </summary>
        /// <remarks>
        /// If the server opens a scene, that scene will get fully initialized and added automatically (regardless of the current state).
        /// Users can also call <see cref="ReloadScenesAsync"/> to load (and update) metadata of all scenes.
        /// Furthermore, users may also invoke <see cref="SceneManager.LoadAsync"/> on the specific instance to load the action objects without opening it.
        /// </remarks>
        public IList<SceneManager> Scenes { get; } = new List<SceneManager>();

        /// <summary>
        /// Collection of available projects.
        /// </summary>
        /// <remarks>
        /// If the server opens a project, that project (and corresponding scene) will get fully initialized and added automatically (regardless of the current state).
        /// Users can also call <see cref="ReloadProjectsAsync"/> to load (and update) metadata of all projects.
        /// </remarks>
        public IList<ProjectManager> Projects { get; } = new List<ProjectManager>();

        #region Connection-related Members

        /// <summary>
        /// Initializes a new instance of <see cref="Arcor2Session"/> class.
        /// </summary>
        /// <param name="logger">A logger instance.</param>
        public Arcor2Session(IArcor2Logger? logger = null) {
            this.logger = logger;
            client = new Arcor2Client(new Arcor2ClientSettings(), this.logger);

            client.OnConnectionOpened += (sender, args) => {
                ConnectionState = Arcor2SessionState.Open;
                OnConnectionOpened?.Invoke(this, EventArgs.Empty);
            };
            client.OnConnectionClosed += (sender, args) => {
                ConnectionState = Arcor2SessionState.Closed;
                OnConnectionClosed?.Invoke(this, EventArgs.Empty);
            };
            client.OnConnectionError += OnConnectionError;
            RegisterHandlers();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Arcor2Session"/> class.
        /// </summary>
        /// <param name="websocket">A WebSocket object implementing the <see cref="IWebSocket"/> interface.</param>
        /// <param name="logger">A logger instance.</param>
        /// <exception cref="InvalidOperationException">If the provided WebSocket instance is not in the <see cref="WebSocketState.None"/> state.</exception>
        public Arcor2Session(IWebSocket websocket, IArcor2Logger? logger = null) {
            if(websocket.State != WebSocketState.None) {
                throw new InvalidOperationException("The socket instance must be in the 'None' state.");
            }

            this.logger = logger;
            client = new Arcor2Client(websocket, new Arcor2ClientSettings(), this.logger);

            client.OnConnectionOpened += (sender, args) => {
                ConnectionState = Arcor2SessionState.Open;
                OnConnectionOpened?.Invoke(this, EventArgs.Empty);
            };
            client.OnConnectionClosed += (sender, args) => {
                ConnectionState = Arcor2SessionState.Closed;
                OnConnectionClosed?.Invoke(this, EventArgs.Empty);
            };
            client.OnConnectionError += OnConnectionError;
        }

        /// <summary>
        /// Retrieves the underlying <see cref="Arcor2Client"/> instance.
        /// </summary>
        /// <returns></returns>
        public Arcor2Client GetUnderlyingArcor2Client() => client;

        /// <summary>
        /// The state of the session.
        /// </summary>
        public Arcor2SessionState ConnectionState { get; private set; } = Arcor2SessionState.None;

        /// <summary>
        /// Raised when any connection-related error occurs.
        /// </summary>
        public event EventHandler<Exception>? OnConnectionError;
        /// <summary>
        /// Raised when connection is closed.
        /// </summary>
        public event EventHandler? OnConnectionClosed;
        /// <summary>
        /// Raised when connection is successfully opened.
        /// </summary>
        public event EventHandler? OnConnectionOpened;

        /// <summary>
        /// Establishes a connection to ARCOR2 server.
        /// </summary>
        /// <param name="domain">Domain of the ARCOR2 server</param>
        /// <param name="port">Port od the ARCOR2 server</param>
        /// <exception cref="UriFormatException" />
        /// <exception cref="InvalidOperationException" />
        public async Task ConnectAsync(string domain, ushort port) {
            await client.ConnectAsync(domain, port);
        }

        /// <summary>
        /// Establishes a connection to ARCOR2 server.
        /// </summary>
        /// <param name="uri">Full WebSocket URI</param>
        /// <exception cref="UriFormatException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="Communication.Arcor2ConnectionException"> When inner WebSocket fails to connect.</exception>
        public async Task ConnectAsync(Uri uri) {
            await client.ConnectAsync(uri);
        }

        /// <summary>
        /// Closes a connection to ARCOR2 sever.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task CloseAsync() {
            await client.CloseAsync();
        }

        #endregion

        /// <summary>
        /// Initializes a session. Internally loads all object types, actions, scenes, project, etc..., and returns the server information.
        /// </summary>
        /// <param name="skipLoadingData">If true, data objects such as scenes, projects, or object types won't be loaded and must be later loaded manually.</param>
        /// <returns>The server information.</returns>
        /// <exception cref="Arcor2Exception" />
        /// <exception cref="Arcor2ConnectionException" />
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<SystemInfoResponseData> InitializeAsync(bool skipLoadingData = false) {
            if(ConnectionState != Arcor2SessionState.Open) {
                throw new InvalidOperationException("Session can be initialized only once.");
            }

            var systemInfoResult = await client.GetSystemInfoAsync();
            if(!systemInfoResult.Result) {
                throw new Arcor2Exception("Getting server information failed.", systemInfoResult.Messages);
            }

            ConnectionState = Arcor2SessionState.Initialized;

            if(!skipLoadingData) {
                await ReloadObjectTypesAsync();
                await ReloadScenesAsync();
                await ReloadProjectsAsync();
            }
            return systemInfoResult.Data;
        }

        /// <summary>
        /// Registers a user for this session and if there is opened, online scene, subscribes to robot events.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<List<ActionObjectManager>> RegisterAndSubscribeAsync(string username) {
            var registrationResult = await client.RegisterUserAsync(new RegisterUserRequestArgs(username));
            if(!registrationResult.Result) {
                throw new Arcor2Exception("User registration failed.", registrationResult.Messages);
            }

            if(NavigationState == NavigationState.Scene) {
                var scene = Scenes.First(s => s.Id == NavigationId);
                if(scene.State.State == OnlineState.Started) {
                    await scene!.GetRobotInfoAndUpdatesAsync();
                }
            }

            if(NavigationState == NavigationState.Project) {
                var project = Projects.First(p => p.Id == NavigationId);
                if(project.Scene!.State.State == OnlineState.Started) {
                    await project.Scene.GetRobotInfoAndUpdatesAsync();
                }
            }
        }

        /// <summary>
        /// Updates the <see cref="Scenes"/> collection and scene metadata.
        /// </summary>
        /// <remarks>
        /// This method is called internally on initialization unless you specify otherwise and generally not needed to be ínvoked again.
        /// </remarks>
        /// <exception cref="Arcor2Exception" />
        public async Task ReloadScenesAsync() {
            var sceneResponse = await client.ListScenesAsync();
            if(!sceneResponse.Result) {
                throw new Arcor2Exception("Loading scenes failed.", sceneResponse.Messages);
            }

            var newScenes = sceneResponse.Data.Select(scene =>
                new BareScene(scene.Name, scene.Description, scene.Created, scene.Modified, scene.Modified, scene.Id)).ToList();

            // If we have unsaved opened scenes/projects, this call
            // will not list this scene. So we can't remove unlisted ones.
            // That should not be an issue though.
            /*var scenesToRemove = Scenes.Where(oldScene => newScenes.All(newScene => newScene.Id != oldScene.Id)).ToList();
            foreach(var scene in scenesToRemove) {
                scene.Dispose();
                Scenes.Remove(scene);
            }*/

            foreach(var newScene in newScenes) {
                var existingScene = Scenes.FirstOrDefault(s => s.Id == newScene.Id);
                if(existingScene != null) {
                    existingScene.UpdateAccordingToNewObject(newScene);
                }
                else {
                    Scenes.Add(new SceneManager(this, newScene));
                }
            }
        }

        /// <summary>
        /// Updates the <see cref="Projects"/> collection and project metadata.
        /// </summary>
        /// <remarks>
        /// This method is called internally on initialization unless you specify otherwise and generally not needed to be ínvoked again.
        /// </remarks>
        /// <exception cref="Arcor2Exception" />
        public async Task ReloadProjectsAsync() {
            var projectResponse = await client.ListProjectsAsync();
            if(!projectResponse.Result) {
                throw new Arcor2Exception("Loading projects failed.", projectResponse.Messages);
            }

            var newProjects = projectResponse.Data.Select(project =>
                new BareProject(project.Name, project.SceneId, project.Description, project.HasLogic, project.Created, project.Modified, project.IntModified, project.Id)).ToList();

            // If we have unsaved opened scenes/projects, this call
            // will not list this scene. So we can't remove unlisted ones.
            // That should not be an issue though.
            /*var projectsToRemove = Projects.Where(oldProject => newProjects.All(newProject => newProject.Id != oldProject.Id)).ToList();
            foreach(var project in projectsToRemove) {
                project.Dispose();
                Projects.Remove(project);
            }*/

            foreach(var newProject in newProjects) {
                var existingProject = Projects.FirstOrDefault(p => p.Id == newProject.Id);
                if(existingProject != null) {
                    existingProject.UpdateAccordingToNewObject(newProject);
                }
                else {
                    Projects.Add(new ProjectManager(this, newProject));
                }
            }
        }

        /// <summary>
        /// Updates the <see cref="ObjectTypes"/> collection.
        /// </summary>
        /// <remarks>
        /// This method is called internally on initialization unless you specify otherwise and generally not needed to be ínvoked again.
        /// </remarks>
        /// <exception cref="Arcor2Exception" />
        private async Task ReloadObjectTypesAsync() {
            var objectTypesResponse = await client.GetObjectTypesAsync();
            if(!objectTypesResponse.Result) {
                throw new Arcor2Exception("Loading object types failed.", objectTypesResponse.Messages);
            }

            var robotMetaResponse = await client.GetRobotMetaAsync();
            if(!robotMetaResponse.Result) {
                throw new Arcor2Exception("Getting robot meta failed.", robotMetaResponse.Messages);
            }

            var newObjectTypes =
                from objectTypeMeta in objectTypesResponse.Data
                join robotMeta in robotMetaResponse.Data on objectTypeMeta.Type equals robotMeta.Type into joinTable
                from r in joinTable.DefaultIfEmpty()
                select new { ObjectTypeMeta = objectTypeMeta, RobotMeta = r };

            var objectTypesToRemove = ObjectTypes.Where(oldObjectType => newObjectTypes.All(newObjectType => newObjectType.ObjectTypeMeta.Type != oldObjectType.Id)).ToList();
            foreach(var objectType in objectTypesToRemove) {
                objectType.Dispose();
                ObjectTypes.Remove(objectType);
            }

            foreach(var newObjectType in newObjectTypes) {
                var existingObjectType = ObjectTypes.FirstOrDefault(o => o.Id == newObjectType.ObjectTypeMeta.Type);
                if(existingObjectType != null) {
                    existingObjectType.UpdateAccordingToNewObject(newObjectType.ObjectTypeMeta, newObjectType.RobotMeta);
                }
                else {
                    ObjectTypes.Add(new ObjectTypeManager(this, newObjectType.ObjectTypeMeta, newObjectType.RobotMeta));
                }
            }

            foreach(var objectType in ObjectTypes) {
                await objectType.ReloadActionsAsync();
            }
        }

        /// <summary>
        /// Adds a new scene.
        /// </summary>
        /// <param name="name">The name for the scene.</param>
        /// <param name="description">The description for the scene.</param>
        /// <exception cref="Arcor2Exception" />
        public async Task CreateSceneAsync(string name, string description = "") {
            var response = await client.AddNewSceneAsync(new NewSceneRequestArgs(name, description));
            if(!response.Result) {
                throw new Arcor2Exception("Creating a new scene failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new project.
        /// </summary>
        /// <param name="scene">The parent scene.</param>
        /// <param name="name">The name for the project.</param>
        /// <param name="description">The description for the project.</param>
        /// <param name="hasLogic">Should project have logic?</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CreateProjectAsync(SceneManager scene, string name, string description = "", bool hasLogic = true) {
            var response = await client.AddNewProjectAsync(new NewProjectRequestArgs(scene.Id, name, description, hasLogic));
            if(!response.Result) {
                throw new Arcor2Exception("Creating a new project failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new project.
        /// </summary>
        /// <param name="sceneId">The parent scene ID.</param>
        /// <param name="name">The name for the project.</param>
        /// <param name="description">The description for the project.</param>
        /// <param name="hasLogic">Should project have logic?</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CreateProjectAsync(string sceneId, string name, string description = "", bool hasLogic = true) {
            var response = await client.AddNewProjectAsync(new NewProjectRequestArgs(sceneId, name, description, hasLogic));
            if(!response.Result) {
                throw new Arcor2Exception("Creating a new project failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new object type.
        /// </summary>
        /// <param name="meta">The metadata of the object type.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CreateObjectTypeAsync(ObjectTypeMeta meta) {
            var response = await client.AddNewObjectTypeAsync(meta);
            if(!response.Result) {
                throw new Arcor2Exception("Adding a new object type failed.", response.Messages);
            }
        }

        private void RegisterHandlers() {
            // Navigation
            client.OnShowMainScreen += OnShowMainScreen;
            client.OnOpenScene += OnOpenScene;
            client.OnSceneClosed += OnSceneClosed;
            client.OnOpenProject += OnOpenProject;
            client.OnProjectClosed += OnProjectClosed;

            // Addition of sub-entities
            client.OnObjectTypeAdded += OnObjectTypeAdded;
            client.OnSceneBaseUpdated += OnSceneBaseUpdated; // Duplication uses this
            client.OnProjectBaseUpdated += OnProjectBaseUpdated; // Duplication uses this
        }

        private void OnProjectClosed(object sender, EventArgs e) {
            // In the rare case the order is weird
            if(NavigationState == NavigationState.Project) {
                NavigationState = NavigationState.ProjectClosed;
            }
        }

        private void OnOpenProject(object sender, OpenProjectEventArgs e) {
            // Ad-Hoc create managers if needed
            var scene = Scenes.FirstOrDefault(s => s.Id == e.Data.Scene.Id);
            if(scene == null) {
                Scenes.Add(new SceneManager(this, e.Data.Scene));
            }
            else {
                scene.UpdateAccordingToNewObject(e.Data.Scene);
            }

            var project = Projects.FirstOrDefault(s => s.Id == e.Data.Project.Id);
            if(project == null) {
                Projects.Add(new ProjectManager(this, e.Data.Project));
            }
            else {
                project.UpdateAccordingToNewObject(e.Data.Project);
            }

            NavigationState = NavigationState.Project;
            NavigationId = e.Data.Project.Id;
        }

        private void OnSceneClosed(object sender, EventArgs e) {
            // In the rare case the order is weird
            if(NavigationState == NavigationState.Scene) {
                NavigationState = NavigationState.SceneClosed;
            }
        }

        private void OnOpenScene(object sender, OpenSceneEventArgs e) {
            // Ad-Hoc create a manager
            var scene = Scenes.FirstOrDefault(s => s.Meta.Id == e.Data.Scene.Id);
            if(scene == null) {
                Scenes.Add(new SceneManager(this, e.Data.Scene));
            }
            else {
                scene.UpdateAccordingToNewObject(e.Data.Scene);
            }

            NavigationState = NavigationState.Scene;
            NavigationId = e.Data.Scene.Id;
        }

        private void OnShowMainScreen(object sender, ShowMainScreenEventArgs e) {
            NavigationState = e.Data.What switch {
                ShowMainScreenData.WhatEnum.ScenesList => NavigationState.MenuListOfScenes,
                ShowMainScreenData.WhatEnum.ProjectsList => NavigationState.MenuListOfProjects,
                ShowMainScreenData.WhatEnum.PackagesList => NavigationState.MenuListOfPackages,
                _ => NavigationState
            };
            NavigationId = string.IsNullOrEmpty(e.Data.Highlight) ? null : e.Data.Highlight;
        }

        private void OnObjectTypeAdded(object sender, ObjectTypesEventArgs args) {
            foreach(var objectTypeMeta in args.ObjectTypes) {
                var objectType = new ObjectTypeManager(this, objectTypeMeta);
                ObjectTypes.Add(objectType);
            }
        }

        private void OnSceneBaseUpdated(object sender, BareSceneEventArgs e) {
            if(Scenes.All(s => s.Id != e.Scene.Id)) {
                Scenes.Add(new SceneManager(this, e.Scene));
            }
        }

        private void OnProjectBaseUpdated(object sender, BareProjectEventArgs e) {
            if(Projects.All(s => s.Id != e.Project.Id)) {
                Projects.Add(new ProjectManager(this, e.Project));
            }
        }
    }
}