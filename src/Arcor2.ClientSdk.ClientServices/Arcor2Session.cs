using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.Managers;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.Design;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using PackageStateEventArgs = Arcor2.ClientSdk.Communication.PackageStateEventArgs;

namespace Arcor2.ClientSdk.ClientServices {

    /// <summary>
    /// Class used for session management and communication with ARCOR2 server.
    /// This class mostly offers actions you could do from the main screen.
    /// </summary>
    public class Arcor2Session : IDisposable {
        private bool disposed;
        // For storing the package state, because we receive PackageState before PackageInfo, and thus must store it. 
        private readonly Stack<PackageStateData> unopenedPackageStates = new Stack<PackageStateData>();
        internal readonly Arcor2Client Client;
        internal readonly IArcor2Logger? Logger;
        internal readonly Arcor2SessionSettings Settings;

        /// <summary>
        /// The registered username. 
        /// </summary>
        public string? Username { get; private set; }

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
        /// Raised when <see cref="NavigationState"/> changes.
        /// </summary>
        public event EventHandler<NavigationStateEventArgs>? NavigationStateChanged;

        /// <summary>
        /// The state of the session.
        /// </summary>
        public Arcor2SessionState ConnectionState { get; private set; } = Arcor2SessionState.None;


        /// <summary>
        /// The ID of a highlighted object or object needed for client view that is expected by the server
        /// (e.g., a scene or project ID).
        /// </summary>
        /// <value>
        /// The ID of an object, <c>null</c> if not applicable.
        /// </value>
        public string? NavigationId;

        internal ObservableCollection<ObjectTypeManager> objectTypes { get; } = new ObservableCollection<ObjectTypeManager>();
        /// <summary>
        /// Collection of available object types.
        /// </summary>
        /// <remarks>Loaded on initialization and automatically maintained.</remarks>
        public ReadOnlyObservableCollection<ObjectTypeManager> ObjectTypes { get; }

        internal ObservableCollection<SceneManager> scenes { get; } = new ObservableCollection<SceneManager>();
        /// <summary>
        /// Collection of available scenes.
        /// </summary>
        /// <remarks>
        /// If the server opens a scene, that scene will get fully initialized and added automatically (regardless of the current state).
        /// Furthermore, users may also invoke <see cref="SceneManager.LoadAsync"/> on the specific instance to load the action objects without opening it.
        /// </remarks>
        public ReadOnlyObservableCollection<SceneManager> Scenes { get; }

        internal ObservableCollection<ProjectManager> projects { get; } = new ObservableCollection<ProjectManager>();

        /// <summary>
        /// Collection of available projects.
        /// </summary>
        /// <remarks>
        /// If the server opens a project, that project (and corresponding scene) will get fully initialized and added automatically (regardless of the current state).
        /// </remarks>
        public ReadOnlyObservableCollection<ProjectManager> Projects { get; }

        internal ObservableCollection<PackageManager> packages { get; } = new ObservableCollection<PackageManager>();
        /// <summary>
        /// Collection of available packages.
        /// </summary>
        public ReadOnlyObservableCollection<PackageManager> Packages { get; }

        /// <summary>
        /// Raised when any connection-related error occurs.
        /// </summary>
        public event EventHandler<Exception>? ConnectionError;
        /// <summary>
        /// Raised when connection is closed.
        /// </summary>
        public event EventHandler? ConnectionClosed;
        /// <summary>
        /// Raised when connection is successfully opened.
        /// </summary>
        public event EventHandler? ConnectionOpened;

