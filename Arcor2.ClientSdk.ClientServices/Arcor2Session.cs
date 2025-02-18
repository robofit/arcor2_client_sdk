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
// 1. The Arcor2Session class serves as the main object or entrypoint and generally provides method similar to things you would find in a menu including connection management.
// 2. It also holds a collection of different Arcor2ObjectManagers, which represent another functional entities (Scene, ActionPoint, etc...), which then manage themselves through their lifetime.
//  2.1 The Arcor2Session class should only care about events and RPCs not delegable to other, more specific, Manager.
//      For example, server creating new instances of Scene or ObjectType is not delegable to any other Manager.
//      But an existing Manager is capable of updating its data, updating its own "sub-Managers" (e.g. SceneManager adding ActionObjectManager to its collection),
//      or even removing itself from the parent collection, and disposing itself. Imagine this whole structure as a tree, where parents add nodes, and the nodes modify and delete themselves.
//      This is not a dogma though, for example, if we can't statically deduct if event is addition or update to our internal DB
//      (this is the case for OpenScene and such [Handled here], as it can be sent before or after Scenes are loaded). Similarly, SceneBaseUpdate may be both rename (update) or for some reason
//      the result of duplication (addition) [Handled in both SceneManager and here].
//  2.2 The Arcor2Session instance is injected into those Managers. It provides them with the ability to use the client, logger, or verify some global state.
//  2.3 Always make sure Manager instances are properly disposed, they will otherwise at best leak a lot of memory (exponentially). At worst, they will cause unwanted behavior.

namespace Arcor2.ClientSdk.ClientServices
{

    /// <summary>
    /// Class used for session management and communication with ARCOR2 server.
    /// This class mostly offers actions you could do from the main screen.
    /// </summary>
    public class Arcor2Session {
        internal readonly Arcor2Client client;
        internal readonly IArcor2Logger? logger;

        public NavigationState NavigationState { get; set; } = NavigationState.None;
        public string? NavigationId = null;

        /// <summary>
        /// Collection of available object types.
        /// </summary>
        public IList<ObjectTypeManager> ObjectTypes { get; } = new List<ObjectTypeManager>();

