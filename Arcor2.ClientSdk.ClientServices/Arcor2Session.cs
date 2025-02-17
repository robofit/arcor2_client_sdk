using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.Design;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices
{

    /// <summary>
    /// Class used for session management and communication with ARCOR2 server.
    /// This class mostly offers actions you could do from the main screen.
    /// </summary>
    public class Arcor2Session : Arcor2Session<SystemNetWebSocket> {
        /// <inheritdoc/>
        public Arcor2Session(IArcor2Logger? logger = null) : base(logger) { }
    };

    /// <summary>
    /// Class used for session management and communication with ARCOR2 server using custom WebSocket implementation.
    /// This class mostly offers actions you could do from the main screen.
    /// </summary>
    /// <typeparam name="TWebSocket">WebSocket implementation</typeparam>
    public class Arcor2Session<TWebSocket> where TWebSocket : class, IWebSocket, new() {
        internal readonly Arcor2Client<TWebSocket> client;
        internal readonly IArcor2Logger? logger;

        /// <summary>
        /// Collection of available object types.
        /// </summary>
        public IList<ObjectType> ObjectTypes { get; } = new List<ObjectType>();
        /// <summary>
        /// Raised when <see cref="ObjectTypes"/> collection or any of its members is modified by the server.
        /// </summary>
        public event EventHandler? OnObjectTypesChanged;

        /// <summary>
        /// Collection of available scene metadata.
        /// </summary>
        public IList<BareScene> Scenes { get; private set; } = new List<BareScene>();
        /// <summary>
        /// Raised when <see cref="Scenes"/> collection or any of its members is modified by the server.
        /// </summary>
        public event EventHandler? OnScenesChanged;

        #region Connection-related Memberss

        /// <summary>
        /// Initializes a new instance of <see cref="Arcor2Session"/> class.
        /// </summary>
        /// <param name="logger">A logger instance.</param>
        public Arcor2Session(IArcor2Logger? logger = null) {
            this.logger = logger;
            client = new Arcor2Client<TWebSocket>(new Arcor2ClientSettings(), this.logger);

            client.OnConnectionOpened += (sender, args) => {
                State = Arcor2SessionState.Open;
                OnConnectionOpened?.Invoke(this, EventArgs.Empty);
            };
            client.OnConnectionClosed += (sender, args) => {
                State = Arcor2SessionState.Closed;
                OnConnectionClosed?.Invoke(this, EventArgs.Empty);
            };
            client.OnConnectionError += OnConnectionError;
        }

        /// <summary>
        /// Retrieves the underlying <see cref="Arcor2Client"/> instance.
        /// </summary>
        /// <returns></returns>
        public Arcor2Client<TWebSocket> GetUnderlyingArcor2Client() => client;

        /// <summary>
        /// The state of the session.
        /// </summary>
        public Arcor2SessionState State { get; private set; } = Arcor2SessionState.None;

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
            var taskLoadObjectTypes = LoadObjectTypes();

            await Task.WhenAll(taskRegistration, taskSystemInfo, taskLoadObjectTypes);

            var registrationResult = await taskRegistration;
            if(!registrationResult.Result) {
                throw new Arcor2Exception("User registration failed.", registrationResult.Messages);
            }
            var systemInfoResult = await taskSystemInfo;
            if(!systemInfoResult.Result) {
                throw new Arcor2Exception("Getting server information failed.", systemInfoResult.Messages);
            }

            State = Arcor2SessionState.Initialized;
            // Notify object type change
            OnObjectTypesChanged?.Invoke(this, EventArgs.Empty);

            RegisterObjectTypeHandlers();

            return systemInfoResult.Data;
        }

        /// <summary>
        /// Updates <see cref="Scenes"/> list.
        /// </summary>
        public async Task LoadScenes() {
            var sceneResponse = await client.ListScenesAsync();
            if (!sceneResponse.Result) {
                throw new Arcor2Exception("Loading scenes failed.", sceneResponse.Messages);
            }

            Scenes = new List<BareScene>();
            foreach (var scene in sceneResponse.Data) {
                Scenes.Add(new BareScene(scene.Name, scene.Description, scene.Created, scene.Modified, scene.Modified, scene.Id));
            }
        }

        public async Task RenameScene(string id, string newName) {
            var @lock = await client.WriteLockAsync(new WriteLockRequestArgs(id));
            if (!@lock.Result) {
                throw new Arcor2Exception("Renaming scene failed.", @lock.Messages);
            }

            var response = await client.RenameSceneAsync(new RenameArgs(id, newName));
            if (!response.Result) {
                throw new Arcor2Exception("Renaming scene failed.", response.Messages);
            }
        }

        /*
        /// <summary>
        /// Gets a list of available projects.
        /// </summary>
        /// <returns>List of project metadata.</returns>
        public async Task<IList<ListProjectsResponseData>> ListProjects() {
            var projectResponse = await client.ListProjectsAsync();
            if(!projectResponse.Result) {
                throw new Arcor2Exception("Listing projects failed", projectResponse.Messages);
            }
            return projectResponse.Data;
        }

        /// <summary>
        /// Gets a list of available packages.
        /// </summary>
        /// <returns>List of package metadata.</returns>
        public async Task<IList<PackageSummary>> ListPackages() {
            var packageResponse = await client.ListPackagesAsync();
            if(!packageResponse.Result) {
                throw new Arcor2Exception("Listing projects failed", packageResponse.Messages);
            }
            return packageResponse.Data;
        }*/

        private async Task LoadObjectTypes() {
            var objectTypesResponse = await client.GetObjectTypesAsync();
            if(!objectTypesResponse.Result) {
                throw new Arcor2Exception("Failed to fetch object types.", objectTypesResponse.Messages);
            }

            // Start tasks to fetch actions for non-built-in object types
            var actionTasks = new List<Task>();
            foreach(var objectTypeMeta in objectTypesResponse.Data) {
                var objectType = new ObjectType(objectTypeMeta);
                ObjectTypes.Add(objectType);

                if(!objectTypeMeta.BuiltIn) {
                    var actionTask = LoadActions(objectType);
                    actionTasks.Add(actionTask);
                }
            }

            await Task.WhenAll(actionTasks);
        }

        private async Task LoadActions(ObjectType objectType) {
            var actions = await client.GetActionsAsync(new TypeArgs(objectType.Meta.Type));
            if(actions.Result) {
                objectType.Actions = actions.Data;
            }
            else {
                logger?.LogWarning(
                    $"The server returned an error when fetching actions for {objectType.Meta.Type} object type. Leaving it blank. Error messages: " +
                    string.Join(",", actions.Messages));
            }
        }

        private void RegisterObjectTypeHandlers() {
            client.OnObjectTypeAdded += async (sender, args) => {
                foreach (var objectTypeMeta in args.ObjectTypes) {
                    var objectType = new ObjectType(objectTypeMeta);
                    await LoadActions(objectType);
                    OnObjectTypesChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            client.OnObjectTypeUpdated += (sender, args) => {
                foreach (var objectTypeMeta in args.ObjectTypes) {
                    var objectTypeToChange = ObjectTypes.FirstOrDefault(o => o.Meta.Type == objectTypeMeta.Type);
                    if (objectTypeToChange == null) {
                        logger?.LogWarning(
                            $"The server requested an update for {objectTypeMeta.Type} object type, which is not in internal list.");
                        continue;
                    }

                    objectTypeToChange.Meta = objectTypeMeta;
                    OnObjectTypesChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            client.OnObjectTypeRemoved += (sender, args) => {
                foreach (var objectTypeMeta in args.ObjectTypes) {
                    var objectTypeToRemove = ObjectTypes.FirstOrDefault(o => o.Meta.Type == objectTypeMeta.Type);
                    if (objectTypeToRemove == null) {
                        logger?.LogWarning(
                            $"The server requested a remove for {objectTypeMeta.Type} object type, which is not in internal list.");
                        continue;
                    }

                    ObjectTypes.Remove(objectTypeToRemove);
                    OnObjectTypesChanged?.Invoke(this, EventArgs.Empty);
                }
            };
        }
    }
}