        /// <summary>
        /// Initializes a new instance of <see cref="Arcor2Session"/> class.
        /// </summary>
        /// <param name="settings">The session settings.</param>
        /// <param name="logger">A logger instance.</param>
        public Arcor2Session(Arcor2SessionSettings? settings = null, IArcor2Logger? logger = null) {
            Logger = logger;
            Settings = settings ?? new Arcor2SessionSettings();
            Client = new Arcor2Client(new Arcor2ClientSettings(), Logger);

            Client.ConnectionOpened += (sender, args) => {
                ConnectionState = Arcor2SessionState.Open;
                ConnectionOpened?.Invoke(this, EventArgs.Empty);
            };
            Client.ConnectionClosed += (sender, args) => {
                ConnectionState = Arcor2SessionState.Closed;
                ConnectionClosed?.Invoke(this, EventArgs.Empty);
            };
            Client.ConnectionError += ConnectionError;
            RegisterHandlers();

            ObjectTypes = new ReadOnlyObservableCollection<ObjectTypeManager>(objectTypes);
            Scenes = new ReadOnlyObservableCollection<SceneManager>(scenes);
            Projects = new ReadOnlyObservableCollection<ProjectManager>(projects);
            Packages = new ReadOnlyObservableCollection<PackageManager>(packages);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Arcor2Session"/> class.
        /// </summary>
        /// <param name="settings">The session settings.</param>
        /// <param name="websocket">A WebSocket object implementing the <see cref="IWebSocket"/> interface.</param>
        /// <param name="logger">A logger instance.</param>
        /// <exception cref="InvalidOperationException">If the provided WebSocket instance is not in the <see cref="WebSocketState.None"/> state.</exception>
        public Arcor2Session(IWebSocket websocket, Arcor2SessionSettings? settings = null, IArcor2Logger? logger = null) {
            if(websocket.State != WebSocketState.None) {
                throw new InvalidOperationException("The socket instance must be in the 'None' state.");
            }

            Logger = logger;
            Settings = settings ?? new Arcor2SessionSettings();
            Client = new Arcor2Client(websocket, new Arcor2ClientSettings(), Logger);

            Client.ConnectionOpened += (sender, args) => {
                ConnectionState = Arcor2SessionState.Open;
                ConnectionOpened?.Invoke(this, EventArgs.Empty);
            };
            Client.ConnectionClosed += (sender, args) => {
                ConnectionState = Arcor2SessionState.Closed;
                ConnectionClosed?.Invoke(this, EventArgs.Empty);
            };
            Client.ConnectionError += ConnectionError;

            ObjectTypes = new ReadOnlyObservableCollection<ObjectTypeManager>(objectTypes);
            Scenes = new ReadOnlyObservableCollection<SceneManager>(scenes);
            Projects = new ReadOnlyObservableCollection<ProjectManager>(projects);
            Packages = new ReadOnlyObservableCollection<PackageManager>(packages);
        }

        /// <summary>
        /// Retrieves the underlying <see cref="Arcor2Client"/> instance.
        /// </summary>
        /// <returns></returns>
        public Arcor2Client GetUnderlyingArcor2Client() => Client;

        /// <summary>
        /// Establishes a connection to ARCOR2 server.
        /// </summary>
        /// <param name="domain">Domain of the ARCOR2 server</param>
        /// <param name="port">Port od the ARCOR2 server</param>
        /// <exception cref="UriFormatException" />
        /// <exception cref="InvalidOperationException" />
        public async Task ConnectAsync(string domain, ushort port) {
            if(disposed) {
                throw new ObjectDisposedException(nameof(Arcor2Session));
            }
            await Client.ConnectAsync(domain, port);
        }

        /// <summary>
        /// Establishes a connection to ARCOR2 server.
        /// </summary>
        /// <param name="uri">Full WebSocket URI</param>
        /// <exception cref="UriFormatException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="Communication.Arcor2ConnectionException"> When inner WebSocket fails to connect.</exception>
        public async Task ConnectAsync(Uri uri) {
            if(disposed) {
                throw new ObjectDisposedException(nameof(Arcor2Session));
            }
            await Client.ConnectAsync(uri);
        }

        /// <summary>
        /// Closes the connection to ARCOR2 sever and disposes the object.
        /// </summary>
        /// <exception cref="InvalidOperationException">When closed in the <see cref="Arcor2SessionState.Closed"/>.</exception>
        public async Task CloseAsync() {
            if(disposed) {
                throw new ObjectDisposedException(nameof(Arcor2Session));
            }
            await Client.CloseAsync();
            Dispose();
        }

        /// <summary>
        /// Disposes the object and if needed, closes the connection to ARCOR2 server.
        /// </summary>
        /// <remarks>
        /// Practically idempotent version of <see cref="CloseAsync"/>.
        /// </remarks>
        public void Dispose() {
            if(ConnectionState != Arcor2SessionState.Closed && ConnectionState != Arcor2SessionState.None) {
                Client.CloseAsync().GetAwaiter().GetResult();
            }

            UnregisterHandlers();
            foreach(var scene in Scenes) {
                scene.Dispose();
            }
            foreach(var project in Projects) {
                project.Dispose();
            }
            foreach(var package in Packages) {
                package.Dispose();
            }
            foreach(var objectType in ObjectTypes) {
                objectType.Dispose();
            }

            disposed = true;
        }

        /// <summary>
        /// Initializes a session. Internally loads all object types, actions, scenes, project, etc..., and returns the server information.
        /// </summary>
        /// <returns>The server information.</returns>
        /// <exception cref="Arcor2Exception" />
        /// <exception cref="Arcor2ConnectionException" />
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<SystemInfoResponseData> InitializeAsync() {
            if(disposed) {
                throw new ObjectDisposedException(nameof(Arcor2Session));
            }

            if(ConnectionState != Arcor2SessionState.Open) {
                throw new InvalidOperationException("Session can be initialized only once.");
            }

            var systemInfoResult = await Client.GetSystemInfoAsync();
            if(!systemInfoResult.Result) {
                throw new Arcor2Exception("Getting server information failed.", systemInfoResult.Messages);
            }

            ConnectionState = Arcor2SessionState.Initialized;

            if(Settings.LoadData) {
                await ReloadObjectTypesAsync();
                await ReloadScenesAsync();
                await ReloadProjectsAsync();
                await ReloadPackagesAsync();
            }
            return systemInfoResult.Data;
        }

        /// <summary>
        /// Registers a user for this session and if there is opened, online scene or project, subscribes to robot events.
        /// </summary>
        /// <remarks>
        /// If any robot is locked making us unable to register for its events, the registration will retry when it gets unlocked.
        /// </remarks>
        /// <param name="username">The username.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RegisterAndSubscribeAsync(string username) {
            var registrationResult = await Client.RegisterUserAsync(new RegisterUserRequestArgs(username));
            if(!registrationResult.Result) {
                throw new Arcor2Exception("User registration failed.", registrationResult.Messages);
            }
            Username = username;
            ConnectionState = Arcor2SessionState.Registered;

            await RegisterForActiveRobotEvents();
        }