        /// <summary>
        /// Collection of available scene metadata.
        /// </summary>
        public IList<SceneManager> Scenes { get; private set; } = new List<SceneManager>();

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
        public Arcor2Session(IWebSocket websocket,IArcor2Logger? logger = null) {
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
        /// Initializes a session by loading all object types nd their actions into <see cref="ObjectTypes"/> dictionary, registering a user, and getting the server information.
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>The server information</returns>
        /// <exception cref="Arcor2Exception" />
        /// <exception cref="Arcor2ConnectionException" />
        public async Task<SystemInfoResponseData> InitializeAsync(string username) {
            var taskRegistration = client.RegisterUserAsync(new RegisterUserRequestArgs(username));
            var taskSystemInfo = client.GetSystemInfoAsync();
            var taskLoadObjectTypes = LoadObjectTypesAsync();

            await Task.WhenAll(taskRegistration, taskSystemInfo, taskLoadObjectTypes);

            var registrationResult = await taskRegistration;
            if(!registrationResult.Result) {
                throw new Arcor2Exception("User registration failed.", registrationResult.Messages);
            }
            var systemInfoResult = await taskSystemInfo;
            if(!systemInfoResult.Result) {
                throw new Arcor2Exception("Getting server information failed.", systemInfoResult.Messages);
            }

            ConnectionState = Arcor2SessionState.Initialized;

            return systemInfoResult.Data;
        }

        /// <summary>
        /// Updates <see cref="Scenes"/> list.
        /// </summary>
        /// <exception cref="Arcor2Exception" />
        public async Task ReloadScenesAsync() {
            var sceneResponse = await client.ListScenesAsync();
            if (!sceneResponse.Result) {
                throw new Arcor2Exception("Loading scenes failed.", sceneResponse.Messages);
            }

            var oldScenes = Scenes;

            var newScenes = new List<SceneManager>();
            foreach(var scene in sceneResponse.Data) {
                newScenes.Add(new SceneManager(this, new BareScene(scene.Name, scene.Description, scene.Created, scene.Modified, scene.Modified, scene.Id)));
            }

            Scenes = newScenes;
            foreach (var scene in oldScenes) {
                scene.Dispose();
            }
        }

        /// <summary>
        /// Adds a new scene.
        /// </summary>
        /// <param name="name">The name for the scene.</param>
        /// <param name="description">The description for the scene.</param>
        /// <exception cref="Arcor2Exception" />
        public async Task AddNewSceneAsync(string name, string description = "") {
            var response = await client.AddNewSceneAsync(new NewSceneRequestArgs(name, description));
            if(!response.Result) {
                throw new Arcor2Exception("Adding a new scene failed.", response.Messages);
            }
        }

        private async Task LoadObjectTypesAsync() {
            var objectTypesResponse = await client.GetObjectTypesAsync();
            if(!objectTypesResponse.Result) {
                throw new Arcor2Exception("Failed to fetch object types.", objectTypesResponse.Messages);
            }

            // Start tasks to fetch actions for non-built-in object types
            var actionTasks = new List<Task>();
            foreach(var objectTypeMeta in objectTypesResponse.Data) {
                var objectType = new ObjectTypeManager(this, objectTypeMeta);
                ObjectTypes.Add(objectType);

                if(!objectTypeMeta.BuiltIn) {
                    var actionTask = LoadActionsAsync(objectType);
                    actionTasks.Add(actionTask);
                }
            }

            await Task.WhenAll(actionTasks);
        }

        private async Task LoadActionsAsync(ObjectTypeManager objectTypeManager) {
            var actions = await client.GetActionsAsync(new TypeArgs(objectTypeManager.Meta.Type));
            if(actions.Result) {
                objectTypeManager.Actions = actions.Data;
            }
            else {
                logger?.LogWarning(
                    $"The server returned an error when fetching actions for {objectTypeManager.Meta.Type} object type. Leaving it blank. Error messages: " +
                    string.Join(",", actions.Messages));
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
        }

        private void OnProjectClosed(object sender, EventArgs e) {
            // In the rare case the order is weird
            if(NavigationState == NavigationState.Project) {
                NavigationState = NavigationState.ProjectClosed;
            }
        }

        private void OnOpenProject(object sender, OpenProjectEventArgs e) {
            // Ad-Hoc create a manager
            var scene = Scenes.FirstOrDefault(s => s.Meta.Id == e.Data.Scene.Id);
            if(scene == null) {
                Scenes.Add(new SceneManager(this, e.Data.Scene));
            }
            else {
                scene.UpdateAccordingToNewObject(e.Data.Scene);
            }
            // TODO: Ad-Hoc create a project

            NavigationState = NavigationState.Project;
            NavigationId = e.Data.Project.Id;
        }

        private void OnSceneClosed(object sender, EventArgs e) {
            // In the rare case the order is weird
            if (NavigationState == NavigationState.Scene) {
                NavigationState = NavigationState.SceneClosed;
            }
        }

        private void OnOpenScene(object sender, OpenSceneEventArgs e) {
            // Ad-Hoc create a manager
            var scene = Scenes.FirstOrDefault(s => s.Meta.Id == e.Data.Scene.Id);
            if (scene == null) {
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
            NavigationId = null;
        }

        private void OnObjectTypeAdded(object sender, ObjectTypesEventArgs args) {
            foreach(var objectTypeMeta in args.ObjectTypes) {
                var objectType = new ObjectTypeManager(this, objectTypeMeta);
                ObjectTypes.Add(objectType);
            }
        }

        private void OnSceneBaseUpdated(object sender, BareSceneEventArgs e) {
            if (Scenes.All(s => s.Meta.Id != e.Scene.Id)) {
                Scenes.Add(new SceneManager(this, e.Scene));
            }
        }
    }
}