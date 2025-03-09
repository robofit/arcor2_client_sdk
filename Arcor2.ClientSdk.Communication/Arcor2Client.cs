using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcor2.ClientSdk.Communication.Design;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Action = Arcor2.ClientSdk.Communication.OpenApi.Models.Action;

namespace Arcor2.ClientSdk.Communication {
    /// <summary>
    /// Client for communication with ARCOR2 servers.
    /// </summary>
    public class Arcor2Client {
        private IWebSocket webSocket;

        /// <summary>
        /// Represents a request waiting for corresponding response.
        /// </summary>
        private class PendingRequest {
            public TaskCompletionSource<string> TaskCompletionSource { get; } = new TaskCompletionSource<string>();
            public CancellationTokenSource CancellationTokenSource { get; }
            // Holds the name of the expected RPC response
            public string Signature { get; }

            public PendingRequest(uint timeout, string signature) {
                Signature = signature;
                CancellationTokenSource = new CancellationTokenSource((int) timeout);
                CancellationTokenSource.Token.Register(() => {
                    TaskCompletionSource.TrySetException(new TimeoutException("The request timed out."));
                }, false);
            }
        }

        private ConcurrentDictionary<int, PendingRequest> pendingRequests = new ConcurrentDictionary<int, PendingRequest>();

        /// <summary>
        /// Holds the current available request ID for tracking request-response messages.
        /// </summary>
        private int requestId;

        /// <summary>
        /// Current settings of the client.
        /// </summary>
        private readonly Arcor2ClientSettings clientSettings;

        /// <summary>
        /// Injected logger.
        /// </summary>
        private readonly IArcor2Logger? logger;

        /// <summary>
        /// JSON deserialization options.
        /// </summary>
        private readonly JsonSerializerSettings jsonSettings;

        /// <summary>
        /// The URI of the current server.
        /// </summary>
        public Uri? Uri { get; private set; }

        /// <summary>
        /// Represents the state of a connection to server.
        /// </summary>
        public WebSocketState ConnectionState => webSocket.State;

        #region Public Event Definitions

        /// <summary>
        /// Raised when any connection-related error occurs.
        /// </summary>
        public event EventHandler<Exception>? ConnectionError;
        /// <summary>
        /// Raised when connection is closed.
        /// </summary>
        public event EventHandler<WebSocketCloseEventArgs>? ConnectionClosed;
        /// <summary>
        /// Raised when connection is successfully opened.
        /// </summary>
        public event EventHandler? ConnectionOpened;

        /// <summary>
        /// Raised when scene is removed.
        /// </summary>
        public event EventHandler<BareSceneEventArgs>? SceneRemoved;
        /// <summary>
        /// Raised when scene base is updated (e.g. duplication or name/description update).
        /// </summary>
        public event EventHandler<BareSceneEventArgs>? SceneBaseUpdated;

        /// <summary>
        /// Raised when state of scene changes (stopping/stopped/starting/started).
        /// </summary>
        public event EventHandler<SceneStateEventArgs>? SceneState;

        /// <summary>
        /// Raised when action object is added. 
        /// </summary>
        public event EventHandler<ActionObjectEventArgs>? ActionObjectAdded;
        /// <summary>
        /// Raised when action object is removed.
        /// </summary>
        public event EventHandler<ActionObjectEventArgs>? ActionObjectRemoved;
        /// <summary>
        /// Raised when action object is updated (e.g. translated).
        /// </summary>
        public event EventHandler<ActionObjectEventArgs>? ActionObjectUpdated;

        /// <summary>
        /// Raised when action point is added.
        /// </summary>
        public event EventHandler<BareActionPointEventArgs>? ActionPointAdded;
        /// <summary>
        /// Raised when action point is updated (e.g. translated).
        /// </summary>
        public event EventHandler<BareActionPointEventArgs>? ActionPointUpdated;
        /// <summary>
        /// Raised when action point base is updated (e.g. renamed).
        /// </summary>
        public event EventHandler<BareActionPointEventArgs>? ActionPointBaseUpdated;
        /// <summary>
        /// Raised when action point is removed.
        /// </summary>
        public event EventHandler<BareActionPointEventArgs>? ActionPointRemoved;

        /// <summary>
        /// Raised when project override is added.
        /// </summary>
        public event EventHandler<ParameterEventArgs>? ProjectOverrideAdded;
        /// <summary>
        /// Raised when project override is updated (existing named value changed).
        /// </summary>
        public event EventHandler<ParameterEventArgs>? ProjectOverrideUpdated;
        /// <summary>
        /// Raised when project override is removed.
        /// </summary>
        public event EventHandler<ParameterEventArgs>? ProjectOverrideRemoved;

        /// <summary>
        /// Raised when action is added.
        /// </summary>
        public event EventHandler<ActionEventArgs>? ActionAdded;
        /// <summary>
        /// Raised when action is updated (e.g. parameters or flows).
        /// </summary>
        public event EventHandler<ActionEventArgs>? ActionUpdated;
        /// <summary>
        /// Raised when action base is updated (e.g. rename).
        /// </summary>
        public event EventHandler<ActionEventArgs>? ActionBaseUpdated;
        /// <summary>
        /// Raised when action is removed.
        /// </summary>
        public event EventHandler<BareActionEventArgs>? ActionRemoved;

        /// <summary>
        /// Raised when logic item is added.
        /// </summary>
        public event EventHandler<LogicItemEventArgs>? LogicItemAdded;
        /// <summary>
        /// Raised when logic item is updated.
        /// </summary>
        public event EventHandler<LogicItemEventArgs>? LogicItemUpdated;
        /// <summary>
        /// Raised when logic item is removed.
        /// </summary>
        public event EventHandler<LogicItemEventArgs>? LogicItemRemoved;

        /// <summary>
        /// Raised when new action point orientation is added.
        /// </summary>
        public event EventHandler<OrientationEventArgs>? OrientationAdded;
        /// <summary>
        /// Raised when action point orientation is updated.
        /// </summary>
        public event EventHandler<OrientationEventArgs>? OrientationUpdated;
        /// <summary>
        /// Raised when action point orientation base is updated (e.g. rename).
        /// </summary>
        public event EventHandler<OrientationEventArgs>? OrientationBaseUpdated;
        /// <summary>
        /// Raised when action point orientation is removed.
        /// </summary>
        public event EventHandler<OrientationEventArgs>? OrientationRemoved;

        /// <summary>
        /// Raised when new action point joints are added.
        /// </summary>
        public event EventHandler<JointsEventArgs>? JointsAdded;
        /// <summary>
        /// Raised when action point joints are updated.
        /// </summary>
        public event EventHandler<JointsEventArgs>? JointsUpdated;
        /// <summary>
        /// Raised when action point joints base is updated (e.g. rename).
        /// </summary>
        public event EventHandler<JointsEventArgs>? JointsBaseUpdated;
        /// <summary>
        /// Raised when action point joints are removed.
        /// </summary>
        public event EventHandler<JointsEventArgs>? JointsRemoved;

        /// <summary>
        /// Raised when new object type is added.
        /// </summary>
        /// <remarks>
        /// Be careful that this event doesn't represent an instance of object type (action object) being added/removed from a scene - for that see <see cref="ActionObjectAdded"/> and related events.
        /// This event is rather used for signaling dynamic changes to the object type database (such as is the case with virtual objects <see cref="AddVirtualCollisionObjectToSceneAsync"/>).
        /// </remarks>
        public event EventHandler<ObjectTypesEventArgs>? ObjectTypeAdded;
        /// <summary>
        /// Raised when new object type is updated.
        /// </summary>
        /// <remarks>
        /// Be careful that this event doesn't represent an instance of object type (action object) being added/removed from a scene - for that see <see cref="ActionObjectAdded"/> and related events.
        /// This event is rather used for signaling dynamic changes to the object type database (such as is the case with virtual objects <see cref="AddVirtualCollisionObjectToSceneAsync"/>).
        /// </remarks>
        public event EventHandler<ObjectTypesEventArgs>? ObjectTypeUpdated;
        /// <summary>
        /// Raised when new object type is removed.
        /// </summary>
        /// <remarks>
        /// Be careful that this event doesn't represent an instance of object type (action object) being added/removed from a scene - for that see <see cref="ActionObjectAdded"/> and related events.
        /// This event is rather used for signaling dynamic changes to the object type database (such as is the case with virtual objects <see cref="AddVirtualCollisionObjectToSceneAsync"/>).
        /// </remarks>
        public event EventHandler<ObjectTypesEventArgs>? ObjectTypeRemoved;

        /// <summary>
        /// Raised when robot moves to a pose (start/end).
        /// </summary>
        public event EventHandler<RobotMoveToPoseEventArgs>? RobotMoveToPose;
        /// <summary>
        /// Raised when robot moves to a joint (start/end).
        /// </summary>
        public event EventHandler<RobotMoveToJointsEventArgs>? RobotMoveToJoints;
        /// <summary>
        /// Raised when robot moves to action point orientation (start/end).
        /// </summary>
        public event EventHandler<RobotMoveToActionPointOrientationEventArgs>? RobotMoveToActionPointOrientation;
        /// <summary>
        /// Raised when robot moves to action point joints (start/end).
        /// </summary>
        public event EventHandler<RobotMoveToActionPointJointsEventArgs>? RobotMoveToActionPointJoints;
        /// <summary>
        /// Raised when hand teaching mode is enabled/disabled.
        /// </summary>
        public event EventHandler<HandTeachingModeEventArgs>? HandTeachingMode;

        /// <summary>
        /// Raised when new end effector poses.
        /// </summary>
        public event EventHandler<RobotEndEffectorUpdatedEventArgs>? RobotEndEffectorUpdated;
        /// <summary>
        /// Raised on new joints values.
        /// </summary>
        public event EventHandler<RobotJointsUpdatedEventArgs>? RobotJointsUpdated;

        /// <summary>
        /// Raised when project is saved by the server.
        /// </summary>
        public event EventHandler? ProjectSaved;
        /// <summary>
        /// Raised when server finds open project for the user, and it is requesting the client UI to open it (e.g. such as when the user quickly reconnects).
        /// </summary>
        public event EventHandler<OpenProjectEventArgs>? ProjectOpened;
        /// <summary>
        /// Raised when server closes a project, and it is requesting the client UI to close it.
        /// </summary>
        public event EventHandler? ProjectClosed;
        /// <summary>
        /// Raised when project base is updated (e.g. rename).
        /// </summary>
        public event EventHandler<BareProjectEventArgs>? ProjectBaseUpdated;
        /// <summary>
        /// Raised when project is removed.
        /// </summary>
        public event EventHandler<BareProjectEventArgs>? ProjectRemoved;

        /// <summary>
        /// Raised when project parameter is added.
        /// </summary>
        public event EventHandler<ProjectParameterEventArgs>? ProjectParameterAdded;
        /// <summary>
        /// Raised when project parameter is updated.
        /// </summary>
        public event EventHandler<ProjectParameterEventArgs>? ProjectParameterUpdated;
        /// <summary>
        /// Raised when project parameter is removed.
        /// </summary>
        public event EventHandler<ProjectParameterEventArgs>? ProjectParameterRemoved;

        /// <summary>
        /// Raised when scene is saved by the server.
        /// </summary>
        public event EventHandler? SceneSaved;
        /// <summary>
        /// Raised when server finds open scene for the user, and it is requesting the client UI to open it (e.g. such as when the user quickly reconnects).
        /// </summary>
        public event EventHandler<OpenSceneEventArgs>? SceneOpened;
        /// <summary>
        /// Raised when server closes a scene, and it is requesting the client UI to close it.
        /// </summary>
        public event EventHandler? SceneClosed;