        /// <summary>
        /// Updates the <see cref="Scenes"/> collection and scene metadata.
        /// </summary>
        /// <remarks>
        /// This method is called internally on initialization unless you specify otherwise and generally not needed to be ínvoked again.
        /// </remarks>
        /// <exception cref="Arcor2Exception" />
        public async Task ReloadScenesAsync() {
            var sceneResponse = await Client.ListScenesAsync();
            if(!sceneResponse.Result) {
                throw new Arcor2Exception("Loading scenes failed.", sceneResponse.Messages);
            }

            var newScenes = sceneResponse.Data.Select(scene =>
                new BareScene(scene.Name, scene.Description, scene.Created, scene.Modified, scene.Modified, scene.Id)).ToList();

            var scenesToRemove = scenes
                .Where(oldScene => newScenes.All(newScene => newScene.Id != oldScene.Id))
                // New opened unsaved scene are not returned by this RPC... so check if there is one active
                .Where(scene => NavigationState != NavigationState.Scene || scene.Id != NavigationId)
                .ToList();

            foreach(var scene in scenesToRemove) {
                scenes.Remove(scene);
                scene.Dispose();
            }

            foreach(var newScene in newScenes) {
                var existingScene = Scenes.FirstOrDefault(s => s.Id == newScene.Id);
                if(existingScene != null) {
                    existingScene.UpdateAccordingToNewObject(newScene);
                }
                else {
                    scenes.Add(new SceneManager(this, newScene));
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
            var projectResponse = await Client.ListProjectsAsync();
            if(!projectResponse.Result) {
                throw new Arcor2Exception("Loading projects failed.", projectResponse.Messages);
            }

            var newProjects = projectResponse.Data.Select(project =>
                new BareProject(project.Name, project.SceneId, project.Description, project.HasLogic, project.Created, project.Modified, project.IntModified, project.Id)).ToList();

            var projectsToRemove = projects
                .Where(oldProject => newProjects.All(newProject => newProject.Id != oldProject.Id))
                // New opened unsaved projects are not returned by this RPC... so check if there is one active
                .Where(project => NavigationState != NavigationState.Project || project.Id != NavigationId)
                .ToList();

            foreach(var project in projectsToRemove) {
                projects.Remove(project);
                project.Dispose();
            }

            foreach(var newProject in newProjects) {
                var existingProject = Projects.FirstOrDefault(p => p.Id == newProject.Id);
                if(existingProject != null) {
                    existingProject.UpdateAccordingToNewObject(newProject);
                }
                else {
                    projects.Add(new ProjectManager(this, newProject));
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
        public async Task ReloadPackagesAsync() {
            var packageResponse = await Client.ListPackagesAsync();
            if(!packageResponse.Result) {
                throw new Arcor2Exception("Loading packages failed.", packageResponse.Messages);
            }

            var packagesToRemove = packages
                .Where(oldPackage => packageResponse.Data.All(newPackage => newPackage.Id != oldPackage.Id))
                // New opened unsaved packages (e.g., temporary) are not returned by this RPC... so check if there is one active
                .Where(package => NavigationState != NavigationState.Package || package.Id != NavigationId)
                .ToList();

            foreach(var package in packagesToRemove) {
                packages.Remove(package);
                package.Dispose();
            }

            foreach(var package in packageResponse.Data) {
                var existingPackage = Packages.FirstOrDefault(p => p.Id == package.Id);
                if(existingPackage != null) {
                    existingPackage.UpdateAccordingToNewObject(package);
                }
                else {
                    packages.Add(new PackageManager(this, package));
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
            var objectTypesResponse = await Client.GetObjectTypesAsync();
            if(!objectTypesResponse.Result) {
                throw new Arcor2Exception("Loading object types failed.", objectTypesResponse.Messages);
            }

            var robotMetaResponse = await Client.GetRobotMetaAsync();
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
                objectTypes.Remove(objectType);
            }

            foreach(var newObjectType in newObjectTypes) {
                var existingObjectType = ObjectTypes.FirstOrDefault(o => o.Id == newObjectType.ObjectTypeMeta.Type);
                if(existingObjectType != null) {
                    existingObjectType.UpdateAccordingToNewObject(newObjectType.ObjectTypeMeta, newObjectType.RobotMeta);
                }
                else {
                    objectTypes.Add(new ObjectTypeManager(this, newObjectType.ObjectTypeMeta, newObjectType.RobotMeta));
                }
            }

            foreach(var objectType in objectTypes) {
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
            var response = await Client.AddNewSceneAsync(new NewSceneRequestArgs(name, description));
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
            var response = await Client.AddNewProjectAsync(new NewProjectRequestArgs(scene.Id, name, description, hasLogic));
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
            var response = await Client.AddNewProjectAsync(new NewProjectRequestArgs(sceneId, name, description, hasLogic));
            if(!response.Result) {
                throw new Arcor2Exception("Creating a new project failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new object type.
        /// </summary>
        /// <param name="type">The object type ID.</param>
        /// <param name="description">The description. By default, <c>null</c>.</param>
        /// <param name="base">The object type ID of the parent object type. By default, Generic.</param>
        /// <param name="model">The model of the object. By default, <c>null</c> meaning no model. Must be based on CollisionModel type.</param>
        /// <param name="sceneParentObjectType">The required parent action object in scene. By default, <c>null</c> meaning no required parent.</param>
        /// <param name="hasPose">When <c>true</c>, the object type will have a pose. By default, <c>false</c>.</param>
        /// <param name="isAbstract">When <c>true</c>, the object type will be marked as abstract and can't be instantiated into action object. By default, <c>false</c>.</param>
        /// <param name="isDisabled">When <c>true</c>, the object type won't be usable. By default, <c>false</c>.</param>
        /// <param name="parameters">A list of parameter definitions.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CreateObjectTypeAsync(string type, string @base = "Generic", string? description = null, CollisionModel? model = null, string? sceneParentObjectType = null, bool hasPose = false, bool isAbstract = false, bool isDisabled = false, List<ParameterMeta>? parameters =  null) {
            var meta = new ObjectTypeMeta(type, description!, false, @base, model?.ToObjectModel(type)!, sceneParentObjectType!,
                hasPose, isAbstract, isDisabled, null!, parameters ?? new List<ParameterMeta>());
            var response = await Client.AddNewObjectTypeAsync(meta);
            if(!response.Result) {
                throw new Arcor2Exception("Adding a new object type failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new object type.
        /// </summary>
        /// <param name="type">The object type ID.</param>
        /// <param name="description">The description. By default, <c>null</c>.</param>
        /// <param name="base">The type of the parent object type. By default, Generic.</param>
        /// <param name="model">The model of the object. By default, <c>null</c> meaning no model.</param>
        /// <param name="sceneParentObjectType">The required parent action object in scene. By default, <c>null</c> meaning no required parent.</param>
        /// <param name="hasPose">When <c>true</c>, the object type will have a pose. By default, <c>false</c>.</param>
        /// <param name="isAbstract">When <c>true</c>, the object type will be marked as abstract and can't be instantiated into action object. By default, <c>false</c>.</param>
        /// <param name="isDisabled">When <c>true</c>, the object type won't be usable. By default, <c>false</c>.</param>
        /// <param name="parameters">A list of parameter definitions.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CreateObjectTypeAsync(string type, ObjectTypeManager @base, string? description = null, CollisionModel? model = null, ObjectTypeManager? sceneParentObjectType = null, bool hasPose = false, bool isAbstract = false, bool isDisabled = false, List<ParameterMeta>? parameters = null) {
            var meta = new ObjectTypeMeta(type, description!, false, @base.Id, model?.ToObjectModel(type)!, sceneParentObjectType?.Id ?? null!,
                hasPose, isAbstract, isDisabled, null!, parameters ?? new List<ParameterMeta>());
            var response = await Client.AddNewObjectTypeAsync(meta);
            if(!response.Result) {
                throw new Arcor2Exception("Adding a new object type failed.", response.Messages);
            }
        }

        /// <summary>
        /// Uploads a package to the server.
        /// </summary>
        /// <param name="packageId">The ID of the package.</param>
        /// <param name="encodedPackage">The package ZIP file encoded as base64 string.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UploadPackageAsync(string packageId, string encodedPackage) {
            var response = await Client.UploadPackageAsync(new UploadPackageRequestArgs(packageId, encodedPackage));
            if(!response.Result) {
                throw new Arcor2Exception($"Upload package {packageId} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Estimates the pose of the camera relative to detected markers in an image.
        /// </summary>
        /// <param name="cameraParameters">Intrinsic camera parameters (camera matrix and distortion coefficients).</param>
        /// <param name="image">Raw byte array representing the image in JPEG format.</param>
        /// <param name="inverse">If true, the returned pose represents the camera's position and orientation relative to the marker. If false, it represents the marker's pose relative to the camera.</param>
        /// <returns>An <see cref="EstimatedPose"/> object containing the camera's position and orientation relative to the detected markers.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<EstimatedPose> EstimateCameraPoseAsync(CameraParameters cameraParameters, byte[] image, bool inverse = false) {
            var encodedImage = Encoding.GetEncoding("iso-8859-1").GetString(image);
            var response = await Client.GetCameraPoseAsync(new GetCameraPoseRequestArgs(cameraParameters, encodedImage, inverse));
            if(!response.Result) {
                throw new Arcor2Exception($"Estimating camera pose failed.", response.Messages);
            }
            return response.Data;
        }


        /// <summary>
        /// Detects the corners of markers in an image and returns their coordinates.
        /// </summary>
        /// <param name="cameraParameters">Intrinsic camera parameters (camera matrix and distortion coefficients).</param>
        /// <param name="image">Raw byte array representing the image in JPEG format.</param>
        /// <returns>A list of <see cref="MarkerCorners"/> objects representing the coordinates of detected marker corners.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<List<MarkerCorners>> EstimateMarkerCornersAsync(CameraParameters cameraParameters, byte[] image) {
            var encodedImage = Encoding.GetEncoding("iso-8859-1").GetString(image);
            var response = await Client.GetMarkersCornersAsync(new MarkersCornersRequestArgs(cameraParameters, encodedImage));
            if(!response.Result) {
                throw new Arcor2Exception($"Estimating marker corners failed.", response.Messages);
            }

            return response.Data;
        }
        /// <summary>
        /// Subscribes to robot events if there is online opened scene.
        /// </summary>
        private async Task RegisterForActiveRobotEvents() {
            var scene = NavigationState == NavigationState.Scene ?
                Scenes.First(s => s.Id == NavigationId) :
                NavigationState == NavigationState.Project ?
                    Projects.First(p => p.Id == NavigationId).Scene :
                    null;
            if(scene is { State: { State: OnlineState.Started } }) {
                var robotsFailedToRegister = await scene.GetRobotInfoAndUpdatesAsync();
                foreach(var robot in robotsFailedToRegister) {
                    // Check if it is locked to be sure of the error origin.
                    if(robot.IsLocked) {
                        ConfigureResubscriptionOnUnlock(robot);
                    }
                    else {
                        // If it is locked, it wasn't an error,
                        // but just someone holding a lock for prolonged time
                        if(robot.IsLocked) {
                            ConfigureResubscriptionOnUnlock(robot);
                        }
                        else {
                            // The lock could come with a delay after the registration.
                            // Check again in 100ms. No harm in doing so.
                            await Task.Delay(100);
                            if(robot.IsLocked) {
                                ConfigureResubscriptionOnUnlock(robot);
                            }
                            else {
                                // Now we can assume it was really an error.
                                Logger?.LogError($"Robot {robot.Id} couldn't be subscribed for updates on registration.");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Configures one time event that resubscribes to robot events on unlock.
        /// </summary>
        private void ConfigureResubscriptionOnUnlock(ActionObjectManager robot) {
            Logger?.LogInfo($"Robot {robot.Id} was locked on registration. Retrying on unlock.");
            EventHandler setRemoveFlag = null!;
            EventHandler<LockEventArgs> resubscribeOnUnlock = null!;

            var id = NavigationId;
            object wasRemoved = false;
            setRemoveFlag = (sender, e) => {
                wasRemoved = true;
                // ReSharper disable once AccessToModifiedClosure
                robot.Removing -= setRemoveFlag;
            };

            // ReSharper disable once AsyncVoidLambda
            resubscribeOnUnlock = async (sender, e) => {
                // Check that we are in the same scene and the object still exists.
                if(NavigationId == id && !(bool) wasRemoved) {
                    var actionObject = (ActionObjectManager) sender;
                    try {
                        if(ConnectionState != Arcor2SessionState.Closed) {
                            await actionObject.ReloadRobotArmsAndEefPoseAsync();
                            await actionObject.ReloadRobotJointsAsync();
                            await actionObject.RegisterForUpdatesAsync(RobotUpdateType.Joints);
                            await actionObject.RegisterForUpdatesAsync(RobotUpdateType.Pose);
                            Logger?.LogInfo($"Successfully retried subscription to robot {robot.Id}.");
                        }
                    }
                    catch(Arcor2Exception ex) {
                        Logger?.LogError($"Failed retried subscription to robot {robot.Id} with \"{ex.Message}\".");
                    }
                    // ReSharper disable once AccessToModifiedClosure
                    actionObject.Unlocked -= resubscribeOnUnlock;
                }
            };
            robot.Removing += setRemoveFlag;
            robot.Unlocked += resubscribeOnUnlock;
        }

        private void RegisterHandlers() {
            // Navigation
            Client.ShowMainScreen += OnShowMainScreen;
            Client.SceneOpened += OnSceneOpened;
            Client.SceneClosed += OnSceneClosed;
            Client.ProjectOpened += OnProjectOpened;
            Client.ProjectClosed += OnProjectClosed;

            // Addition of sub-entities
            Client.ObjectTypeAdded += OnObjectTypeAdded;
            Client.SceneBaseUpdated += OnSceneBaseUpdated; // Duplication uses this
            Client.ProjectBaseUpdated += OnProjectBaseUpdated; // Duplication uses this
            Client.PackageAdded += OnPackageAdded;

            Client.PackageState += OnPackageState;
            Client.PackageInfo += OnPackageInfo;
        }

        private void UnregisterHandlers() {
            // Navigation
            Client.ShowMainScreen -= OnShowMainScreen;
            Client.SceneOpened -= OnSceneOpened;
            Client.SceneClosed -= OnSceneClosed;
            Client.ProjectOpened -= OnProjectOpened;
            Client.ProjectClosed -= OnProjectClosed;

            // Addition of sub-entities
            Client.ObjectTypeAdded -= OnObjectTypeAdded;
            Client.SceneBaseUpdated -= OnSceneBaseUpdated; // Duplication uses this
            Client.ProjectBaseUpdated -= OnProjectBaseUpdated; // Duplication uses this
            Client.PackageAdded -= OnPackageAdded;

            Client.PackageState -= OnPackageState;
            Client.PackageInfo -= OnPackageInfo;
        }

        private void OnPackageAdded(object sender, PackageEventArgs e) {
            packages.Add(new PackageManager(this, e.Data));
        }

        private async void OnProjectClosed(object sender, EventArgs e) {
            // In the rare case the order is weird
            if(NavigationState == NavigationState.Project) {
                NavigationState = NavigationState.ProjectClosed;
            }

            if(ConnectionState != Arcor2SessionState.Closed) {
                try {
                    await ReloadProjectsAsync();
                }
                catch { /*It is possible close can happen soon*/ }
            }

            NavigationStateChanged?.Invoke(this, new NavigationStateEventArgs(NavigationState, NavigationId));
        }

        private void OnProjectOpened(object sender, OpenProjectEventArgs e) {
            // Ad-Hoc create managers if needed
            var scene = Scenes.FirstOrDefault(s => s.Id == e.Data.Scene.Id);
            if(scene == null) {
                scenes.Add(new SceneManager(this, e.Data.Scene));
            }
            else {
                scene.UpdateAccordingToNewObject(e.Data.Scene);
            }

            var project = Projects.FirstOrDefault(s => s.Id == e.Data.Project.Id);
            if(project == null) {
                projects.Add(new ProjectManager(this, e.Data.Project));
            }
            else {
                project.UpdateAccordingToNewObject(e.Data.Project);
            }

            NavigationState = NavigationState.Project;
            NavigationId = e.Data.Project.Id;
            NavigationStateChanged?.Invoke(this, new NavigationStateEventArgs(NavigationState, NavigationId));
        }

        private async void OnSceneClosed(object sender, EventArgs e) {
            // In the rare case the order is weird
            if(NavigationState == NavigationState.Scene) {
                NavigationState = NavigationState.SceneClosed;
            }

            if(ConnectionState != Arcor2SessionState.Closed) {
                try {
                    await ReloadScenesAsync();
                }
                catch { /*It is possible close can happen soon*/ }
            }
            NavigationStateChanged?.Invoke(this, new NavigationStateEventArgs(NavigationState, NavigationId));
        }

        private void OnSceneOpened(object sender, OpenSceneEventArgs e) {
            // Ad-Hoc create a manager
            var scene = Scenes.FirstOrDefault(s => s.Data.Id == e.Data.Scene.Id);
            if(scene == null) {
                scenes.Add(new SceneManager(this, e.Data.Scene));
            }
            else {
                scene.UpdateAccordingToNewObject(e.Data.Scene);
            }

            NavigationState = NavigationState.Scene;
            NavigationId = e.Data.Scene.Id;
            NavigationStateChanged?.Invoke(this, new NavigationStateEventArgs(NavigationState, NavigationId));
        }

        private async void OnShowMainScreen(object sender, ShowMainScreenEventArgs e) {
            NavigationState = e.Data.What switch {
                ShowMainScreenData.WhatEnum.ScenesList => NavigationState.MenuListOfScenes,
                ShowMainScreenData.WhatEnum.ProjectsList => NavigationState.MenuListOfProjects,
                ShowMainScreenData.WhatEnum.PackagesList => NavigationState.MenuListOfPackages,
                _ => NavigationState
            };
            if(NavigationState == NavigationState.MenuListOfPackages) {
                // This will rid the list of temporary packages
                if(ConnectionState != Arcor2SessionState.Closed) {
                    try {
                        await ReloadPackagesAsync();
                    }
                    catch { /*It is possible close can happen soon*/ }
                }
            }

            NavigationId = string.IsNullOrEmpty(e.Data.Highlight) ? null : e.Data.Highlight;
            NavigationStateChanged?.Invoke(this, new NavigationStateEventArgs(NavigationState, NavigationId));
        }

        private void OnObjectTypeAdded(object sender, ObjectTypesEventArgs args) {
            foreach(var objectTypeMeta in args.Data) {
                var objectType = new ObjectTypeManager(this, objectTypeMeta);
                objectTypes.Add(objectType);
            }
        }

        private void OnSceneBaseUpdated(object sender, BareSceneEventArgs e) {
            if(scenes.All(s => s.Id != e.Data.Id)) {
                scenes.Add(new SceneManager(this, e.Data));
            }
        }

        private void OnProjectBaseUpdated(object sender, BareProjectEventArgs e) {
            if(projects.All(s => s.Id != e.Data.Id)) {
                projects.Add(new ProjectManager(this, e.Data));
            }
        }

        private void OnPackageState(object sender, PackageStateEventArgs e) {
            if(packages.FirstOrDefault(p => p.Id == e.Data.PackageId) == null) {
                unopenedPackageStates.Push(e.Data);
            }
        }

        private void OnPackageInfo(object sender, PackageInfoEventArgs e) {
            var scene = scenes.FirstOrDefault(s => s.Id == e.Data.Scene.Id);
            if(scene == null) {
                scenes.Add(new SceneManager(this, e.Data.Scene));
            }
            else {
                scene.UpdateAccordingToNewObject(e.Data.Scene);
            }

            var project = projects.FirstOrDefault(s => s.Id == e.Data.Project.Id);
            if(project == null) {
                projects.Add(new ProjectManager(this, e.Data.Project));
            }
            else {
                project.UpdateAccordingToNewObject(e.Data.Project);
            }

            var package = packages.FirstOrDefault(p => p.Id == e.Data.PackageId);
            if(package == null) {
                if(unopenedPackageStates.TryPeek(out var data) && data.PackageId == e.Data.PackageId) {
                    packages.Add(new PackageManager(this, e.Data, data));
                    unopenedPackageStates.Pop();
                }
                else {
                    packages.Add(new PackageManager(this, e.Data));
                }
            }
            else {
                package.UpdateAccordingToNewObject(e.Data);
            }

            NavigationState = NavigationState.Package;
            NavigationId = e.Data.PackageId;
            NavigationStateChanged?.Invoke(this, new NavigationStateEventArgs(NavigationState, NavigationId));
        }
    }
}