        /// <summary>
        /// Raised when the server is requesting the client UI to show the main screen (e.g. after project/scene is closed).
        /// </summary>
        public event EventHandler<ShowMainScreenEventArgs>? ShowMainScreen;

        /// <summary>
        /// Raised when objects get locked by a user.
        /// </summary>
        public event EventHandler<ObjectsLockEventArgs>? ObjectsLocked;
        /// <summary>
        /// Raised when objects get unlocked.
        /// </summary>
        public event EventHandler<ObjectsLockEventArgs>? ObjectsUnlocked;

        /// <summary>
        /// Raised when server notifies beginning of the action execution triggered while editing a project.
        /// </summary>
        public event EventHandler<ActionExecutionEventArgs>? ActionExecution;
        /// <summary>
        /// Raised when server notifies that action execution was cancelled.
        /// </summary>
        public event EventHandler? ActionCancelled;
        /// <summary>
        /// Raised when server notifies the result of the action execution triggered while editing a project.
        /// </summary>
        public event EventHandler<ActionResultEventArgs>? ActionResult;
        /// <summary>
        /// Raised when the state of long-running process changes.
        /// </summary>
        public event EventHandler<ProcessStateEventArgs>? ProcessState;

        /// <summary>
        /// Raised when new package is added.
        /// </summary>
        public event EventHandler<PackageEventArgs>? PackageAdded;
        /// <summary>
        /// Raised when package is updated (e.g. renamed)
        /// </summary>
        public event EventHandler<PackageEventArgs>? PackageUpdated;
        /// <summary>
        /// Raised when package is removed.
        /// </summary>
        public event EventHandler<PackageEventArgs>? PackageRemoved;

        /// <summary>
        /// Raised when package is initialized and ready to execute.
        /// </summary>
        public event EventHandler<PackageInfoEventArgs>? PackageInfo;
        /// <summary>
        /// Raised when execution status of a package changes.
        /// </summary>
        public event EventHandler<PackageStateEventArgs>? PackageState;
        /// <summary>
        /// Raised when error occurs while running a package.
        /// </summary>
        public event EventHandler<PackageExceptionEventArgs>? PackageException;
        /// <summary>
        /// Raised while running a package before an execution of an action (parameters and other information).
        /// </summary>
        public event EventHandler<ActionStateBeforeEventArgs>? ActionStateBefore;
        /// <summary>
        /// Raised while running a package after n execution of an action (returned value and other information).
        /// </summary>
        public event EventHandler<ActionStateAfterEventArgs>? ActionStateAfter;

        #endregion

        /// <summary>
        /// Creates an instance of <see cref="Arcor2Client"/>.
        /// </summary>
        /// <param name="settings">The client settings.</param>
        /// <param name="logger">An instance of <see cref="IArcor2Logger"/>.</param>
        public Arcor2Client(Arcor2ClientSettings? settings = null, IArcor2Logger? logger = null) {
            webSocket = new SystemNetWebSocket();
            clientSettings = settings ?? new Arcor2ClientSettings();
            jsonSettings = clientSettings.ParseJsonSerializerSettings();
            this.logger = logger ?? null;

            webSocket.OnError += (_, args) => {
                ConnectionError?.Invoke(this, args.Exception);
                logger?.LogError($"A connection-related exception occured.\n{args}");
            };
            webSocket.OnClose += (_, args) => {
                ConnectionClosed?.Invoke(this, args);
                logger?.LogInfo("A connection with the ARCOR2 server was closed.");
            };
            webSocket.OnOpen += (_, __) => {
                ConnectionOpened?.Invoke(this, EventArgs.Empty);
                logger?.LogInfo("A connection with the ARCOR2 server was opened.");
            };
            webSocket.OnMessage += (_, args) => {
                OnMessage(args);
            };
        }

        /// <summary>
        /// Creates an instance of <see cref="Arcor2Client"/> with the provided websocket instance.
        /// </summary>
        /// <param name="websocket">A WebSocket object implementing the <see cref="IWebSocket"/> interface.</param>
        /// <param name="settings">The client settings.</param>
        /// <param name="logger">An instance of <see cref="IArcor2Logger"/>.</param>
        /// <exception cref="InvalidOperationException">If the provided WebSocket instance is not in the <see cref="WebSocketState.None"/> state.</exception>
        public Arcor2Client(IWebSocket websocket, Arcor2ClientSettings? settings = null, IArcor2Logger? logger = null) {
            if(websocket.State != WebSocketState.None) {
                throw new InvalidOperationException("The socket instance must be in the 'None' state.");
            }

            webSocket = websocket;
            clientSettings = settings ?? new Arcor2ClientSettings();
            jsonSettings = clientSettings.ParseJsonSerializerSettings();
            this.logger = logger ?? null;

            webSocket.OnError += (_, args) => {
                ConnectionError?.Invoke(this, args.Exception);
                logger?.LogError($"A connection-related exception occured.\n{args}");
            };
            webSocket.OnClose += (_, args) => {
                ConnectionClosed?.Invoke(this, args);
                logger?.LogInfo("A connection with the ARCOR2 server was closed.");
            };
            webSocket.OnOpen += (_, __) => {
                ConnectionOpened?.Invoke(this, EventArgs.Empty);
                logger?.LogInfo("A connection with the ARCOR2 server was opened.");
            };
            webSocket.OnMessage += (_, args) => {
                OnMessage(args);
            };
        }

        /// <summary>
        /// Resets the client state, allowing reconnection. Does not unregister event handlers or reset request ID.
        /// </summary>
        /// <remarks>
        /// This method exists for compatibility support with architectures registering events only once at startup. Using new instance is recommended.
        /// </remarks>
        public void Reset() {
            webSocket = new SystemNetWebSocket();
            Uri = null;
            pendingRequests = new ConcurrentDictionary<int, PendingRequest>();
            webSocket.OnError += (_, args) => {
                ConnectionError?.Invoke(this, args.Exception);
                logger?.LogError($"A connection-related exception occured.\n{args}");
            };
            webSocket.OnClose += (_, args) => {
                ConnectionClosed?.Invoke(this, args);
                logger?.LogInfo("A connection with the ARCOR2 server was closed.");
            };
            webSocket.OnOpen += (_, __) => {
                ConnectionOpened?.Invoke(this, EventArgs.Empty);
                logger?.LogInfo("A connection with the ARCOR2 server was opened.");
            };
            webSocket.OnMessage += (_, args) => {
                OnMessage(args);
            };
        }

        /// <summary>
        /// Resets the client state, allowing reconnection. Does not unregister event handlers or reset request ID.
        /// </summary>
        /// <param name="websocket">New WebSocket instance.</param>
        /// <remarks>
        /// This method exists for compatibility support with architectures registering events only once at startup. Using new instance is recommended.
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the provided WebSocket instance is not in the <see cref="WebSocketState.None"/> state.</exception>
        public void Reset(IWebSocket websocket) {
            if(websocket.State != WebSocketState.None) {
                throw new InvalidOperationException("The socket instance must be in the 'None' state.");
            }
            webSocket = websocket;
            Uri = null;
            pendingRequests = new ConcurrentDictionary<int, PendingRequest>();
            webSocket.OnError += (_, args) => {
                ConnectionError?.Invoke(this, args.Exception);
                logger?.LogError($"A connection-related exception occured.\n{args}");
            };
            webSocket.OnClose += (_, args) => {
                ConnectionClosed?.Invoke(this, args);
                logger?.LogInfo("A connection with the ARCOR2 server was closed.");
            };
            webSocket.OnOpen += (_, __) => {
                ConnectionOpened?.Invoke(this, EventArgs.Empty);
                logger?.LogInfo("A connection with the ARCOR2 server was opened.");
            };
            webSocket.OnMessage += (_, args) => {
                OnMessage(args);
            };
        }

        /// <summary>
        /// Gets the WebSocket used by the client.
        /// </summary>
        /// <returns>The WebSocket used by the client.</returns>
        public IWebSocket GetUnderlyingWebSocket() => webSocket;

        #region Connection Management Methods

        /// <summary>
        /// Establishes a connection to ARCOR2 server.
        /// </summary>
        /// <param name="domain">Domain of the ARCOR2 server</param>
        /// <param name="port">Port od the ARCOR2 server</param>
        /// <exception cref="UriFormatException" />
        /// <exception cref="InvalidOperationException" />
        public async Task ConnectAsync(string domain, ushort port) {
            await ConnectAsync(new Uri($"ws://{domain}:{port}"));
        }

        /// <summary>
        /// Establishes a connection to ARCOR2 server.
        /// </summary>
        /// <param name="uri">Full WebSocket URI</param>
        /// <exception cref="UriFormatException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="Arcor2ConnectionException"> When inner WebSocket fails to connect.</exception>
        public async Task ConnectAsync(Uri uri) {
            // Other parameters are implicitly validated by the parameter being Uri type.
            if(uri.Scheme != "ws" && uri.Scheme != "wss") {
                throw new UriFormatException("The URI scheme must be 'ws' or 'wss'.");
            }

            if(webSocket.State != WebSocketState.None) {
                throw new InvalidOperationException("ConnectAsync can not be invoked when connection is not opened.");
            }

            try {
                await webSocket.ConnectAsync(uri);
            }
            catch(WebSocketConnectionException e) {
                throw new Arcor2ConnectionException("WebSocket failed to connect.", e);
            }

            Uri = uri;
        }

        /// <summary>
        /// Closes a connection to ARCOR2 sever.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task CloseAsync() {
            if(webSocket.State != WebSocketState.Open) {
                throw new InvalidOperationException("CloseAsync can not be invoked when connection is not open.");
            }

            await webSocket.CloseAsync();
            Uri = null;
        }


        #endregion

        #region Send Methods

        /// <summary>
        /// Sends a request and waits for a response with the matching ID.
        /// </summary>
        /// <param name="message">Request object to be serialized and sent</param>
        /// <param name="id">ID of the request. Will increment and assign from <see cref="requestId"/> if null. </param>
        /// <param name="rpcSignature">Name of the RPC used by the server.</param>
        /// <returns>Response message</returns>
        /// <exception cref="InvalidOperationException">Thrown when connection is not open</exception>
        /// <exception cref="TimeoutException">Thrown when response is not received within timeout period</exception>
        private async Task<string> SendAndWaitAsync(string message, int id, string rpcSignature) {
            if(webSocket.State != WebSocketState.Open) {
                throw new InvalidOperationException("Cannot send message when connection is not open.");
            }

            var pendingRequest = new PendingRequest(clientSettings.RpcTimeout, rpcSignature);

            if(!pendingRequests.TryAdd(id, pendingRequest)) {
                throw new InvalidOperationException($"Request ID {id} already exists.");
            }

            try {
                await webSocket.SendAsync(message);
                logger?.LogInfo($"Sent a new ARCOR2 message:\n{message}");
                return await pendingRequest.TaskCompletionSource.Task;
            }
            finally {
                pendingRequests.TryRemove(id, out _);
                pendingRequest.CancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// Sends a request and waits for a response with the matching ID
        /// </summary>
        /// <typeparam name="T">The object type to be serialized</typeparam>
        /// <param name="message">Request object to be serialized and sent</param>
        /// <param name="id">ID of the request.</param>
        /// <param name="rpcSignature">Name of the RPC used by the server.</param>
        /// <returns>Response message</returns>
        /// <exception cref="InvalidOperationException">Thrown when connection is not open</exception>
        /// <exception cref="TimeoutException">Thrown when response is not received within timeout period</exception>
        private async Task<string> SendAndWaitAsync<T>(T message, int id, string rpcSignature) {
            return await SendAndWaitAsync(JsonConvert.SerializeObject(message), id, rpcSignature);
        }

        #endregion

        /// <summary>
        /// Invoked when message from server is received.
        /// Sets request-response (RPC) completion or delegates events.
        /// </summary>
        /// <param name="args"></param>
        private void OnMessage(WebSocketMessageEventArgs args) {
            try {
                var data = Encoding.Default.GetString(args.Data);
                logger?.LogInfo($"Received a new ARCOR2 message:\n{data}");

                var dispatch = JsonConvert.DeserializeAnonymousType(data, new {
                    id = (int?) default,
                    response = (string?) default,
                    @event = (string?) default,
                    request = (string?) default
                });

                if(dispatch == null ||
                    (dispatch.response == null && dispatch.request == null && dispatch.@event == null)) {
                    return;
                }

                // Handle responses
                if(dispatch.response != null && dispatch.id != null && dispatch.id != 0) {
                    if(pendingRequests.TryGetValue(dispatch.id.Value, out PendingRequest? pendingRequest)) {
                        if(clientSettings.ValidateRpcResponseName && dispatch.response != pendingRequest.Signature) {
                            logger?.LogWarn(
                                $"An ARCOR2 RPC response with matching ID was received, but the RPC name does not correspond to the request. Expected \"{pendingRequest.Signature}\", got \"{dispatch.response}\".");
                            pendingRequest.TaskCompletionSource.TrySetException(
                                new Arcor2ConnectionException(
                                    $"A response with matching ID was received, but the RPC name does not correspond to the request. Expected \"{pendingRequest.Signature}\", got \"{dispatch.response}\"."));
                        }
                        else {
                            pendingRequest.TaskCompletionSource.TrySetResult(data);
                        }
                    }
                }
                else if(dispatch.@event != null) {
                    switch(dispatch.@event) {
                        case "SceneChanged":
                            HandleSceneChanged(data);
                            break;
                        case "SceneState":
                            HandleSceneState(data);
                            break;
                        case "SceneObjectChanged":
                            HandleSceneObjectChanged(data);
                            break;
                        case "ActionPointChanged":
                            HandleActionPointChanged(data);
                            break;
                        case "OverrideUpdated":
                            HandleOverrideUpdated(data);
                            break;
                        case "ActionChanged":
                            HandleActionChanged(data);
                            break;
                        case "LogicItemChanged":
                            HandleLogicItemChanged(data);
                            break;
                        case "OrientationChanged":
                            HandleOrientationChanged(data);
                            break;
                        case "JointsChanged":
                            HandleJointsChanged(data);
                            break;
                        case "ChangedObjectTypes":
                            HandleChangedObjectTypes(data);
                            break;
                        case "RobotMoveToActionPointOrientation":
                            HandleRobotMoveToActionPointOrientation(data);
                            break;
                        case "RobotMoveToPose":
                            HandleRobotMoveToPose(data);
                            break;
                        case "RobotMoveToJoints":
                            HandleRobotMoveToJoints(data);
                            break;
                        case "RobotMoveToActionPointJoints":
                            HandleRobotMoveToActionPointJoints(data);
                            break;
                        case "ActionStateBefore":
                            HandleActionStateBefore(data);
                            break;
                        case "ActionStateAfter":
                            HandleActionStateAfter(data);
                            break;
                        case "PackageState":
                            HandlePackageState(data);
                            break;
                        case "PackageInfo":
                            HandlePackageInfo(data);
                            break;
                        case "ProjectSaved":
                            HandleProjectSaved(data);
                            break;
                        case "SceneSaved":
                            HandleSceneSaved(data);
                            break;
                        case "ProjectException":
                            HandleProjectException(data);
                            break;
                        case "ActionResult":
                            HandleActionResult(data);
                            break;
                        case "ActionCancelled":
                            HandleActionCancelled(data);
                            break;
                        case "ActionExecution":
                            HandleActionExecution(data);
                            break;
                        case "RobotEef":
                            HandleRobotEef(data);
                            break;
                        case "RobotJoints":
                            HandleRobotJoints(data);
                            break;
                        case "OpenScene":
                            HandleOpenScene(data);
                            break;
                        case "OpenProject":
                            HandleOpenProject(data);
                            break;
                        case "SceneClosed":
                            HandleSceneClosed(data);
                            break;
                        case "ProjectClosed":
                            HandleProjectClosed(data);
                            break;
                        case "ProjectChanged":
                            HandleProjectChanged(data);
                            break;
                        case "ShowMainScreen":
                            HandleShowMainScreen(data);
                            break;
                        case "ObjectsLocked":
                            HandleObjectLocked(data);
                            break;
                        case "ObjectsUnlocked":
                            HandleObjectUnlocked(data);
                            break;
                        case "ProcessState":
                            HandleProcessState(data);
                            break;
                        case "ProjectParameterChanged":
                            HandleProjectParameterChanged(data);
                            break;
                        case "HandTeachingMode":
                            HandleHandTeachingMode(data);
                            break;
                        case "PackageChanged":
                            HandlePackageChanged(data);
                            break;
                        default:
                            logger?.LogWarn($"Unknown ARCOR2 \"{dispatch.@event}\" event received.");
                            break;
                    }
                }
            }
            catch(JsonException ex) {
                logger?.LogError($"A JSON Exception occured while deserializing received ARCOR2 message.\n{ex}");
            }
            catch(Exception ex) {
                // Catch everything else, as we do not want the exception to bubble up to the WebSocket dispatcher
                // and close the connection. This is to increase compatibility between different ARCOR2 server
                // and client versions.
                logger?.LogError($"An Exception occured while decoding a received ARCOR2 message.\n{ex}");
            }
        }

        #region Event Handlers

        private void HandleSceneChanged(string data) {
            var sceneChangedEvent = JsonConvert.DeserializeObject<SceneChanged>(data, jsonSettings)!;
            switch(sceneChangedEvent.ChangeType) {
                case SceneChanged.ChangeTypeEnum.Add:
                    throw new NotImplementedException("SceneChanged add should never occur.");
                case SceneChanged.ChangeTypeEnum.Remove:
                    SceneRemoved?.Invoke(this, new BareSceneEventArgs(sceneChangedEvent.Data));
                    break;
                case SceneChanged.ChangeTypeEnum.Update:
                    throw new NotImplementedException("SceneChanged update should never occur.");
                case SceneChanged.ChangeTypeEnum.UpdateBase:
                    SceneBaseUpdated?.Invoke(this, new BareSceneEventArgs(sceneChangedEvent.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type for 'SceneChanged' event.");
            }
        }

        private void HandleSceneState(string data) {
            var sceneState = JsonConvert.DeserializeObject<SceneState>(data, jsonSettings)!;
            SceneState?.Invoke(this, new SceneStateEventArgs(sceneState.Data));
        }

        private void HandleSceneObjectChanged(string data) {
            var sceneObjectChanged = JsonConvert.DeserializeObject<SceneObjectChanged>(data, jsonSettings)!;
            switch(sceneObjectChanged.ChangeType) {
                case SceneObjectChanged.ChangeTypeEnum.Add:
                    ActionObjectAdded?.Invoke(this, new ActionObjectEventArgs(sceneObjectChanged.Data));
                    break;
                case SceneObjectChanged.ChangeTypeEnum.Remove:
                    ActionObjectRemoved?.Invoke(this, new ActionObjectEventArgs(sceneObjectChanged.Data));
                    break;
                case SceneObjectChanged.ChangeTypeEnum.Update:
                    ActionObjectUpdated?.Invoke(this, new ActionObjectEventArgs(sceneObjectChanged.Data));
                    break;
                case SceneObjectChanged.ChangeTypeEnum.UpdateBase:
                    throw new NotImplementedException("SceneObjectChanged base update should never occur.");
                default:
                    throw new NotImplementedException("Unknown change type for 'SceneObjectChanged' event.");
            }
        }

        private void HandleActionPointChanged(string data) {
            var actionPointChanged = JsonConvert.DeserializeObject<ActionPointChanged>(data, jsonSettings)!;

            switch(actionPointChanged.ChangeType) {
                case ActionPointChanged.ChangeTypeEnum.Add:
                    ActionPointAdded?.Invoke(this, new BareActionPointEventArgs(actionPointChanged.Data));
                    break;
                case ActionPointChanged.ChangeTypeEnum.Remove:
                    ActionPointRemoved?.Invoke(this, new BareActionPointEventArgs(actionPointChanged.Data));
                    break;
                case ActionPointChanged.ChangeTypeEnum.Update:
                    ActionPointUpdated?.Invoke(this, new BareActionPointEventArgs(actionPointChanged.Data));
                    break;
                case ActionPointChanged.ChangeTypeEnum.UpdateBase:
                    ActionPointBaseUpdated?.Invoke(this, new BareActionPointEventArgs(actionPointChanged.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type for 'ActionPointChanged' event.");
            }
        }

        private void HandleOverrideUpdated(string data) {
            var overrideUpdated = JsonConvert.DeserializeObject<OverrideUpdated>(data, jsonSettings)!;

            switch(overrideUpdated.ChangeType) {
                case OverrideUpdated.ChangeTypeEnum.Add:
                    ProjectOverrideAdded?.Invoke(this, new ParameterEventArgs(overrideUpdated.Data, overrideUpdated.ParentId));
                    break;
                case OverrideUpdated.ChangeTypeEnum.Remove:
                    ProjectOverrideRemoved?.Invoke(this, new ParameterEventArgs(overrideUpdated.Data, overrideUpdated.ParentId));
                    break;
                case OverrideUpdated.ChangeTypeEnum.Update:
                    ProjectOverrideUpdated?.Invoke(this, new ParameterEventArgs(overrideUpdated.Data, overrideUpdated.ParentId));
                    break;
                case OverrideUpdated.ChangeTypeEnum.UpdateBase:
                    throw new NotImplementedException("OverrideUpdated base update should never occur.");
                default:
                    throw new NotImplementedException("Unknown change type for 'OverrideUpdated' event.");
            }
        }

        private void HandleActionChanged(string data) {
            var actionChanged = JsonConvert.DeserializeObject<ActionChanged>(data, jsonSettings)!;

            // The OpenApi generator thinks all the events use BareAction, which is not true.
            // So we have to use this small hack.
            switch(actionChanged.ChangeType) {
                case ActionChanged.ChangeTypeEnum.Add:
                    var fullActionJson = JObject.Parse(data)["data"]!.ToString();
                    var fullAction = JsonConvert.DeserializeObject<Action>(fullActionJson, jsonSettings);
                    ActionAdded?.Invoke(this, new ActionEventArgs(fullAction!, actionChanged.ParentId));
                    break;
                case ActionChanged.ChangeTypeEnum.Remove:
                    ActionRemoved?.Invoke(this, new BareActionEventArgs(actionChanged.Data));
                    break;
                case ActionChanged.ChangeTypeEnum.Update:
                    var fullActionJson2 = JObject.Parse(data)["data"]!.ToString();
                    var fullAction2 = JsonConvert.DeserializeObject<Action>(fullActionJson2, jsonSettings);
                    ActionUpdated?.Invoke(this, new ActionEventArgs(fullAction2!, actionChanged.ParentId));
                    break;
                case ActionChanged.ChangeTypeEnum.UpdateBase:
                    var fullActionJson3 = JObject.Parse(data)["data"]!.ToString();
                    var fullAction3 = JsonConvert.DeserializeObject<Action>(fullActionJson3, jsonSettings);
                    ActionBaseUpdated?.Invoke(this, new ActionEventArgs(fullAction3!, actionChanged.ParentId));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type for 'ActionChanged' event.");
            }
        }

        private void HandleLogicItemChanged(string data) {
            var logicItemChanged = JsonConvert.DeserializeObject<LogicItemChanged>(data, jsonSettings)!;

            switch(logicItemChanged.ChangeType) {
                case LogicItemChanged.ChangeTypeEnum.Add:
                    LogicItemAdded?.Invoke(this, new LogicItemEventArgs(logicItemChanged.Data));
                    break;
                case LogicItemChanged.ChangeTypeEnum.Remove:
                    LogicItemRemoved?.Invoke(this, new LogicItemEventArgs(logicItemChanged.Data));
                    break;
                case LogicItemChanged.ChangeTypeEnum.Update:
                    LogicItemUpdated?.Invoke(this, new LogicItemEventArgs(logicItemChanged.Data));
                    break;
                case LogicItemChanged.ChangeTypeEnum.UpdateBase:
                    throw new NotImplementedException("LogicItemChanged base update should never occur.");
                default:
                    throw new NotImplementedException("Unknown change type for 'LogicItemChanged' event.");
            }
        }

        private void HandleOrientationChanged(string data) {
            var orientationChanged = JsonConvert.DeserializeObject<OrientationChanged>(data, jsonSettings)!;

            switch(orientationChanged.ChangeType) {
                case OrientationChanged.ChangeTypeEnum.Add:
                    OrientationAdded?.Invoke(this, new OrientationEventArgs(orientationChanged.Data, orientationChanged.ParentId));
                    break;
                case OrientationChanged.ChangeTypeEnum.Remove:
                    OrientationRemoved?.Invoke(this, new OrientationEventArgs(orientationChanged.Data));
                    break;
                case OrientationChanged.ChangeTypeEnum.Update:
                    OrientationUpdated?.Invoke(this, new OrientationEventArgs(orientationChanged.Data));
                    break;
                case OrientationChanged.ChangeTypeEnum.UpdateBase:
                    OrientationBaseUpdated?.Invoke(this, new OrientationEventArgs(orientationChanged.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type for 'OrientationChanged' event.");
            }
        }

        private void HandleJointsChanged(string data) {
            var jointsChanged = JsonConvert.DeserializeObject<JointsChanged>(data, jsonSettings)!;

            switch(jointsChanged.ChangeType) {
                case JointsChanged.ChangeTypeEnum.Add:
                    JointsAdded?.Invoke(this, new JointsEventArgs(jointsChanged.Data, jointsChanged.ParentId));
                    break;
                case JointsChanged.ChangeTypeEnum.Remove:
                    JointsRemoved?.Invoke(this, new JointsEventArgs(jointsChanged.Data));
                    break;
                case JointsChanged.ChangeTypeEnum.Update:
                    JointsUpdated?.Invoke(this, new JointsEventArgs(jointsChanged.Data));
                    break;
                case JointsChanged.ChangeTypeEnum.UpdateBase:
                    JointsBaseUpdated?.Invoke(this, new JointsEventArgs(jointsChanged.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type for 'JointsChanged' event.");
            }
        }

        private void HandleChangedObjectTypes(string data) {
            var objectTypesChangedEvent = JsonConvert.DeserializeObject<ChangedObjectTypes>(data, jsonSettings)!;

            switch(objectTypesChangedEvent.ChangeType) {
                case ChangedObjectTypes.ChangeTypeEnum.Add:
                    ObjectTypeAdded?.Invoke(this, new ObjectTypesEventArgs(objectTypesChangedEvent.Data));
                    break;
                case ChangedObjectTypes.ChangeTypeEnum.Remove:
                    ObjectTypeRemoved?.Invoke(this, new ObjectTypesEventArgs(objectTypesChangedEvent.Data));
                    break;
                case ChangedObjectTypes.ChangeTypeEnum.Update:
                    ObjectTypeUpdated?.Invoke(this, new ObjectTypesEventArgs(objectTypesChangedEvent.Data));
                    break;
                case ChangedObjectTypes.ChangeTypeEnum.UpdateBase:
                    throw new NotImplementedException("ChangedObjectTypes base update should never occur.");
                default:
                    throw new NotImplementedException("Unknown change type for 'ChangedObjectTypes' event.");
            }
        }

        private void HandleRobotMoveToActionPointOrientation(string data) {
            var robotMoveToActionPointOrientation = JsonConvert.DeserializeObject<RobotMoveToActionPointOrientation>(data, jsonSettings)!;
            RobotMoveToActionPointOrientation?.Invoke(this, new RobotMoveToActionPointOrientationEventArgs(robotMoveToActionPointOrientation.Data));
        }

        private void HandleRobotMoveToPose(string data) {
            var robotMoveToPose = JsonConvert.DeserializeObject<RobotMoveToPose>(data, jsonSettings)!;
            RobotMoveToPose?.Invoke(this, new RobotMoveToPoseEventArgs(robotMoveToPose.Data));
        }

        private void HandleRobotMoveToJoints(string data) {
            var robotMoveToJoints = JsonConvert.DeserializeObject<RobotMoveToJoints>(data, jsonSettings)!;
            RobotMoveToJoints?.Invoke(this, new RobotMoveToJointsEventArgs(robotMoveToJoints.Data));
        }

        private void HandleRobotMoveToActionPointJoints(string data) {
            var robotMoveToActionPointJoints = JsonConvert.DeserializeObject<RobotMoveToActionPointJoints>(data, jsonSettings)!;
            RobotMoveToActionPointJoints?.Invoke(this, new RobotMoveToActionPointJointsEventArgs(robotMoveToActionPointJoints.Data));
        }

        private void HandleActionStateBefore(string data) {
            var actionStateBefore = JsonConvert.DeserializeObject<ActionStateBefore>(data, jsonSettings)!;
            ActionStateBefore?.Invoke(this, new ActionStateBeforeEventArgs(actionStateBefore.Data));
        }

        private void HandleActionStateAfter(string data) {
            var actionStateAfter = JsonConvert.DeserializeObject<ActionStateAfter>(data, jsonSettings)!;
            ActionStateAfter?.Invoke(this, new ActionStateAfterEventArgs(actionStateAfter.Data));
        }

        private void HandlePackageState(string data) {
            var projectState = JsonConvert.DeserializeObject<PackageState>(data, jsonSettings)!;
            PackageState?.Invoke(this, new PackageStateEventArgs(projectState.Data));
        }

        private void HandlePackageInfo(string data) {
            var packageInfo = JsonConvert.DeserializeObject<PackageInfo>(data, jsonSettings)!;
            PackageInfo?.Invoke(this, new PackageInfoEventArgs(packageInfo.Data));
        }

        private void HandleProjectSaved(string data) {
            // Not needed, but may throw depending on the set strictness
            var projectSaved = JsonConvert.DeserializeObject<ProjectSaved>(data, jsonSettings)!;
            ProjectSaved?.Invoke(this, EventArgs.Empty);
        }

        private void HandleSceneSaved(string data) {
            // Not needed, but may throw depending on the set strictness
            var sceneSaved = JsonConvert.DeserializeObject<SceneSaved>(data, jsonSettings)!;
            SceneSaved?.Invoke(this, EventArgs.Empty);
        }

        private void HandleProjectException(string data) {
            var projectException = JsonConvert.DeserializeObject<ProjectException>(data, jsonSettings)!;
            PackageException?.Invoke(this, new PackageExceptionEventArgs(projectException.Data));
        }

        private void HandleActionResult(string data) {
            var actionResult = JsonConvert.DeserializeObject<ActionResult>(data, jsonSettings)!;
            ActionResult?.Invoke(this, new ActionResultEventArgs(actionResult.Data));
        }

        private void HandleActionCancelled(string data) {
            // Not needed, but may throw depending on the set strictness
            var actionCancelled = JsonConvert.DeserializeObject<ActionCancelled>(data, jsonSettings)!;
            ActionCancelled?.Invoke(this, EventArgs.Empty);
        }

        private void HandleActionExecution(string data) {
            var actionExecution = JsonConvert.DeserializeObject<ActionExecution>(data, jsonSettings)!;
            ActionExecution?.Invoke(this, new ActionExecutionEventArgs(actionExecution.Data));
        }

        private void HandleRobotEef(string data) {
            var robotEef = JsonConvert.DeserializeObject<RobotEef>(data, jsonSettings)!;
            RobotEndEffectorUpdated?.Invoke(this, new RobotEndEffectorUpdatedEventArgs(robotEef.Data));
        }

        private void HandleRobotJoints(string data) {
            var robotJoints = JsonConvert.DeserializeObject<RobotJoints>(data, jsonSettings)!;
            RobotJointsUpdated?.Invoke(this, new RobotJointsUpdatedEventArgs(robotJoints.Data));
        }

        private void HandleOpenProject(string data) {
            var openProject = JsonConvert.DeserializeObject<OpenProject>(data, jsonSettings)!;
            ProjectOpened?.Invoke(this, new OpenProjectEventArgs(openProject.Data));
        }

        private void HandleOpenScene(string data) {
            var openScene = JsonConvert.DeserializeObject<OpenScene>(data, jsonSettings)!;
            SceneOpened?.Invoke(this, new OpenSceneEventArgs(openScene.Data));
        }

        private void HandleProjectClosed(string data) {
            // Not needed, but may throw depending on the set strictness
            var projectClosed = JsonConvert.DeserializeObject<ProjectClosed>(data, jsonSettings)!;
            ProjectClosed?.Invoke(this, EventArgs.Empty);
        }

        private void HandleSceneClosed(string data) {
            // Not needed, but may throw depending on the set strictness
            var sceneClosed = JsonConvert.DeserializeObject<SceneClosed>(data, jsonSettings)!;
            SceneClosed?.Invoke(this, EventArgs.Empty);
        }

        private void HandleProjectChanged(string data) {
            var projectChanged = JsonConvert.DeserializeObject<ProjectChanged>(data, jsonSettings)!;

            switch(projectChanged.ChangeType) {
                case ProjectChanged.ChangeTypeEnum.Add:
                    throw new NotImplementedException("ProjectChanged add should never occur!");
                case ProjectChanged.ChangeTypeEnum.Remove:
                    ProjectRemoved?.Invoke(this, new BareProjectEventArgs(projectChanged.Data));
                    break;
                case ProjectChanged.ChangeTypeEnum.Update:
                    throw new NotImplementedException("ProjectChanged update should never occur!");
                case ProjectChanged.ChangeTypeEnum.UpdateBase:
                    ProjectBaseUpdated?.Invoke(this, new BareProjectEventArgs(projectChanged.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type for 'ProjectChanged' event.");
            }
        }

        private void HandleShowMainScreen(string data) {
            var showMainScreen = JsonConvert.DeserializeObject<ShowMainScreen>(data, jsonSettings)!;
            ShowMainScreen?.Invoke(this, new ShowMainScreenEventArgs(showMainScreen.Data));
        }

        private void HandleProjectParameterChanged(string data) {
            var projectParameterChanged = JsonConvert.DeserializeObject<ProjectParameterChanged>(data, jsonSettings)!;

            switch(projectParameterChanged.ChangeType) {
                case ProjectParameterChanged.ChangeTypeEnum.Add:
                    ProjectParameterAdded?.Invoke(this, new ProjectParameterEventArgs(projectParameterChanged.Data));
                    break;
                case ProjectParameterChanged.ChangeTypeEnum.Update:
                    ProjectParameterUpdated?.Invoke(this, new ProjectParameterEventArgs(projectParameterChanged.Data));
                    break;
                case ProjectParameterChanged.ChangeTypeEnum.Remove:
                    ProjectParameterRemoved?.Invoke(this, new ProjectParameterEventArgs(projectParameterChanged.Data));
                    break;
                case ProjectParameterChanged.ChangeTypeEnum.UpdateBase:
                    ProjectParameterUpdated?.Invoke(this, new ProjectParameterEventArgs(projectParameterChanged.Data));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void HandleObjectUnlocked(string data) {
            var objectsUnlocked = JsonConvert.DeserializeObject<ObjectsUnlocked>(data, jsonSettings)!;
            ObjectsUnlocked?.Invoke(this, new ObjectsLockEventArgs(objectsUnlocked.Data));
        }

        private void HandleObjectLocked(string data) {
            var objectsLocked = JsonConvert.DeserializeObject<ObjectsLocked>(data, jsonSettings)!;
            ObjectsLocked?.Invoke(this, new ObjectsLockEventArgs(objectsLocked.Data));
        }

        private void HandleProcessState(string data) {
            var processState = JsonConvert.DeserializeObject<ProcessState>(data, jsonSettings)!;
            ProcessState?.Invoke(this, new ProcessStateEventArgs(processState.Data));
        }

        private void HandleHandTeachingMode(string data) {
            var handTeachingMode = JsonConvert.DeserializeObject<HandTeachingMode>(data, jsonSettings)!;
            HandTeachingMode?.Invoke(this, new HandTeachingModeEventArgs(handTeachingMode.Data));
        }

        private void HandlePackageChanged(string data) {
            var packageChanged = JsonConvert.DeserializeObject<PackageChanged>(data, jsonSettings)!;

            switch(packageChanged.ChangeType) {
                case PackageChanged.ChangeTypeEnum.Add:
                    PackageAdded?.Invoke(this, new PackageEventArgs(packageChanged.Data));
                    break;
                case PackageChanged.ChangeTypeEnum.Update:
                    PackageUpdated?.Invoke(this, new PackageEventArgs(packageChanged.Data));
                    break;
                case PackageChanged.ChangeTypeEnum.Remove:
                    PackageRemoved?.Invoke(this, new PackageEventArgs(packageChanged.Data));
                    break;
                case PackageChanged.ChangeTypeEnum.UpdateBase:
                    throw new NotImplementedException("PackageChanged base update should never occur.");
                default:
                    throw new NotImplementedException("Unknown change type for 'PackageChanged' event.");
            }
        }

        #endregion

        #region Request-Response Methods

        #region Request-Response Methods Without Coressponding Endpoint

        /// <summary>
        /// Sends a request to remove specified object type.
        /// </summary>
        /// <remarks>For bulk removal, use <see cref="RemoveObjectTypesAsync" />.</remarks>
        /// <param name="args">Object Type.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>

        public async Task<DeleteObjectTypesResponse> RemoveObjectTypeAsync(string args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteObjectTypesRequest(id, "DeleteObjectTypes", new List<string>() { args }, isDryRun), id, "DeleteObjectTypes");
            return JsonConvert.DeserializeObject<DeleteObjectTypesResponse>(response, jsonSettings)!;
        }

        #endregion

        #region Request-Response Methods With Direct Endpoint

        /// <summary>
        /// Sends a request to remove specified object types.
        /// </summary>
        /// <param name="args">A list of object types.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<DeleteObjectTypesResponse> RemoveObjectTypesAsync(List<string> args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteObjectTypesRequest(id, "DeleteObjectTypes", args, isDryRun), id, "DeleteObjectTypes");
            return JsonConvert.DeserializeObject<DeleteObjectTypesResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to retrieve object types supported by the server.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetObjectTypesResponse> GetObjectTypesAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetObjectTypesRequest(id, "GetObjectTypes"), id, "GetObjectTypes");
            return JsonConvert.DeserializeObject<GetObjectTypesResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to retrieve list of available actions for an object type.
        /// </summary>
        /// <param name="args">The object type.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetActionsResponse> GetActionsAsync(TypeArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetActionsRequest(id, "GetActions", args), id, "GetActions");
            return JsonConvert.DeserializeObject<GetActionsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to save the current scene.
        /// </summary>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<SaveSceneResponse> SaveSceneAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SaveSceneRequest(id, "SaveScene", isDryRun), id, "SaveScene");
            return JsonConvert.DeserializeObject<SaveSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to save the current project.
        /// </summary>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<SaveProjectResponse> SaveProjectAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SaveProjectRequest(id, "SaveProject", isDryRun), id, "SaveProject");
            return JsonConvert.DeserializeObject<SaveProjectResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to open a project.
        /// </summary>
        /// <param name="args">The project ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<OpenProjectResponse> OpenProjectAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new OpenProjectRequest(id, "OpenProject", args), id, "OpenProject");
            return JsonConvert.DeserializeObject<OpenProjectResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to run a package.
        /// </summary>
        /// <param name="args">The run parameters.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RunPackageResponse> RunPackageAsync(RunPackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RunPackageRequest(id, "RunPackage", args), id, "RunPackage");
            return JsonConvert.DeserializeObject<RunPackageResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to build the current project into temporary package and run it.
        /// This package is not saved on execution unit and is removed immediately after package execution.
        /// </summary>
        /// <param name="args">The debugging execution parameters.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<TemporaryPackageResponse> RunTemporaryPackageAsync(TemporaryPackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new TemporaryPackageRequest(id, "TemporaryPackage", args), id, "TemporaryPackage");
            return JsonConvert.DeserializeObject<TemporaryPackageResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to terminate a running package.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<StopPackageResponse> StopPackageAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StopPackageRequest(id, "StopPackage"), id, "StopPackage");
            return JsonConvert.DeserializeObject<StopPackageResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to pause a running package.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<PausePackageResponse> PausePackageAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new PausePackageRequest(id, "PausePackage"), id, "PausePackage");
            return JsonConvert.DeserializeObject<PausePackageResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to resume a pause package.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ResumePackageResponse> ResumePackageAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ResumePackageRequest(id, "ResumePackage"), id, "ResumePackage");
            return JsonConvert.DeserializeObject<ResumePackageResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to upload a package.
        /// </summary>
        /// <param name="args">The package ID and its data.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UploadPackageResponse> UploadPackageAsync(UploadPackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UploadPackageRequest(id, "UploadPackage", args), id, "UploadPackage");
            return JsonConvert.DeserializeObject<UploadPackageResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to rename a package.
        /// </summary>
        /// <param name="args">The package ID and new name.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RenamePackageResponse> RenamePackageAsync(RenamePackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenamePackageRequest(id, "RenamePackage", args), id, "RenamePackage");
            return JsonConvert.DeserializeObject<RenamePackageResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to retrieve a list of available packages.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ListPackagesResponse> ListPackagesAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListPackagesRequest(id, "ListPackages"), id, "ListPackages");
            return JsonConvert.DeserializeObject<ListPackagesResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update pose of action point to the robot's end effector.
        /// </summary>
        /// <param name="args">Action point ID and a robot.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateActionPointUsingRobotResponse> UpdateActionPointUsingRobotAsync(UpdateActionPointUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointUsingRobotRequest(id, "UpdateActionPointUsingRobot", args), id, "UpdateActionPointUsingRobot");
            return JsonConvert.DeserializeObject<UpdateActionPointUsingRobotResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update pose (position and orientation) of an action object.
        /// </summary>
        /// <param name="args">Action object ID and a new pose.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateObjectPoseResponse> UpdateActionObjectPoseAsync(UpdateObjectPoseRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectPoseRequest(id, "UpdateObjectPose", args, isDryRun), id, "UpdateObjectPose");
            return JsonConvert.DeserializeObject<UpdateObjectPoseResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update pose of action object to the robot's end effector.
        /// </summary>
        /// <param name="args">Robot and pivot option.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateObjectPoseUsingRobotResponse> UpdateObjectPoseUsingRobotAsync(UpdateObjectPoseUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectPoseUsingRobotRequest(id, "UpdateObjectPoseUsingRobot", args), id, "UpdateObjectPoseUsingRobot");
            return JsonConvert.DeserializeObject<UpdateObjectPoseUsingRobotResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to define a new object type.
        /// </summary>
        /// <param name="args">The object type definition.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<NewObjectTypeResponse> AddNewObjectTypeAsync(ObjectTypeMeta args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new NewObjectTypeRequest(id, "NewObjectType", args, isDryRun), id, "NewObjectType");
            return JsonConvert.DeserializeObject<NewObjectTypeResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to start an object aiming process for a robot.
        /// </summary>
        /// <remarks>Only possible when the scene is online and all write locks for object and robot are acquired in advance.</remarks>
        /// <param name="args">Action object ID and a robot.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ObjectAimingStartResponse> ObjectAimingStartAsync(ObjectAimingStartRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingStartRequest(id, "ObjectAimingStart", args, isDryRun), id, "ObjectAimingStart");
            return JsonConvert.DeserializeObject<ObjectAimingStartResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to save current position of selected robot's end effector during the object aiming process as the specified index.
        /// </summary>
        /// <param name="args">ID of currently selected focus point.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ObjectAimingAddPointResponse> ObjectAimingAddPointAsync(ObjectAimingAddPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingAddPointRequest(id, "ObjectAimingAddPoint", args, isDryRun), id, "ObjectAimingAddPoint");
            return JsonConvert.DeserializeObject<ObjectAimingAddPointResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to finish object aiming process and compute a new pose of the object.
        /// </summary>
        /// <remarks>On failure, you can do another attempt or invoke <see cref="ObjectAimingCancelAsync"/>.</remarks>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ObjectAimingDoneResponse> ObjectAimingDoneAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingDoneRequest(id, "ObjectAimingDone", isDryRun), id, "ObjectAimingDone");
            return JsonConvert.DeserializeObject<ObjectAimingDoneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to cancel current object aiming process.
        /// </summary>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ObjectAimingCancelResponse> ObjectAimingCancelAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingCancelRequest(id, "ObjectAimingCancel", isDryRun), id, "ObjectAimingCancel");
            return JsonConvert.DeserializeObject<ObjectAimingCancelResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to retrieve a list of available scenes.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ListScenesResponse> ListScenesAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListScenesRequest(id, "ListScenes"), id, "ListScenes");
            return JsonConvert.DeserializeObject<ListScenesResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to retrieve a list of available projects.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ListProjectsResponse> ListProjectsAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListProjectsRequest(id, "ListProjects"), id, "ListProjects");
            return JsonConvert.DeserializeObject<ListProjectsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to add a new action object to a scene.
        /// </summary>
        /// <param name="args">The name, type, pose and parameters of the action object.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<AddObjectToSceneResponse> AddActionObjectToSceneAsync(AddObjectToSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddObjectToSceneRequest(id, "AddObjectToScene", args, isDryRun), id, "AddObjectToScene");
            return JsonConvert.DeserializeObject<AddObjectToSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to remove an action object from scene.
        /// </summary>
        /// <param name="args">Action Object ID and if the removal should be forced.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RemoveFromSceneResponse> RemoveActionObjectFromSceneAsync(RemoveFromSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveFromSceneRequest(id, "RemoveFromScene", args, isDryRun), id, "RemoveFromScene");
            return JsonConvert.DeserializeObject<RemoveFromSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to open a scene.
        /// </summary>
        /// <param name="args">Scene ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<OpenSceneResponse> OpenSceneAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new OpenSceneRequest(id, "OpenScene", args), id, "OpenScene");
            return JsonConvert.DeserializeObject<OpenSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get all available values for selected parameter.
        /// </summary>
        /// <param name="args">Action object ID, parameter ID, and a list of parent parameters. </param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ActionParamValuesResponse> GetActionParameterValuesAsync(ActionParamValuesRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ActionParamValuesRequest(id, "ActionParamValues", args), id, "ActionParamValues");
            return JsonConvert.DeserializeObject<ActionParamValuesResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to execute selected action.
        /// </summary>
        /// <param name="args">Action ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ExecuteActionResponse> ExecuteActionAsync(ExecuteActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ExecuteActionRequest(id, "ExecuteAction", args, isDryRun), id, "ExecuteAction");
            return JsonConvert.DeserializeObject<ExecuteActionResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to cancel an execution of currently running action.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<CancelActionResponse> CancelActionAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CancelActionRequest(id, "CancelAction"), id, "CancelAction");
            return JsonConvert.DeserializeObject<CancelActionResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to retrieve information about the server (server version, API version, supported parameter types and RPCs).
        /// </summary>
        /// <returns>THe response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<SystemInfoResponse> GetSystemInfoAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SystemInfoRequest(id, "SystemInfo"), id, "SystemInfo");
            return JsonConvert.DeserializeObject<SystemInfoResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to build a project into a package.
        /// </summary>
        /// <param name="args">The project ID and resulting package name.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<BuildProjectResponse> BuildProjectAsync(BuildProjectRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new BuildProjectRequest(id, "BuildProject", args), id, "BuildProject");
            return JsonConvert.DeserializeObject<BuildProjectResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to create a new scene.
        /// </summary>
        /// <param name="args">Name and description.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<NewSceneResponse> AddNewSceneAsync(NewSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new NewSceneRequest(id, "NewScene", args, isDryRun), id, "NewScene");
            return JsonConvert.DeserializeObject<NewSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to remove a scene.
        /// </summary>
        /// <param name="args">ID of the scene.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<DeleteSceneResponse> RemoveSceneAsync(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteSceneRequest(id, "DeleteScene", args, isDryRun), id, "DeleteScene");
            return JsonConvert.DeserializeObject<DeleteSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to rename a scene.
        /// </summary>
        /// <param name="args">ID and a new name of the scene.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RenameSceneResponse> RenameSceneAsync(RenameArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameSceneRequest(id, "RenameScene", args, isDryRun), id, "RenameScene");
            return JsonConvert.DeserializeObject<RenameSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to rename an action object.
        /// </summary>
        /// <remarks>This RPC automatically releases write lock.</remarks>
        /// <param name="args">The action object ID and new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RenameObjectResponse> RenameActionObjectAsync(RenameArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameObjectRequest(id, "RenameObject", args, isDryRun), id, "RenameObject");
            return JsonConvert.DeserializeObject<RenameObjectResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to close a scene.
        /// </summary>
        /// <param name="args">Should the action be forced (e.g. in case of unsaved changes).</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<CloseSceneResponse> CloseSceneAsync(CloseSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CloseSceneRequest(id, "CloseScene", args, isDryRun), id, "CloseScene");
            return JsonConvert.DeserializeObject<CloseSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to retrieve existing project of a scene.
        /// </summary>
        /// <param name="args">Scene ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ProjectsWithSceneResponse> GetProjectsWithSceneAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ProjectsWithSceneRequest(id, "ProjectsWithScene", args), id, "ProjectsWithScene");
            return JsonConvert.DeserializeObject<ProjectsWithSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to create a new project.
        /// </summary>
        /// <param name="args">Parent scene ID, project name, description, and if it should have its own logic.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<NewProjectResponse> AddNewProjectAsync(NewProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new NewProjectRequest(id, "NewProject", args, isDryRun), id, "NewProject");
            return JsonConvert.DeserializeObject<NewProjectResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to remove a project.
        /// </summary>
        /// <param name="args">Project ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<DeleteProjectResponse> RemoveProjectAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteProjectRequest(id, "DeleteProject", args), id, "DeleteProject");
            return JsonConvert.DeserializeObject<DeleteProjectResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to remove a package.
        /// </summary>
        /// <param name="args">Package ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<DeletePackageResponse> RemovePackageAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeletePackageRequest(id, "DeletePackage", args), id, "DeletePackage");
            return JsonConvert.DeserializeObject<DeletePackageResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to create a new action point.
        /// </summary>
        /// <param name="args">Name, position, and optional parent.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<AddActionPointResponse> AddActionPointAsync(AddActionPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointRequest(id, "AddActionPoint", args, isDryRun), id, "AddActionPoint");
            return JsonConvert.DeserializeObject<AddActionPointResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to create a new action point for robot's end effector.
        /// </summary>
        /// <param name="args">Robot (action object) ID, name, end effector ID, and optional arm ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<AddApUsingRobotResponse> AddActionPointUsingRobotAsync(AddApUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddApUsingRobotRequest(id, "AddApUsingRobot", args, isDryRun), id, "AddApUsingRobot");
            return JsonConvert.DeserializeObject<AddApUsingRobotResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update position of an action point.
        /// </summary>
        /// <param name="args">Action point ID and a new position.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateActionPointPositionResponse> UpdateActionPointPositionAsync(UpdateActionPointPositionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointPositionRequest(id, "UpdateActionPointPosition", args, isDryRun), id, "UpdateActionPointPosition");
            return JsonConvert.DeserializeObject<UpdateActionPointPositionResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to change the parent of an action point.
        /// </summary>
        /// <remarks>Only the child has to be locked manually. The parent is locked automatically and then both child and parent are unlocked automatically.</remarks>
        /// <param name="args">Action point ID and the ID of the new parent.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateActionPointParentResponse> UpdateActionPointParentAsync(UpdateActionPointParentRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointParentRequest(id, "UpdateActionPointParent", args, isDryRun), id, "UpdateActionPointParent");
            return JsonConvert.DeserializeObject<UpdateActionPointParentResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to create a new orientation of an action point.
        /// </summary>
        /// <param name="args">Action point ID, orientation and a name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<AddActionPointOrientationResponse> AddActionPointOrientationAsync(AddActionPointOrientationRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointOrientationRequest(id, "AddActionPointOrientation", args, isDryRun), id, "AddActionPointOrientation");
            return JsonConvert.DeserializeObject<AddActionPointOrientationResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to remove orientation from an action point.
        /// </summary>
        /// <param name="args">Orientation ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RemoveActionPointOrientationResponse> RemoveActionPointOrientationAsync(RemoveActionPointOrientationRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointOrientationRequest(id, "RemoveActionPointOrientation", args, isDryRun), id, "RemoveActionPointOrientation");
            return JsonConvert.DeserializeObject<RemoveActionPointOrientationResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update orientation of an action point.
        /// </summary>
        /// <param name="args">Orientation ID and a new orientation data.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateActionPointOrientationResponse> UpdateActionPointOrientationAsync(UpdateActionPointOrientationRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointOrientationRequest(id, "UpdateActionPointOrientation", args), id, "UpdateActionPointOrientation");
            return JsonConvert.DeserializeObject<UpdateActionPointOrientationResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to create a new orientation of robot end effector's action point.
        /// </summary>
        /// <param name="args">Action point ID, robot information, and a name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<AddActionPointOrientationUsingRobotResponse> AddActionPointOrientationUsingRobotAsync(AddActionPointOrientationUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointOrientationUsingRobotRequest(id, "AddActionPointOrientationUsingRobot", args, isDryRun), id, "AddActionPointOrientationUsingRobot");
            return JsonConvert.DeserializeObject<AddActionPointOrientationUsingRobotResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update orientation of robot end effector's action point.
        /// </summary>
        /// <param name="args">Orientation ID and robot information.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateActionPointOrientationUsingRobotResponse> UpdateActionPointOrientationUsingRobotAsync(UpdateActionPointOrientationUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointOrientationUsingRobotRequest(id, "UpdateActionPointOrientationUsingRobot", args), id, "UpdateActionPointOrientationUsingRobot");
            return JsonConvert.DeserializeObject<UpdateActionPointOrientationUsingRobotResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to create new joints of robot's (end effector) action point.
        /// </summary>
        /// <param name="args">Action point ID, robot/arm/end effector ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<AddActionPointJointsUsingRobotResponse> AddActionPointJointsUsingRobotAsync(AddActionPointJointsUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointJointsUsingRobotRequest(id, "AddActionPointJointsUsingRobot", args, isDryRun), id, "AddActionPointJointsUsingRobot");
            return JsonConvert.DeserializeObject<AddActionPointJointsUsingRobotResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update joints of an action point.
        /// </summary>
        /// <param name="args">Joints ID and a list of joint names and values.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateActionPointJointsResponse> UpdateActionPointJointsAsync(UpdateActionPointJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointJointsRequest(id, "UpdateActionPointJoints", args), id, "UpdateActionPointJoints");
            return JsonConvert.DeserializeObject<UpdateActionPointJointsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update joints of robot's (end effector) action point.
        /// </summary>
        /// <param name="args">Joints ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateActionPointJointsUsingRobotResponse> UpdateActionPointJointsUsingRobotAsync(UpdateActionPointJointsUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointJointsUsingRobotRequest(id, "UpdateActionPointJointsUsingRobot", args), id, "UpdateActionPointJointsUsingRobot");
            return JsonConvert.DeserializeObject<UpdateActionPointJointsUsingRobotResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to rename an action point.
        /// </summary>
        /// <param name="args">Action point ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RenameActionPointResponse> RenameActionPointAsync(RenameActionPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionPointRequest(id, "RenameActionPoint", args, isDryRun), id, "RenameActionPoint");
            return JsonConvert.DeserializeObject<RenameActionPointResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to rename joints of an action point.
        /// </summary>
        /// <param name="args">Joints ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RenameActionPointJointsResponse> RenameActionPointJointsAsync(RenameActionPointJointsRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionPointJointsRequest(id, "RenameActionPointJoints", args, isDryRun), id, "RenameActionPointJoints");
            return JsonConvert.DeserializeObject<RenameActionPointJointsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to rename orientation of an action point.
        /// </summary>
        /// <param name="args">Orientation ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RenameActionPointOrientationResponse> RenameActionPointOrientationAsync(RenameActionPointOrientationRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionPointOrientationRequest(id, "RenameActionPointOrientation", args, isDryRun), id, "RenameActionPointOrientation");
            return JsonConvert.DeserializeObject<RenameActionPointOrientationResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to move selected robot to an action point.
        /// </summary>
        /// <param name="args">Robot ID, speed (0-1f), optional end effector ID, either an orientation or joints ID, safe flag (collision checks), linear flag, and optional arm ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<MoveToActionPointResponse> MoveToActionPointAsync(MoveToActionPointRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new MoveToActionPointRequest(id, "MoveToActionPoint", args), id, "MoveToActionPoint");
            return JsonConvert.DeserializeObject<MoveToActionPointResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to move selected robot to a pose.
        /// </summary>
        /// <remarks>
        /// Either position or orientation must be filled.
        /// </remarks>
        /// <param name="args">Robot ID, end effector ID, speed (0-1f), optional position or orientation, safe flag (collision checks), linear flag, and optional arm ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<MoveToPoseResponse> MoveToPoseAsync(MoveToPoseRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new MoveToPoseRequest(id, "MoveToPose", args), id, "MoveToPose");
            return JsonConvert.DeserializeObject<MoveToPoseResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to remove joints of an action point.
        /// </summary>
        /// <param name="args">Joints ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RemoveActionPointJointsResponse> RemoveActionPointJointsAsync(RemoveActionPointJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointJointsRequest(id, "RemoveActionPointJoints", args), id, "RemoveActionPointJoints");
            return JsonConvert.DeserializeObject<RemoveActionPointJointsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to add an action to action point.
        /// </summary>
        /// <param name="args">Action point ID, name, action type, parameters, and flows.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<AddActionResponse> AddActionAsync(AddActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionRequest(id, "AddAction", args, isDryRun), id, "AddAction");
            return JsonConvert.DeserializeObject<AddActionResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update an action.
        /// </summary>
        /// <param name="args">Action ID and updated parameters and flows.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateActionResponse> UpdateActionAsync(UpdateActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionRequest(id, "UpdateAction", args, isDryRun), id, "UpdateAction");
            return JsonConvert.DeserializeObject<UpdateActionResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to remove an action.
        /// </summary>
        /// <param name="args">Action ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RemoveActionResponse> RemoveActionAsync(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionRequest(id, "RemoveAction", args, isDryRun), id, "RemoveAction");
            return JsonConvert.DeserializeObject<RemoveActionResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to rename an action.
        /// </summary>
        /// <param name="args">Action ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RenameActionResponse> RenameActionAsync(RenameActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionRequest(id, "RenameAction", args, isDryRun), id, "RenameAction");
            return JsonConvert.DeserializeObject<RenameActionResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to add a new logic item (a connection between two actions) to the project.
        /// </summary>
        /// <remarks>Only the first action has to be locked manually. The second action is locked automatically by the server and then both are also unlocked automatically.</remarks>
        /// <param name="args">Start (ID of first action), end (ID of second action), and an optional condition for the logic item.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<AddLogicItemResponse> AddLogicItemAsync(AddLogicItemRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddLogicItemRequest(id, "AddLogicItem", args, isDryRun), id, "AddLogicItem");
            return JsonConvert.DeserializeObject<AddLogicItemResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update a logic item (a connection between two actions).
        /// </summary>
        /// <param name="args">Logic item ID, start (ID of first action), end (ID of second action), and an optional condition for the logic item.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateLogicItemResponse> UpdateLogicItemAsync(UpdateLogicItemRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateLogicItemRequest(id, "UpdateLogicItem", args, isDryRun), id, "UpdateLogicItem");
            return JsonConvert.DeserializeObject<UpdateLogicItemResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to remove a logic item (a connection between two actions).
        /// </summary>
        /// <param name="args">Logic item ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RemoveLogicItemResponse> RemoveLogicItemAsync(RemoveLogicItemRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveLogicItemRequest(id, "RemoveLogicItem", args), id, "RemoveLogicItem");
            return JsonConvert.DeserializeObject<RemoveLogicItemResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to rename a project.
        /// </summary>
        /// <param name="args">Project ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RenameProjectResponse> RenameProjectAsync(RenameProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameProjectRequest(id, "RenameProject", args, isDryRun), id, "RenameProject");
            return JsonConvert.DeserializeObject<RenameProjectResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to remove an action point.
        /// </summary>
        /// <param name="args">Action point ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RemoveActionPointResponse> RemoveActionPointAsync(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointRequest(id, "RemoveActionPoint", args, isDryRun), id, "RemoveActionPoint");
            return JsonConvert.DeserializeObject<RemoveActionPointResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to close a project.
        /// </summary>
        /// <param name="args">Should the action be forced (e.g. in case of unsaved changes).</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<CloseProjectResponse> CloseProjectAsync(CloseProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CloseProjectRequest(id, "CloseProject", args, isDryRun), id, "CloseProject");
            return JsonConvert.DeserializeObject<CloseProjectResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get a pose of robot's end effector.
        /// </summary>
        /// <param name="args">Robot, end effector (and arm) ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetEndEffectorPoseResponse> GetEndEffectorPoseAsync(GetEndEffectorPoseRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetEndEffectorPoseRequest(id, "GetEndEffectorPose", args), id, "GetEndEffectorPose");
            return JsonConvert.DeserializeObject<GetEndEffectorPoseResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to register/unregister itself for robot's end effector/joints update events.
        /// </summary>
        /// <param name="args">Robot ID, type (eef/joints), and if the request is registering or unregistering.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RegisterForRobotEventResponse> RegisterForRobotEventAsync(RegisterForRobotEventRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RegisterForRobotEventRequest(id, "RegisterForRobotEvent", args), id, "RegisterForRobotEvent");
            return JsonConvert.DeserializeObject<RegisterForRobotEventResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get information about a robot.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetRobotMetaResponse> GetRobotMetaAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetRobotMetaRequest(id, "GetRobotMeta"), id, "GetRobotMeta");
            return JsonConvert.DeserializeObject<GetRobotMetaResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get a list of end effectors of a robot.
        /// </summary>
        /// <param name="args">Robot (and arm) ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetEndEffectorsResponse> GetRobotEndEffectorsAsync(GetEndEffectorsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetEndEffectorsRequest(id, "GetEndEffectors", args), id, "GetEndEffectors");
            return JsonConvert.DeserializeObject<GetEndEffectorsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get a list of arms of a robot.
        /// </summary>
        /// <param name="args">Robot ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetRobotArmsResponse> GetRobotArmsAsync(GetRobotArmsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetRobotArmsRequest(id, "GetRobotArms", args), id, "GetRobotArms");
            return JsonConvert.DeserializeObject<GetRobotArmsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to start an offline scene.
        /// </summary>
        /// <remarks>All locks must be freed before starting a scene.</remarks>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<StartSceneResponse> StartSceneAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StartSceneRequest(id, "StartScene", isDryRun), id, "StartScene");
            return JsonConvert.DeserializeObject<StartSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to stop an online scene.
        /// </summary>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<StopSceneResponse> StopSceneAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StopSceneRequest(id, "StopScene", isDryRun), id, "StopScene");
            return JsonConvert.DeserializeObject<StopSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update action object's parameters.
        /// </summary>
        /// <param name="args">Action object ID and a list of Name-Type-Value parameters.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateObjectParametersResponse> UpdateActionObjectParametersAsync(UpdateObjectParametersRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectParametersRequest(id, "UpdateObjectParameters", args, isDryRun), id, "UpdateObjectParameters");
            return JsonConvert.DeserializeObject<UpdateObjectParametersResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to add an override to project's action object.
        /// </summary>
        /// <param name="args">Action object ID and a new parameter override.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<AddOverrideResponse> AddOverrideAsync(AddOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddOverrideRequest(id, "AddOverride", args, isDryRun), id, "AddOverride");
            return JsonConvert.DeserializeObject<AddOverrideResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update an override of project's action object.
        /// </summary>
        /// <param name="args">Action object ID and the parameter override.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateOverrideResponse> UpdateOverrideAsync(UpdateOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateOverrideRequest(id, "UpdateOverride", args, isDryRun), id, "UpdateOverride");
            return JsonConvert.DeserializeObject<UpdateOverrideResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to remove an override of project's action object.
        /// </summary>
        /// <param name="args">Action object ID and the parameter override.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<DeleteOverrideResponse> RemoveOverrideAsync(DeleteOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteOverrideRequest(id, "DeleteOverride", args, isDryRun), id, "DeleteOverride");
            return JsonConvert.DeserializeObject<DeleteOverrideResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to calculate the inverse kinematics for a robot's end-effector and update it.
        /// </summary>
        /// <param name="args">Robot ID, end effector ID, target pose, optional start joints, collision avoidance flag, and optional arm ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<InverseKinematicsResponse> InverseKinematicsAsync(InverseKinematicsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new InverseKinematicsRequest(id, "InverseKinematics", args), id, "InverseKinematics");
            return JsonConvert.DeserializeObject<InverseKinematicsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to calculate the forward kinematics for a robot's joints and update it.
        /// </summary>
        /// <param name="args">Robot ID, end effector ID, joint positions, and optional arm ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ForwardKinematicsResponse> ForwardKinematicsAsync(ForwardKinematicsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ForwardKinematicsRequest(id, "ForwardKinematics", args), id, "ForwardKinematics");
            return JsonConvert.DeserializeObject<ForwardKinematicsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to calibrate a robot.
        /// </summary>
        /// <remarks>Robot with a model and calibrated camera is required.</remarks>
        /// <param name="args">Robot ID, camera ID, and if the robot should move into the calibration pose flag.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<CalibrateRobotResponse> CalibrateRobotAsync(CalibrateRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CalibrateRobotRequest(id, "CalibrateRobot", args), id, "CalibrateRobot");
            return JsonConvert.DeserializeObject<CalibrateRobotResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to calibrate a camera action object.
        /// </summary>
        /// <param name="args">Camera ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<CalibrateCameraResponse> CalibrateCameraAsync(CalibrateCameraRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CalibrateCameraRequest(id, "CalibrateCamera", args), id, "CalibrateCamera");
            return JsonConvert.DeserializeObject<CalibrateCameraResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get a color image from camera.
        /// </summary>
        /// <remarks>
        ///  The image is encoded as a Latin-1 string representation of JPEG image data,
        /// where each byte of the JPEG is mapped directly to its corresponding Latin-1 character
        /// </remarks>
        /// <param name="args">Camera ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        [Obsolete("Current ARCOR2 implementation (1.5.0) does not implement this RPC yet.")]
        public async Task<CameraColorImageResponse> GetCameraColorImageAsync(CameraColorImageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CameraColorImageRequest(id, "CameraColorImage", args), id, "CameraColorImage");
            return JsonConvert.DeserializeObject<CameraColorImageResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to estimate the pose of a camera.
        /// </summary>
        /// <param name="args">Camera parameters, image (latin-1 encoded), and inverse flag.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetCameraPoseResponse> GetCameraPoseAsync(GetCameraPoseRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetCameraPoseRequest(id, "GetCameraPose", args), id, "GetCameraPose");
            return JsonConvert.DeserializeObject<GetCameraPoseResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to estimate markers corners.
        /// </summary>
        /// <param name="args">Camera parameters, image (latin-1 encoded).</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<MarkersCornersResponse> GetMarkersCornersAsync(MarkersCornersRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new MarkersCornersRequest(id, "MarkersCorners", args), id, "MarkersCorners");
            return JsonConvert.DeserializeObject<MarkersCornersResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to lock an object for writing.
        /// </summary>
        /// <param name="args">Object ID and if the whole object subtree should be locked.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<WriteLockResponse> WriteLockAsync(WriteLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new WriteLockRequest(id, "WriteLock", args), id, "WriteLock");
            return JsonConvert.DeserializeObject<WriteLockResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to unlock an object for writing.
        /// </summary>
        /// <param name="args">Object ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<WriteUnlockResponse> WriteUnlockAsync(WriteUnlockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new WriteUnlockRequest(id, "WriteUnlock", args), id, "WriteUnlock");
            return JsonConvert.DeserializeObject<WriteUnlockResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to lock an object for reading.
        /// </summary>
        /// <param name="args">Object ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        [Obsolete("Current ARCOR2 implementation (1.5.0) does not have a real use for client read-locking.")]
        public async Task<ReadLockResponse> ReadLockAsync(ReadLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ReadLockRequest(id, "ReadLock", args), id, "ReadLock");
            return JsonConvert.DeserializeObject<ReadLockResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to unlock an object for reading.
        /// </summary>
        /// <param name="args">Object ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        [Obsolete("Current ARCOR2 implementation (1.5.0) does not have a real use for client read-locking.")]
        public async Task<ReadUnlockResponse> ReadUnlockAsync(ReadUnlockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ReadUnlockRequest(id, "ReadUnlock", args), id, "ReadUnlock");
            return JsonConvert.DeserializeObject<ReadUnlockResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update lock type (object/tree).
        /// </summary>
        /// <param name="args">Object ID and new lock type.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateLockResponse> UpdateLockAsync(UpdateLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateLockRequest(id, "UpdateLock", args), id, "UpdateLock");
            return JsonConvert.DeserializeObject<UpdateLockResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to toggle hand teaching mode.
        /// </summary>
        /// <param name="args">Robot ID, toggle.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<HandTeachingModeResponse> SetHandTeachingModeAsync(HandTeachingModeRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new HandTeachingModeRequest(id, "HandTeachingMode", args, isDryRun), id, "HandTeachingMode");
            return JsonConvert.DeserializeObject<HandTeachingModeResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to duplicate an action point.
        /// </summary>
        /// <param name="args">Object ID and boolean if the object tree should be locked.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<CopyActionPointResponse> DuplicateActionPointAsync(CopyActionPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CopyActionPointRequest(id, "CopyActionPoint", args, isDryRun), id, "CopyActionPoint");
            return JsonConvert.DeserializeObject<CopyActionPointResponse>(response, jsonSettings)!;

        }

        /// <summary>
        /// Sends a request to step robot's end effector.
        /// </summary>
        /// <remarks>Mode.User is not yet supported as of ARCOR2 server v1.5.0.</remarks>
        /// <param name="args">
        /// Robot ID, end effector ID, axis, what (position/orientation), mode, step size, safe flag,
        /// optional pose (e.g. if relative), speed (0-1f), linear movement flag, and optional arm ID.
        /// </param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<StepRobotEefResponse> StepRobotEndEffectorAsync(StepRobotEefRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StepRobotEefRequest(id, "StepRobotEef", args, isDryRun), id, "StepRobotEef");
            return JsonConvert.DeserializeObject<StepRobotEefResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to set the end effector perpendicular to the world frame.
        /// </summary>
        /// <param name="args">Robot ID, end effector ID, safety flag, speed, linear movement flag, and optional arm ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The result.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<SetEefPerpendicularToWorldResponse> SetEndEffectorPerpendicularToWorldAsync(SetEefPerpendicularToWorldRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SetEefPerpendicularToWorldRequest(id, "SetEefPerpendicularToWorld", args, isDryRun), id, "SetEefPerpendicularToWorld");
            return JsonConvert.DeserializeObject<SetEefPerpendicularToWorldResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to register user for this session.
        /// </summary>
        /// <param name="args">Username.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RegisterUserRequest(id, "RegisterUser", args), id, "RegisterUser");
            return JsonConvert.DeserializeObject<RegisterUserResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to add a project parameter.
        /// </summary>
        /// <param name="args">Parameter in Name-Type-Value format.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<AddProjectParameterResponse> AddProjectParameterAsync(AddProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddProjectParameterRequest(id, "AddProjectParameter", args, isDryRun), id, "AddProjectParameter");
            return JsonConvert.DeserializeObject<AddProjectParameterResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update a value of project parameter.
        /// </summary>
        /// <param name="args">Project parameter ID and a new value.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateProjectParameterResponse> UpdateProjectParameterAsync(UpdateProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateProjectParameterRequest(id, "UpdateProjectParameter", args, isDryRun), id, "UpdateProjectParameter");
            return JsonConvert.DeserializeObject<UpdateProjectParameterResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to remove project parameter.
        /// </summary>
        /// <param name="args">Project parameter ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<RemoveProjectParameterResponse> RemoveProjectParameterAsync(RemoveProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveProjectParameterRequest(id, "RemoveProjectParameter", args, isDryRun), id, "RemoveProjectParameter");
            return JsonConvert.DeserializeObject<RemoveProjectParameterResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update object model of an object type.
        /// </summary>
        /// <param name="args">Object type ID and the object model.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateObjectModelResponse> UpdateObjectTypeModelAsync(UpdateObjectModelRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectModelRequest(id, "UpdateObjectModel", args, isDryRun), id, "UpdateObjectModel");
            return JsonConvert.DeserializeObject<UpdateObjectModelResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to add a virtual collision object to a scene.
        /// </summary>
        /// <param name="args">Name, pose, and the object.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<AddVirtualCollisionObjectToSceneResponse> AddVirtualCollisionObjectToSceneAsync(AddVirtualCollisionObjectToSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddVirtualCollisionObjectToSceneRequest(id, "AddVirtualCollisionObjectToScene", args, isDryRun), id, "AddVirtualCollisionObjectToScene");
            return JsonConvert.DeserializeObject<AddVirtualCollisionObjectToSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to duplicate a scene.
        /// </summary>
        /// <param name="args">Scene ID and a new name.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<CopySceneResponse> DuplicateSceneAsync(CopySceneRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CopySceneRequest(id, "CopyScene", args), id, "CopyScene");
            return JsonConvert.DeserializeObject<CopySceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to duplicate a scene.
        /// </summary>
        /// <param name="args">Project ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<CopyProjectResponse> DuplicateProjectAsync(CopyProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CopyProjectRequest(id, "CopyProject", args, isDryRun), id, "CopyProject");
            return JsonConvert.DeserializeObject<CopyProjectResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to step currently executing package by actions.
        /// </summary>
        /// <remarks>The execution must be paused before calling.</remarks>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<StepActionResponse> StepActionAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StepActionRequest(id, "StepAction"), id, "StepAction");
            return JsonConvert.DeserializeObject<StepActionResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get camera's color parameters.
        /// </summary>
        /// <param name="args">Camera ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<CameraColorParametersResponse> GetCameraColorParametersAsync(CameraColorParametersRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CameraColorParametersRequest(id, "CameraColorParameters", args), id, "CameraColorParameters");
            return JsonConvert.DeserializeObject<CameraColorParametersResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get a list of robot's gripper IDs.
        /// </summary>
        /// <param name="args">Robot ID and optional arm ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetGrippersResponse> GetGrippersAsync(GetGrippersRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetGrippersRequest(id, "GetGrippers", args), id, "GetGrippers");
            return JsonConvert.DeserializeObject<GetGrippersResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get a project using ID.
        /// </summary>
        /// <param name="args">Project ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetProjectResponse> GetProjectAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetProjectRequest(id, "GetProject", args), id, "GetProject");
            return JsonConvert.DeserializeObject<GetProjectResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get joints of a robot.
        /// </summary>
        /// <param name="args">Robot (and arm) ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetRobotJointsResponse> GetRobotJointsAsync(GetRobotJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetRobotJointsRequest(id, "GetRobotJoints", args), id, "GetRobotJoints");
            return JsonConvert.DeserializeObject<GetRobotJointsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get a scene using ID.
        /// </summary>
        /// <param name="args">Scene ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetSceneResponse> GetSceneAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetSceneRequest(id, "GetScene", args), id, "GetScene");
            return JsonConvert.DeserializeObject<GetSceneResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get a list of robot's suctions IDs.
        /// </summary>
        /// <param name="args">Robot ID and optional arm ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<GetSuctionsResponse> GetSuctionsAsync(GetSuctionsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetSuctionsRequest(id, "GetSuctions", args), id, "GetSuctions");
            return JsonConvert.DeserializeObject<GetSuctionsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to move robot joints.
        /// </summary>
        /// <param name="args">Robot ID, speed (0-1f), list of joints, safe flag, and optional arm ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<MoveToJointsResponse> MoveToJointsAsync(MoveToJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new MoveToJointsRequest(id, "MoveToJoints", args), id, "MoveToJoints");
            return JsonConvert.DeserializeObject<MoveToJointsResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get scene IDs that use specified object type.
        /// </summary>
        /// <param name="args">Object type ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<ObjectTypeUsageResponse> GetObjectTypeUsageAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectTypeUsageRequest(id, "ObjectTypeUsage", args), id, "ObjectTypeUsage");
            return JsonConvert.DeserializeObject<ObjectTypeUsageResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to get project IDs that use specified action object from a scene.
        /// </summary>
        /// <param name="args">Action object ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<SceneObjectUsageResponse> GetSceneActionObjectUsageAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SceneObjectUsageRequest(id, "SceneObjectUsage", args), id, "SceneObjectUsage");
            return JsonConvert.DeserializeObject<SceneObjectUsageResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to stop a robot.
        /// </summary>
        /// <param name="args">Robot ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<StopRobotResponse> StopRobotAsync(StopRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StopRobotRequest(id, "StopRobot", args), id, "StopRobot");
            return JsonConvert.DeserializeObject<StopRobotResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update project's description.
        /// </summary>
        /// <param name="args">Project ID and new description.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateProjectDescriptionResponse> UpdateProjectDescriptionAsync(UpdateProjectDescriptionRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateProjectDescriptionRequest(id, "UpdateProjectDescription", args), id, "UpdateProjectDescription");
            return JsonConvert.DeserializeObject<UpdateProjectDescriptionResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update if project contains logic.
        /// </summary>
        /// <param name="args">Project ID and boolean value indicating if project should have logic.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateProjectHasLogicResponse> UpdateProjectHasLogicAsync(UpdateProjectHasLogicRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateProjectHasLogicRequest(id, "UpdateProjectHasLogic", args), id, "UpdateProjectHasLogic");
            return JsonConvert.DeserializeObject<UpdateProjectHasLogicResponse>(response, jsonSettings)!;
        }

        /// <summary>
        /// Sends a request to update scene's description.
        /// </summary>
        /// <param name="args">Scene ID and new description.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">When the response is not received within <see cref="Arcor2ClientSettings.RpcTimeout"/> (10 seconds by default).</exception>
        /// <exception cref="Arcor2ConnectionException">When connection fails or the in case of ARCOR2 protocol violation (e.g. matching IDs, but mismatching RPC names).</exception>
        public async Task<UpdateSceneDescriptionResponse> UpdateSceneDescriptionAsync(UpdateSceneDescriptionRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateSceneDescriptionRequest(id, "UpdateSceneDescription", args), id, "UpdateSceneDescription");
            return JsonConvert.DeserializeObject<UpdateSceneDescriptionResponse>(response, jsonSettings)!;
        }

        #endregion

        #endregion
    }
}