using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcor2.ClientSdk.Communication.Design;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Newtonsoft.Json;
using WebSocketState = Arcor2.ClientSdk.Communication.Design.WebSocketState;

namespace Arcor2.ClientSdk.Communication {
    /// <summary>
    /// Client for communication with ARCOR2 server. Default implementation using the System.Net.WebSockets.ClientWebSocket.
    /// </summary>
    public class Arcor2Client : Arcor2Client<SystemNetWebSocket> { };

    /// <summary>
    /// Client for communication with ARCOR2 server using custom WebSocket implementation.
    /// </summary>
    /// <typeparam name="TWebSocket">WebSocket implementation</typeparam>
    public class Arcor2Client<TWebSocket> where TWebSocket : class, IWebSocket, new() {
        private readonly TWebSocket webSocket = new TWebSocket();

        /// <summary>
        /// Represents a request waiting for corresponding response.
        /// </summary>
        private class PendingRequest {
            public TaskCompletionSource<string> TaskCompletionSource { get; } = new TaskCompletionSource<string>();
            public CancellationTokenSource CancellationTokenSource { get; }

            public PendingRequest(int timeout) {
                CancellationTokenSource = new CancellationTokenSource(timeout);
                CancellationTokenSource.Token.Register(() => {
                    TaskCompletionSource.TrySetException(new TimeoutException("The request timed out."));
                }, false);
            }
        }

        private readonly ConcurrentDictionary<int, PendingRequest> pendingRequests = new ConcurrentDictionary<int, PendingRequest>();

        /// <summary>
        /// Holds the current available request ID for tracking request-response messages.
        /// </summary>
        private int requestId;

        /// <summary>
        /// The URI of the current server.
        /// </summary>
        public Uri? Uri { get; private set; }

        public WebSocketState State { get; }

        #region Public Event Definitions

        /// <summary>
        /// Raised when any connection-related error occurs.
        /// </summary>
        public EventHandler<Exception>? ConnectionError;
        /// <summary>
        /// Raised when connection is closed.
        /// </summary>
        public EventHandler<WebSocketCloseEventArgs>? ConnectionClosed;
        /// <summary>
        /// Raised when connection is successfully opened.
        /// </summary>
        public EventHandler? ConnectionOpened;

        /// <summary>
        /// Raised when scene is removed.
        /// </summary>
        public event EventHandler<BareSceneEventArgs>? OnSceneRemoved;
        /// <summary>
        /// Raised when scene base is updated (e.g. copy or name/description update).
        /// </summary>
        public event EventHandler<BareSceneEventArgs>? OnSceneBaseUpdated;

        /// <summary>
        /// Raised when state of scene changes (stopping/stopped/starting/started).
        /// </summary>
        public event EventHandler<SceneStateEventArgs>? OnSceneStateEvent;

        /// <summary>
        /// Raised when action object is added. 
        /// </summary>
        public event EventHandler<SceneObjectEventArgs>? OnSceneObjectAdded;
        /// <summary>
        /// Raised when action object is removed.
        /// </summary>
        public event EventHandler<SceneObjectEventArgs>? OnSceneObjectRemoved;
        /// <summary>
        /// Raised when action object is updated (e.g. translated).
        /// </summary>
        public event EventHandler<SceneObjectEventArgs>? OnSceneObjectUpdated;

        /// <summary>
        /// Raised when action point is added.
        /// </summary>
        public event EventHandler<BareActionPointEventArgs>? OnActionPointAdded;
        /// <summary>
        /// Raised when action point is updated (e.g. translated).
        /// </summary>
        public event EventHandler<BareActionPointEventArgs>? OnActionPointUpdated;
        /// <summary>
        /// Raised when action point base is updated (e.g. renamed).
        /// </summary>
        public event EventHandler<BareActionPointEventArgs>? OnActionPointBaseUpdated;
        /// <summary>
        /// Raised when action point is removed.
        /// </summary>
        public event EventHandler<BareActionPointEventArgs>? OnActionPointRemoved;

        /// <summary>
        /// Raised when project override is added.
        /// </summary>
        public event EventHandler<ParameterEventArgs>? OnOverrideAdded;
        /// <summary>
        /// Raised when project override is updated (existing named value changed).
        /// </summary>
        public event EventHandler<ParameterEventArgs>? OnOverrideUpdated;
        /// <summary>
        /// Raised when project override is removed.
        /// </summary>
        public event EventHandler<ParameterEventArgs>? OnOverrideRemoved;

        /// <summary>
        /// Raised when action is added.
        /// </summary>
        public event EventHandler<BareActionEventArgs>? OnActionAdded;
        /// <summary>
        /// Raised when action is updated (e.g. parameters or flows).
        /// </summary>
        public event EventHandler<BareActionEventArgs>? OnActionUpdated;
        /// <summary>
        /// Raised when action base is updated (e.g. rename).
        /// </summary>
        public event EventHandler<BareActionEventArgs>? OnActionBaseUpdated;
        /// <summary>
        /// Raised when action is removed.
        /// </summary>
        public event EventHandler<BareActionEventArgs>? OnActionRemoved;

        /// <summary>
        /// Raised when logic item is added.
        /// </summary>
        public event EventHandler<LogicItemChangedEventArgs>? OnLogicItemAdded;
        /// <summary>
        /// Raised when logic item is updated.
        /// </summary>
        public event EventHandler<LogicItemChangedEventArgs>? OnLogicItemUpdated;
        /// <summary>
        /// Raised when logic item is removed.
        /// </summary>
        public event EventHandler<LogicItemChangedEventArgs>? OnLogicItemRemoved;

        /// <summary>
        /// Raised when new action point orientation is added.
        /// </summary>
        public event EventHandler<OrientationEventArgs>? OnOrientationAdded;
        /// <summary>
        /// Raised when action point orientation is updated.
        /// </summary>
        public event EventHandler<OrientationEventArgs>? OnOrientationUpdated;
        /// <summary>
        /// Raised when action point orientation base is updated (e.g. rename).
        /// </summary>
        public event EventHandler<OrientationEventArgs>? OnOrientationBaseUpdated;
        /// <summary>
        /// Raised when action point orientation is removed.
        /// </summary>
        public event EventHandler<OrientationEventArgs>? OnOrientationRemoved;

        /// <summary>
        /// Raised when new action point joints are added.
        /// </summary>
        public event EventHandler<JointsEventArgs>? OnJointsAdded;
        /// <summary>
        /// Raised when action point joints are updated.
        /// </summary>
        public event EventHandler<JointsEventArgs>? OnJointsUpdated;
        /// <summary>
        /// Raised when action point joints base is updated (e.g. rename).
        /// </summary>
        public event EventHandler<JointsEventArgs>? OnJointsBaseUpdated;
        /// <summary>
        /// Raised when action point joints are removed.
        /// </summary>
        public event EventHandler<JointsEventArgs>? OnJointsRemoved;

        /// <summary>
        /// Raised when new object type is added.
        /// </summary>
        /// <remarks>
        /// Be careful that this event doesn't represent an instance of object type (action object) being added/removed from a scene - for that see <see cref="OnSceneObjectAdded"/> and related events.
        /// This event is rather used for signaling dynamic changes to the object type database (such as is the case with virtual objects <see cref="AddVirtualCollisionObjectToSceneAsync"/>).
        /// </remarks>
        public event EventHandler<ObjectTypesEventArgs>? OnObjectTypeAdded;
        /// <summary>
        /// Raised when new object type is updated.
        /// </summary>
        /// <remarks>
        /// Be careful that this event doesn't represent an instance of object type (action object) being added/removed from a scene - for that see <see cref="OnSceneObjectAdded"/> and related events.
        /// This event is rather used for signaling dynamic changes to the object type database (such as is the case with virtual objects <see cref="AddVirtualCollisionObjectToSceneAsync"/>).
        /// </remarks>
        public event EventHandler<ObjectTypesEventArgs>? OnObjectTypeUpdated;
        /// <summary>
        /// Raised when new object type is removed.
        /// </summary>
        /// <remarks>
        /// Be careful that this event doesn't represent an instance of object type (action object) being added/removed from a scene - for that see <see cref="OnSceneObjectAdded"/> and related events.
        /// This event is rather used for signaling dynamic changes to the object type database (such as is the case with virtual objects <see cref="AddVirtualCollisionObjectToSceneAsync"/>).
        /// </remarks>
        public event EventHandler<ObjectTypesEventArgs>? OnObjectTypeRemoved;

        /// <summary>
        /// Raised when robot moves to a pose (start/end).
        /// </summary>
        public event EventHandler<RobotMoveToPoseEventArgs>? OnRobotMoveToPose;
        /// <summary>
        /// Raised when robot moves to a joint (start/end).
        /// </summary>
        public event EventHandler<RobotMoveToJointsEventArgs>? OnRobotMoveToJoints;
        /// <summary>
        /// Raised when robot moves to action point orientation (start/end).
        /// </summary>
        public event EventHandler<RobotMoveToActionPointOrientationEventArgs>? OnRobotMoveToActionPointOrientation;
        /// <summary>
        /// Raised when robot moves to action point joints (start/end).
        /// </summary>
        public event EventHandler<RobotMoveToActionPointJointsEventArgs>? OnRobotMoveToActionPointJoints;
        /// <summary>
        /// Raised when hand teaching mode is enabled/disabled.
        /// </summary>
        public event EventHandler<HandTeachingModeEventArgs>? OnHandTeachingMode;

        /// <summary>
        /// Raised when new end effector poses.
        /// </summary>
        public event EventHandler<RobotEefUpdatedEventArgs>? OnRobotEefUpdated;
        /// <summary>
        /// Raised on new joints values.
        /// </summary>
        public event EventHandler<RobotJointsUpdatedEventArgs>? OnRobotJointsUpdated;

        /// <summary>
        /// Raised when project is saved by the server.
        /// </summary>
        public event EventHandler? OnProjectSaved;
        /// <summary>
        /// Raised when server finds open project for the user, and it is requesting the client UI to open it (e.g. such as when the user quickly reconnects).
        /// </summary>
        public event EventHandler<OpenProjectEventArgs>? OnOpenProject;
        /// <summary>
        /// Raised when server closes a project, and it is requesting the client UI to close it.
        /// </summary>
        public event EventHandler? OnProjectClosed;
        /// <summary>
        /// Raised when project base is updated (e.g. rename).
        /// </summary>
        public event EventHandler<BareProjectEventArgs>? OnProjectBaseUpdated;
        /// <summary>
        /// Raised when project is removed.
        /// </summary>
        public event EventHandler<BareProjectEventArgs>? OnProjectRemoved;

        /// <summary>
        /// Raised when project parameter is added.
        /// </summary>
        public event EventHandler<ProjectParameterEventArgs>? OnProjectParameterAdded;
        /// <summary>
        /// Raised when project parameter is updated.
        /// </summary>
        public event EventHandler<ProjectParameterEventArgs>? OnProjectParameterUpdated;
        /// <summary>
        /// Raised when project parameter is removed.
        /// </summary>
        public event EventHandler<ProjectParameterEventArgs>? OnProjectParameterRemoved;

        /// <summary>
        /// Raised when scene is saved by the server.
        /// </summary>
        public event EventHandler? OnSceneSaved;
        /// <summary>
        /// Raised when server finds open scene for the user, and it is requesting the client UI to open it (e.g. such as when the user quickly reconnects).
        /// </summary>
        public event EventHandler<OpenSceneEventArgs>? OnOpenScene;
        /// <summary>
        /// Raised when server closes a scene, and it is requesting the client UI to close it.
        /// </summary>
        public event EventHandler? OnSceneClosed;

        /// <summary>
        /// Raised when the server is requesting the client UI to show the main screen (e.g. after project/scene is closed).
        /// </summary>
        public event EventHandler<ShowMainScreenEventArgs>? OnShowMainScreen;

        /// <summary>
        /// Raised when objects get locked by a user.
        /// </summary>
        public event EventHandler<ObjectsLockEventArgs>? OnObjectsLocked;
        /// <summary>
        /// Raised when objects get unlocked.
        /// </summary>
        public event EventHandler<ObjectsLockEventArgs>? OnObjectsUnlocked;

        /// <summary>
        /// Raised when server notifies beginning of the action execution triggered while editing a project.
        /// </summary>
        public event EventHandler<ActionExecutionEventArgs>? OnActionExecution;
        /// <summary>
        /// Raised when server notifies that action execution was cancelled.
        /// </summary>
        public event EventHandler? OnActionCancelled;
        /// <summary>
        /// Raised when server notifies the result of the action execution triggered while editing a project.
        /// </summary>
        public event EventHandler<ActionResultEventArgs>? OnActionResult;

        /// <summary>
        /// Raised when the state of long-running process changes.
        /// </summary>
        public event EventHandler<ProcessStateEventArgs>? OnProcessState;

        /// <summary>
        /// Raised when new package is added.
        /// </summary>
        public event EventHandler<PackageChangedEventArgs>? OnPackageAdded;
        /// <summary>
        /// Raised when package is updated (e.g. renamed)
        /// </summary>
        public event EventHandler<PackageChangedEventArgs>? OnPackageUpdated;
        /// <summary>
        /// Raised when package is removed.
        /// </summary>
        public event EventHandler<PackageChangedEventArgs>? OnPackageRemoved;

        /// <summary>
        /// Raised when package (script) is initialized and ready to execute.
        /// </summary>
        public event EventHandler<PackageInfoEventArgs>? OnPackageInfo;
        /// <summary>
        /// Raised when execution status of a package changes.
        /// </summary>
        public event EventHandler<PackageStateEventArgs>? OnPackageState;
        /// <summary>
        /// Raised when error occurs while running a package.
        /// </summary>
        public event EventHandler<ProjectExceptionEventArgs>? OnProjectException;
        /// <summary>
        /// Raised while running a package before execution of an action (parameters and other information).
        /// </summary>
        public event EventHandler<ActionStateBeforeEventArgs>? OnActionStateBefore;
        /// <summary>
        /// Raised while running a package after execution of an action (returned value and other information).
        /// </summary>
        public event EventHandler<ActionStateAfterEventArgs>? OnActionStateAfter;

        #endregion

        /// <summary>
        /// Creates an instance of <see cref="Arcor2Client"/>.
        /// </summary>
        public Arcor2Client() {
            webSocket.OnError += (_, args) => {
                ConnectionError?.Invoke(this, args.Exception);
            };
            webSocket.OnClose += (_, args) => {
                ConnectionClosed?.Invoke(this, args);
            };
            webSocket.OnOpen += (_, __) => {
                ConnectionOpened?.Invoke(this, EventArgs.Empty);
            };
            webSocket.OnMessage += (_, args) => {
                OnMessage(args);
            };
        }

        /// <summary>
        /// Gets the WebSocket used by the client.
        /// </summary>
        /// <returns>The WebSocket used by the client.</returns>
        public TWebSocket GetUnderlyingWebSocket() => webSocket;

        #region Connection Management Methods

        /// <summary>
        /// Establishes a connection to ARCOR2 server.
        /// </summary>
        /// <remarks>The <see cref="ConnectionClosed"/> event is raised even when error occurs while connecting.</remarks>
        /// <param name="domain">Domain of the ARCOR2 server</param>
        /// <param name="port">Port od the ARCOR2 server</param>
        /// <exception cref="UriFormatException" />
        /// <exception cref="InvalidOperationException" />
        public async Task ConnectAsync(string domain, string port) {
            if(webSocket.State != WebSocketState.Open) {
                throw new InvalidOperationException("CloseAsync can not be invoked when connection is not opened.");
            }

            await webSocket.ConnectAsync(new Uri($"ws://{domain}:{port}"));
        }

        /// <summary>
        /// Establishes a connection to ARCOR2 server.
        /// </summary>
        /// <remarks>The <see cref="ConnectionClosed"/> event is raised even when error occurs while connecting.</remarks>
        /// <param name="uri">Full WebSocket URI</param>
        /// <exception cref="UriFormatException" />
        /// <exception cref="InvalidOperationException" />
        public async Task ConnectAsync(Uri uri) {
            if(webSocket.State != WebSocketState.Open) {
                throw new InvalidOperationException("CloseAsync can not be invoked when connection is not opened.");
            }

            await webSocket.ConnectAsync(uri);
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
        /// Sends a message without waiting for a response.
        /// </summary>
        private async Task SendAsync(string message) {
            if(webSocket.State != WebSocketState.Open) {
                throw new InvalidOperationException("Cannot send message when connection is not open.");
            }

            await webSocket.SendAsync(message);
        }

        /// <summary>
        /// Sends a request and waits for a response with the matching ID.
        /// </summary>
        /// <param name="message">Request object to be serialized and sent</param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <param name="id">ID of the request. Will increment and assign from <see cref="requestId"/> if null. </param>
        /// <returns>Response message</returns>
        /// <exception cref="InvalidOperationException">Thrown when connection is not open</exception>
        /// <exception cref="TimeoutException">Thrown when response is not received within timeout period</exception>
        private async Task<string> SendAndWaitAsync(string message, int? id = null, int timeout = 15000) {
            if(webSocket.State != WebSocketState.Open) {
                throw new InvalidOperationException("Cannot send message when connection is not open.");
            }

            id ??= Interlocked.Increment(ref requestId);

            var pendingRequest = new PendingRequest(timeout);

            if(!pendingRequests.TryAdd(id.Value, pendingRequest)) {
                throw new InvalidOperationException($"Request ID {id} already exists.");
            }

            try {
                await webSocket.SendAsync(message);
                return await pendingRequest.TaskCompletionSource.Task;
            }
            finally {
                pendingRequests.TryRemove(id.Value, out _);
                pendingRequest.CancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// Sends a request and waits for a response with the matching ID
        /// </summary>
        /// <typeparam name="T">The object type to be serialized</typeparam>
        /// <param name="message">Request object to be serialized and sent</param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <param name="id">ID of the request. Will increment and assign from <see cref="requestId"/> if null. </param>
        /// <returns>Response message</returns>
        /// <exception cref="InvalidOperationException">Thrown when connection is not open</exception>
        /// <exception cref="TimeoutException">Thrown when response is not received within timeout period</exception>
        private async Task<string> SendAndWaitAsync<T>(T message, int? id = null, int timeout = 15000) {
            return await SendAndWaitAsync(JsonConvert.SerializeObject(message), id, timeout);
        }

        #endregion

        /// <summary>
        /// Invoked when message from server is received.
        /// Sets request-response (RPC) completion or delegates events.
        /// </summary>
        /// <param name="args"></param>
        private void OnMessage(WebSocketMessageEventArgs args) {
            var data = Encoding.Default.GetString(args.Data);

            var dispatch = JsonConvert.DeserializeAnonymousType(data, new {
                id = (int?) default,
                response = (string?) default,
                @event = (string?) default,
                request = (string?) default
            });

            if(dispatch == null || (dispatch.response == null && dispatch.request == null && dispatch.@event == null)) {
                return;
            }

            // Handle responses
            if(dispatch.response != null && dispatch.id != null && dispatch.id != 0) {
                if(pendingRequests.TryGetValue(dispatch.id.Value, out PendingRequest? pendingRequest)) {
                    pendingRequest.TaskCompletionSource.TrySetResult(data);
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
                       throw new NotImplementedException($"Unknown event received: {dispatch.@event}");
                }
            }
        }

        #region Event Handlers

        private void HandleSceneChanged(string data) {
            var sceneChangedEvent = JsonConvert.DeserializeObject<SceneChanged>(data)!;
            switch(sceneChangedEvent.ChangeType) {
                case SceneChanged.ChangeTypeEnum.Add:
                    throw new NotImplementedException("Scene add should never occur.");
                case SceneChanged.ChangeTypeEnum.Remove:
                    OnSceneRemoved?.Invoke(this, new BareSceneEventArgs(sceneChangedEvent.Data));
                    break;
                case SceneChanged.ChangeTypeEnum.Update:
                    throw new NotImplementedException("Scene update should never occur.");
                case SceneChanged.ChangeTypeEnum.UpdateBase:
                    OnSceneBaseUpdated?.Invoke(this, new BareSceneEventArgs(sceneChangedEvent.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        private void HandleSceneState(string data) {
            var sceneState = JsonConvert.DeserializeObject<SceneState>(data)!;
            OnSceneStateEvent?.Invoke(this, new SceneStateEventArgs(sceneState.Data));
        }

        private void HandleSceneObjectChanged(string data) {
            var sceneObjectChanged = JsonConvert.DeserializeObject<SceneObjectChanged>(data)!;
            switch(sceneObjectChanged.ChangeType) {
                case SceneObjectChanged.ChangeTypeEnum.Add:
                    OnSceneObjectAdded?.Invoke(this, new SceneObjectEventArgs(sceneObjectChanged.Data));
                    break;
                case SceneObjectChanged.ChangeTypeEnum.Remove:
                    OnSceneObjectRemoved?.Invoke(this, new SceneObjectEventArgs(sceneObjectChanged.Data));
                    break;
                case SceneObjectChanged.ChangeTypeEnum.Update:
                    OnSceneObjectUpdated?.Invoke(this, new SceneObjectEventArgs(sceneObjectChanged.Data));
                    break;
                case SceneObjectChanged.ChangeTypeEnum.UpdateBase:
                    throw new NotImplementedException("SceneObject base update should never occur.");
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        private void HandleActionPointChanged(string data) {
            var actionPointChanged = JsonConvert.DeserializeObject<ActionPointChanged>(data)!;

            switch(actionPointChanged.ChangeType) {
                case ActionPointChanged.ChangeTypeEnum.Add:
                    OnActionPointAdded?.Invoke(this, new BareActionPointEventArgs(actionPointChanged.Data));
                    break;
                case ActionPointChanged.ChangeTypeEnum.Remove:
                    OnActionPointRemoved?.Invoke(this, new BareActionPointEventArgs(actionPointChanged.Data));
                    break;
                case ActionPointChanged.ChangeTypeEnum.Update:
                    OnActionPointUpdated?.Invoke(this, new BareActionPointEventArgs(actionPointChanged.Data));
                    break;
                case ActionPointChanged.ChangeTypeEnum.UpdateBase:
                    OnActionPointBaseUpdated?.Invoke(this, new BareActionPointEventArgs(actionPointChanged.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        private void HandleOverrideUpdated(string data) {
            var overrideUpdated = JsonConvert.DeserializeObject<OverrideUpdated>(data)!;

            switch(overrideUpdated.ChangeType) {
                case OverrideUpdated.ChangeTypeEnum.Add:
                    OnOverrideAdded?.Invoke(this, new ParameterEventArgs(overrideUpdated.Data, overrideUpdated.ParentId));
                    break;
                case OverrideUpdated.ChangeTypeEnum.Remove:
                    OnOverrideRemoved?.Invoke(this, new ParameterEventArgs(overrideUpdated.Data, overrideUpdated.ParentId));
                    break;
                case OverrideUpdated.ChangeTypeEnum.Update:
                    OnOverrideUpdated?.Invoke(this, new ParameterEventArgs(overrideUpdated.Data, overrideUpdated.ParentId));
                    break;
                case OverrideUpdated.ChangeTypeEnum.UpdateBase:
                    throw new NotImplementedException("OverrideUpdated base update should never occur.");
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        private void HandleActionChanged(string data) {
            var actionChanged = JsonConvert.DeserializeObject<ActionChanged>(data)!;

            switch(actionChanged.ChangeType) {
                case ActionChanged.ChangeTypeEnum.Add:
                    OnActionAdded?.Invoke(this, new BareActionEventArgs(actionChanged.Data, actionChanged.ParentId));
                    break;
                case ActionChanged.ChangeTypeEnum.Remove:
                    OnActionRemoved?.Invoke(this, new BareActionEventArgs(actionChanged.Data));
                    break;
                case ActionChanged.ChangeTypeEnum.Update:
                    OnActionUpdated?.Invoke(this, new BareActionEventArgs(actionChanged.Data));
                    break;
                case ActionChanged.ChangeTypeEnum.UpdateBase:
                    OnActionBaseUpdated?.Invoke(this, new BareActionEventArgs(actionChanged.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        private void HandleLogicItemChanged(string data) {
            var logicItemChanged = JsonConvert.DeserializeObject<LogicItemChanged>(data)!;

            switch(logicItemChanged.ChangeType) {
                case LogicItemChanged.ChangeTypeEnum.Add:
                    OnLogicItemAdded?.Invoke(this, new LogicItemChangedEventArgs(logicItemChanged.Data));
                    break;
                case LogicItemChanged.ChangeTypeEnum.Remove:
                    OnLogicItemRemoved?.Invoke(this, new LogicItemChangedEventArgs(logicItemChanged.Data));
                    break;
                case LogicItemChanged.ChangeTypeEnum.Update:
                    OnLogicItemUpdated?.Invoke(this, new LogicItemChangedEventArgs(logicItemChanged.Data));
                    break;
                case LogicItemChanged.ChangeTypeEnum.UpdateBase:
                    throw new NotImplementedException("Logic item base update should never occur.");
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        private void HandleOrientationChanged(string data) {
            var orientationChanged = JsonConvert.DeserializeObject<OrientationChanged>(data)!;

            switch(orientationChanged.ChangeType) {
                case OrientationChanged.ChangeTypeEnum.Add:
                    OnOrientationAdded?.Invoke(this, new OrientationEventArgs(orientationChanged.Data, orientationChanged.ParentId));
                    break;
                case OrientationChanged.ChangeTypeEnum.Remove:
                    OnOrientationRemoved?.Invoke(this, new OrientationEventArgs(orientationChanged.Data));
                    break;
                case OrientationChanged.ChangeTypeEnum.Update:
                    OnOrientationUpdated?.Invoke(this, new OrientationEventArgs(orientationChanged.Data));
                    break;
                case OrientationChanged.ChangeTypeEnum.UpdateBase:
                    OnOrientationBaseUpdated?.Invoke(this, new OrientationEventArgs(orientationChanged.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        private void HandleJointsChanged(string data) {
            var jointsChanged = JsonConvert.DeserializeObject<JointsChanged>(data)!;

            switch(jointsChanged.ChangeType) {
                case JointsChanged.ChangeTypeEnum.Add:
                    OnJointsAdded?.Invoke(this, new JointsEventArgs(jointsChanged.Data, jointsChanged.ParentId));
                    break;
                case JointsChanged.ChangeTypeEnum.Remove:
                    OnJointsRemoved?.Invoke(this, new JointsEventArgs(jointsChanged.Data));
                    break;
                case JointsChanged.ChangeTypeEnum.Update:
                    OnJointsUpdated?.Invoke(this, new JointsEventArgs(jointsChanged.Data));
                    break;
                case JointsChanged.ChangeTypeEnum.UpdateBase:
                    OnJointsBaseUpdated?.Invoke(this, new JointsEventArgs(jointsChanged.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        private void HandleChangedObjectTypes(string data) {
            var objectTypesChangedEvent = JsonConvert.DeserializeObject<ChangedObjectTypes>(data)!;

            switch(objectTypesChangedEvent.ChangeType) {
                case ChangedObjectTypes.ChangeTypeEnum.Add:
                    OnObjectTypeAdded?.Invoke(this, new ObjectTypesEventArgs(objectTypesChangedEvent.Data));
                    break;
                case ChangedObjectTypes.ChangeTypeEnum.Remove:
                    OnObjectTypeRemoved?.Invoke(this, new ObjectTypesEventArgs(objectTypesChangedEvent.Data));
                    break;
                case ChangedObjectTypes.ChangeTypeEnum.Update:
                    OnObjectTypeUpdated?.Invoke(this, new ObjectTypesEventArgs(objectTypesChangedEvent.Data));
                    break;
                case ChangedObjectTypes.ChangeTypeEnum.UpdateBase:
                    throw new NotImplementedException("Object type base update should never occur.");
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        private void HandleRobotMoveToActionPointOrientation(string data) {
            var robotMoveToActionPointOrientation = JsonConvert.DeserializeObject<RobotMoveToActionPointOrientation>(data)!;
            OnRobotMoveToActionPointOrientation?.Invoke(this, new RobotMoveToActionPointOrientationEventArgs(robotMoveToActionPointOrientation.Data));
        }

        private void HandleRobotMoveToPose(string data) {
            var robotMoveToPose = JsonConvert.DeserializeObject<RobotMoveToPose>(data)!;
            OnRobotMoveToPose?.Invoke(this, new RobotMoveToPoseEventArgs(robotMoveToPose.Data));
        }

        private void HandleRobotMoveToJoints(string data) {
            var robotMoveToJoints = JsonConvert.DeserializeObject<RobotMoveToJoints>(data)!;
            OnRobotMoveToJoints?.Invoke(this, new RobotMoveToJointsEventArgs(robotMoveToJoints.Data));
        }

        private void HandleRobotMoveToActionPointJoints(string data) {
            var robotMoveToActionPointJoints = JsonConvert.DeserializeObject<RobotMoveToActionPointJoints>(data)!;
            OnRobotMoveToActionPointJoints?.Invoke(this, new RobotMoveToActionPointJointsEventArgs(robotMoveToActionPointJoints.Data));
        }

        private void HandleActionStateBefore(string data) {
            var actionStateBefore = JsonConvert.DeserializeObject<ActionStateBefore>(data)!;
            OnActionStateBefore?.Invoke(this, new ActionStateBeforeEventArgs(actionStateBefore.Data));
        }

        private void HandleActionStateAfter(string data) {
            var actionStateAfter = JsonConvert.DeserializeObject<ActionStateAfter>(data)!;
            OnActionStateAfter?.Invoke(this, new ActionStateAfterEventArgs(actionStateAfter.Data));
        }

        private void HandlePackageState(string data) {
            var projectState = JsonConvert.DeserializeObject<PackageState>(data)!;
            OnPackageState?.Invoke(this, new PackageStateEventArgs(projectState.Data));
        }

        private void HandlePackageInfo(string data) {
            var packageInfo = JsonConvert.DeserializeObject<PackageInfo>(data)!;
            OnPackageInfo?.Invoke(this, new PackageInfoEventArgs(packageInfo.Data));
        }

        private void HandleProjectSaved(string data) {
            // Not needed: var projectSaved = JsonConvert.DeserializeObject<ProjectSaved>(data)!;
            OnProjectSaved?.Invoke(this, EventArgs.Empty);
        }

        private void HandleSceneSaved(string data) {
            // Not needed: var sceneSaved = JsonConvert.DeserializeObject<SceneSaved>(data)!;
            OnSceneSaved?.Invoke(this, EventArgs.Empty);
        }

        private void HandleProjectException(string data) {
            var projectException = JsonConvert.DeserializeObject<ProjectException>(data)!;
            OnProjectException?.Invoke(this, new ProjectExceptionEventArgs(projectException.Data));
        }

        private void HandleActionResult(string data) {
            var actionResult = JsonConvert.DeserializeObject<ActionResult>(data)!;
            OnActionResult?.Invoke(this, new ActionResultEventArgs(actionResult.Data));
        }

        private void HandleActionCancelled(string data) {
            // Not needed: var actionCancelled = JsonConvert.DeserializeObject<ActionCancelled>(data);
            OnActionCancelled?.Invoke(this, EventArgs.Empty);
        }

        private void HandleActionExecution(string data) {
            var actionExecution = JsonConvert.DeserializeObject<ActionExecution>(data)!;
            OnActionExecution?.Invoke(this, new ActionExecutionEventArgs(actionExecution.Data));
        }

        private void HandleRobotEef(string data) {
            var robotEef = JsonConvert.DeserializeObject<RobotEef>(data)!;
            OnRobotEefUpdated?.Invoke(this, new RobotEefUpdatedEventArgs(robotEef.Data));
        }

        private void HandleRobotJoints(string data) {
            var robotJoints = JsonConvert.DeserializeObject<RobotJoints>(data)!;
            OnRobotJointsUpdated?.Invoke(this, new RobotJointsUpdatedEventArgs(robotJoints.Data));
        }

        private void HandleOpenProject(string data) {
            var openProject = JsonConvert.DeserializeObject<OpenProject>(data)!;
            OnOpenProject?.Invoke(this, new OpenProjectEventArgs(openProject.Data));
        }

        private void HandleOpenScene(string data) {
            var openScene = JsonConvert.DeserializeObject<OpenScene>(data)!;
            OnOpenScene?.Invoke(this, new OpenSceneEventArgs(openScene.Data));
        }

        private void HandleProjectClosed(string data) {
            // Not needed: var projectClosed = JsonConvert.DeserializeObject<ProjectClosed>(data)!;
            OnProjectClosed?.Invoke(this, EventArgs.Empty);
        }

        private void HandleSceneClosed(string data) {
            // Not needed: var sceneClosed = JsonConvert.DeserializeObject<SceneClosed>(data)!;
            OnSceneClosed?.Invoke(this, EventArgs.Empty);
        }

        private void HandleProjectChanged(string data) {
            var projectChanged = JsonConvert.DeserializeObject<ProjectChanged>(data)!;

            switch(projectChanged.ChangeType) {
                case ProjectChanged.ChangeTypeEnum.Add:
                    throw new NotImplementedException("ProjectChanged add should never occur!");
                case ProjectChanged.ChangeTypeEnum.Remove:
                    OnProjectRemoved?.Invoke(this, new BareProjectEventArgs(projectChanged.Data));
                    break;
                case ProjectChanged.ChangeTypeEnum.Update:
                    throw new NotImplementedException("ProjectChanged update should never occur!");
                case ProjectChanged.ChangeTypeEnum.UpdateBase:
                    OnProjectBaseUpdated?.Invoke(this, new BareProjectEventArgs(projectChanged.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        private void HandleShowMainScreen(string data) {
            var showMainScreen = JsonConvert.DeserializeObject<ShowMainScreen>(data)!;
            OnShowMainScreen?.Invoke(this, new ShowMainScreenEventArgs(showMainScreen.Data));
        }

        private void HandleProjectParameterChanged(string data) {
            var projectParameterChanged = JsonConvert.DeserializeObject<ProjectParameterChanged>(data)!;

            switch(projectParameterChanged.ChangeType) {
                case ProjectParameterChanged.ChangeTypeEnum.Add:
                    OnProjectParameterAdded?.Invoke(this, new ProjectParameterEventArgs(projectParameterChanged.Data));
                    break;
                case ProjectParameterChanged.ChangeTypeEnum.Update:
                    OnProjectParameterUpdated?.Invoke(this, new ProjectParameterEventArgs(projectParameterChanged.Data));
                    break;
                case ProjectParameterChanged.ChangeTypeEnum.Remove:
                    OnProjectParameterRemoved?.Invoke(this, new ProjectParameterEventArgs(projectParameterChanged.Data));
                    break;
                case ProjectParameterChanged.ChangeTypeEnum.UpdateBase:
                    OnProjectParameterUpdated?.Invoke(this, new ProjectParameterEventArgs(projectParameterChanged.Data));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void HandleObjectUnlocked(string data) {
            var objectsUnlocked = JsonConvert.DeserializeObject<ObjectsUnlocked>(data)!;
            OnObjectsUnlocked?.Invoke(this, new ObjectsLockEventArgs(objectsUnlocked.Data));
        }

        private void HandleObjectLocked(string data) {
            var objectsLocked = JsonConvert.DeserializeObject<ObjectsLocked>(data)!;
            OnObjectsLocked?.Invoke(this, new ObjectsLockEventArgs(objectsLocked.Data));
        }

        private void HandleProcessState(string data) {
            var processState = JsonConvert.DeserializeObject<ProcessState>(data)!;
            OnProcessState?.Invoke(this, new ProcessStateEventArgs(processState.Data));
        }

        private void HandleHandTeachingMode(string data) {
            var handTeachingMode = JsonConvert.DeserializeObject<HandTeachingMode>(data)!;
            OnHandTeachingMode?.Invoke(this, new HandTeachingModeEventArgs(handTeachingMode.Data));
        }

        private void HandlePackageChanged(string data) {
            var packageChanged = JsonConvert.DeserializeObject<PackageChanged>(data)!;

            switch(packageChanged.ChangeType) {
                case PackageChanged.ChangeTypeEnum.Add:
                    OnPackageAdded?.Invoke(this, new PackageChangedEventArgs(packageChanged.Data));
                    break;
                case PackageChanged.ChangeTypeEnum.Update:
                    OnPackageUpdated?.Invoke(this, new PackageChangedEventArgs(packageChanged.Data));
                    break;
                case PackageChanged.ChangeTypeEnum.Remove:
                    OnPackageRemoved?.Invoke(this, new PackageChangedEventArgs(packageChanged.Data));
                    break;
                case PackageChanged.ChangeTypeEnum.UpdateBase:
                    throw new NotImplementedException("Package base update should never occur.");
                    break;
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        #endregion

        #region Request-Response Methods

        #region Request-Response Methods Without Coressponding Endpoint

        public async Task<DeleteObjectTypesResponse> DeleteObjectTypeAsync(string args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteObjectTypesRequest(id, "DeleteObjectTypes", new List<string>() { args }, isDryRun), id);
            return JsonConvert.DeserializeObject<DeleteObjectTypesResponse>(response)!;
        }

        #endregion

        #region Request-Response Methods With Direct Endpoint

        /// <summary>
        /// Sends a request to retrieve object types supported by the server.
        /// </summary>
        /// <returns>The response.</returns>
        public async Task<GetObjectTypesResponse> GetObjectTypesAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetObjectTypesRequest(id, "GetObjectTypes"), id);
            return JsonConvert.DeserializeObject<GetObjectTypesResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to retrieve list of available actions for an object type.
        /// </summary>
        /// <param name="args">The object type.</param>
        /// <returns>The response.</returns>
        public async Task<GetActionsResponse> GetActionsAsync(TypeArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetActionsRequest(id, "GetActions", args), id);
            return JsonConvert.DeserializeObject<GetActionsResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to save the current scene.
        /// </summary>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<SaveSceneResponse> SaveSceneAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SaveSceneRequest(id, "SaveScene", isDryRun), id);
            return JsonConvert.DeserializeObject<SaveSceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to save the current project.
        /// </summary>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<SaveProjectResponse> SaveProjectAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SaveProjectRequest(id, "SaveProject", isDryRun), id);
            return JsonConvert.DeserializeObject<SaveProjectResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to open a project.
        /// </summary>
        /// <param name="args">The project ID.</param>
        /// <returns>The response.</returns>
        public async Task<OpenProjectResponse> OpenProjectAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new OpenProjectRequest(id, "OpenProject", args), id);
            return JsonConvert.DeserializeObject<OpenProjectResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to run a package.
        /// </summary>
        /// <param name="args">The run parameters.</param>
        /// <returns>The response.</returns>
        public async Task<RunPackageResponse> RunPackageAsync(RunPackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RunPackageRequest(id, "RunPackage", args), id);
            return JsonConvert.DeserializeObject<RunPackageResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to build the current project into temporary package and run it.
        /// </summary>
        /// <param name="args">The debugging execution parameters.</param>
        /// <returns>The response.</returns>
        public async Task<TemporaryPackageResponse> TemporaryPackageAsync(TemporaryPackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new TemporaryPackageRequest(id, "TemporaryPackage", args), id);
            return JsonConvert.DeserializeObject<TemporaryPackageResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to terminate a running package.
        /// </summary>
        /// <returns>The response.</returns>
        public async Task<StopPackageResponse> StopPackageAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StopPackageRequest(id, "StopPackage"), id);
            return JsonConvert.DeserializeObject<StopPackageResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to pause a running package.
        /// </summary>
        /// <returns>The response.</returns>
        public async Task<PausePackageResponse> PausePackageAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new PausePackageRequest(id, "PausePackage"), id);
            return JsonConvert.DeserializeObject<PausePackageResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to resume a pause package.
        /// </summary>
        /// <returns>The response.</returns>
        public async Task<ResumePackageResponse> ResumePackageAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ResumePackageRequest(id, "ResumePackage"), id);
            return JsonConvert.DeserializeObject<ResumePackageResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to upload a package.
        /// </summary>
        /// <param name="args">The package ID and its data.</param>
        /// <returns>The response.</returns>
        public async Task<UploadPackageResponse> UploadPackageAsync(UploadPackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UploadPackageRequest(id, "UploadPackage", args), id);
            return JsonConvert.DeserializeObject<UploadPackageResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to rename a package.
        /// </summary>
        /// <param name="args">The package ID and new name.</param>
        /// <returns>The response.</returns>
        public async Task<RenamePackageResponse> RenamePackageAsync(RenamePackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenamePackageRequest(id, "RenamePackage", args), id);
            return JsonConvert.DeserializeObject<RenamePackageResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to retrieve a list of available packages.
        /// </summary>
        /// <returns>The response.</returns>
        public async Task<ListPackagesResponse> ListPackagesAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListPackagesRequest(id, "ListPackages"), id);
            return JsonConvert.DeserializeObject<ListPackagesResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update pose of action point using the robot's end effector.
        /// </summary>
        /// <param name="args">Action point ID and a robot.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateActionPointUsingRobotResponse> UpdateActionPointUsingRobotAsync(UpdateActionPointUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointUsingRobotRequest(id, "UpdateActionPointUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointUsingRobotResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update pose of action object.
        /// </summary>
        /// <param name="args">Action object ID and a new pose.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateObjectPoseResponse> UpdateObjectPoseAsync(UpdateObjectPoseRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectPoseRequest(id, "UpdateObjectPose", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateObjectPoseResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update pose of action object using the robot's end effector.
        /// </summary>
        /// <param name="args">Robot and pivot option.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateObjectPoseUsingRobotResponse> UpdateObjectPoseUsingRobotAsync(UpdateObjectPoseUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectPoseUsingRobotRequest(id, "UpdateObjectPoseUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateObjectPoseUsingRobotResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to define a new object type.
        /// </summary>
        /// <param name="args">The object type definition.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<NewObjectTypeResponse> NewObjectTypeAsync(ObjectTypeMeta args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new NewObjectTypeRequest(id, "NewObjectType", args, isDryRun), id);
            return JsonConvert.DeserializeObject<NewObjectTypeResponse>(response)!;
        }

        public async Task<ObjectAimingStartResponse> ObjectAimingStartAsync(ObjectAimingStartRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingStartRequest(id, "ObjectAimingStart", args, isDryRun), id);
            return JsonConvert.DeserializeObject<ObjectAimingStartResponse>(response)!;
        }

        public async Task<ObjectAimingAddPointResponse> ObjectAimingAddPointAsync(ObjectAimingAddPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingAddPointRequest(id, "ObjectAimingAddPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<ObjectAimingAddPointResponse>(response)!;
        }

        public async Task<ObjectAimingDoneResponse> ObjectAimingDoneAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingDoneRequest(id, "ObjectAimingDone", isDryRun), id);
            return JsonConvert.DeserializeObject<ObjectAimingDoneResponse>(response)!;
        }

        public async Task<ObjectAimingCancelResponse> ObjectAimingCancelAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingCancelRequest(id, "ObjectAimingCancel", isDryRun), id);
            return JsonConvert.DeserializeObject<ObjectAimingCancelResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to retrieve a list of available scenes.
        /// </summary>
        /// <returns>The response.</returns>
        public async Task<ListScenesResponse> ListScenesAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListScenesRequest(id, "ListScenes"), id);
            return JsonConvert.DeserializeObject<ListScenesResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to retrieve a list of available projects.
        /// </summary>
        /// <returns>The response.</returns>
        public async Task<ListProjectsResponse> ListProjectsAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListProjectsRequest(id, "ListProjects"), id);
            return JsonConvert.DeserializeObject<ListProjectsResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to add a new action object to a scene.
        /// </summary>
        /// <param name="args">The name, type, pose and parameters of the action object.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<AddObjectToSceneResponse> AddObjectToSceneAsync(AddObjectToSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddObjectToSceneRequest(id, "AddObjectToScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddObjectToSceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to remove an action object from scene.
        /// </summary>
        /// <param name="args">Action Object ID and if the removal should be forced.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RemoveFromSceneResponse> RemoveFromSceneAsync(RemoveFromSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveFromSceneRequest(id, "RemoveFromScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveFromSceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to open a scene.
        /// </summary>
        /// <param name="args">Scene ID.</param>
        /// <returns>THe response.</returns>
        public async Task<OpenSceneResponse> OpenSceneAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new OpenSceneRequest(id, "OpenScene", args), id);
            return JsonConvert.DeserializeObject<OpenSceneResponse>(response)!;
        }

        public async Task<ActionParamValuesResponse> ActionParamValuesAsync(ActionParamValuesRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ActionParamValuesRequest(id, "ActionParamValues", args), id);
            return JsonConvert.DeserializeObject<ActionParamValuesResponse>(response)!;
        }

        public async Task<ExecuteActionResponse> ExecuteActionAsync(ExecuteActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ExecuteActionRequest(id, "ExecuteAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<ExecuteActionResponse>(response)!;
        }

        public async Task<CancelActionResponse> CancelActionAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CancelActionRequest(id, "CancelAction"), id);
            return JsonConvert.DeserializeObject<CancelActionResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to retrieve information about the server (version, supported parameter types, and RPCs).
        /// </summary>
        /// <returns>THe response.</returns>
        public async Task<SystemInfoResponse> SystemInfoAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SystemInfoRequest(id, "SystemInfo"), id);
            return JsonConvert.DeserializeObject<SystemInfoResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to build a project into a package and upload it.
        /// </summary>
        /// <param name="args">The project ID and resulting package name.</param>
        /// <returns>The response.</returns>
        public async Task<BuildProjectResponse> BuildProjectAsync(BuildProjectRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new BuildProjectRequest(id, "BuildProject", args), id);
            return JsonConvert.DeserializeObject<BuildProjectResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to create a new scene.
        /// </summary>
        /// <param name="args">Name and description.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<NewSceneResponse> NewSceneAsync(NewSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new NewSceneRequest(id, "NewScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<NewSceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to delete a scene.
        /// </summary>
        /// <param name="args">ID of the scene.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<DeleteSceneResponse> DeleteSceneAsync(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteSceneRequest(id, "DeleteScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<DeleteSceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to rename a scene.
        /// </summary>
        /// <param name="args">ID and a new name of the scene.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RenameSceneResponse> RenameSceneAsync(RenameArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameSceneRequest(id, "RenameScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameSceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to rename an action object.
        /// </summary>
        /// <param name="args">The action object ID and new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RenameObjectResponse> RenameObjectAsync(RenameArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameObjectRequest(id, "RenameObject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameObjectResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to close a scene.
        /// </summary>
        /// <param name="args">Should the action be forced (e.g. in case of unsaved changes).</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<CloseSceneResponse> CloseSceneAsync(CloseSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CloseSceneRequest(id, "CloseScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<CloseSceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to retrieve existing project of a scene.
        /// </summary>
        /// <param name="args">Scene ID.</param>
        /// <returns>The response.</returns>
        public async Task<ProjectsWithSceneResponse> ProjectsWithSceneAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ProjectsWithSceneRequest(id, "ProjectsWithScene", args), id);
            return JsonConvert.DeserializeObject<ProjectsWithSceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to create a new project.
        /// </summary>
        /// <param name="args">Parent scene ID, project name, description, and if it should have its own logic.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<NewProjectResponse> NewProjectAsync(NewProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new NewProjectRequest(id, "NewProject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<NewProjectResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to delete a project.
        /// </summary>
        /// <param name="args">Project ID.</param>
        /// <returns>The response.</returns>
        public async Task<DeleteProjectResponse> DeleteProjectAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteProjectRequest(id, "DeleteProject", args), id);
            return JsonConvert.DeserializeObject<DeleteProjectResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to delete a package.
        /// </summary>
        /// <param name="args">Package ID.</param>
        /// <returns>The response.</returns>
        public async Task<DeletePackageResponse> DeletePackageAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeletePackageRequest(id, "DeletePackage", args), id);
            return JsonConvert.DeserializeObject<DeletePackageResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to create a new action point.
        /// </summary>
        /// <param name="args">Name, position, and optional parent.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<AddActionPointResponse> AddActionPointAsync(AddActionPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointRequest(id, "AddActionPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to create a new action point for robot's end effector.
        /// </summary>
        /// <param name="args">Robot (action object) ID, name, end effector ID, and optional arm ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<AddApUsingRobotResponse> AddApUsingRobotAsync(AddApUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddApUsingRobotRequest(id, "AddApUsingRobot", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddApUsingRobotResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update position of an action point.
        /// </summary>
        /// <param name="args">Action point ID and a new position.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateActionPointPositionResponse> UpdateActionPointPositionAsync(UpdateActionPointPositionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointPositionRequest(id, "UpdateActionPointPosition", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateActionPointPositionResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to change the parent of an action point.
        /// </summary>
        /// <param name="args">Action point ID and the ID of the new parent.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateActionPointParentResponse> UpdateActionPointParentAsync(UpdateActionPointParentRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointParentRequest(id, "UpdateActionPointParent", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateActionPointParentResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to create a new orientation of an action point.
        /// </summary>
        /// <param name="args">Action point ID, orientation and a name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<AddActionPointOrientationResponse> AddActionPointOrientationAsync(AddActionPointOrientationRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointOrientationRequest(id, "AddActionPointOrientation", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointOrientationResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to remove orientation from an action point.
        /// </summary>
        /// <param name="args">Orientation ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RemoveActionPointOrientationResponse> RemoveActionPointOrientationAsync(RemoveActionPointOrientationRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointOrientationRequest(id, "RemoveActionPointOrientation", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveActionPointOrientationResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update orientation of an action point.
        /// </summary>
        /// <param name="args">Orientation ID and a new orientation data.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateActionPointOrientationResponse> UpdateActionPointOrientationAsync(UpdateActionPointOrientationRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointOrientationRequest(id, "UpdateActionPointOrientation", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointOrientationResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to create a new orientation of robot end effector's action point.
        /// </summary>
        /// <param name="args">Action point ID, robot information, and a name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<AddActionPointOrientationUsingRobotResponse> AddActionPointOrientationUsingRobotAsync(AddActionPointOrientationUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointOrientationUsingRobotRequest(id, "AddActionPointOrientationUsingRobot", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointOrientationUsingRobotResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update orientation of robot end effector's action point.
        /// </summary>
        /// <param name="args">Orientation ID and robot information.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateActionPointOrientationUsingRobotResponse> UpdateActionPointOrientationUsingRobotAsync(UpdateActionPointOrientationUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointOrientationUsingRobotRequest(id, "UpdateActionPointOrientationUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointOrientationUsingRobotResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to create new joints of robot's (end effector) action point.
        /// </summary>
        /// <param name="args">Action point ID, robot/arm/end effector ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<AddActionPointJointsUsingRobotResponse> AddActionPointJointsUsingRobotAsync(AddActionPointJointsUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointJointsUsingRobotRequest(id, "AddActionPointJointsUsingRobot", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointJointsUsingRobotResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update joints of an action point.
        /// </summary>
        /// <param name="args">Joints ID and a list of joint names and values.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateActionPointJointsResponse> UpdateActionPointJointsAsync(UpdateActionPointJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointJointsRequest(id, "UpdateActionPointJoints", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointJointsResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update joints of robot's (end effector) action point.
        /// </summary>
        /// <param name="args">Joints ID.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateActionPointJointsUsingRobotResponse> UpdateActionPointJointsUsingRobotAsync(UpdateActionPointJointsUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointJointsUsingRobotRequest(id, "UpdateActionPointJointsUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointJointsUsingRobotResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to rename an action point.
        /// </summary>
        /// <param name="args">Action point ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RenameActionPointResponse> RenameActionPointAsync(RenameActionPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionPointRequest(id, "RenameActionPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameActionPointResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to rename joints of an action point.
        /// </summary>
        /// <param name="args">Joints ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RenameActionPointJointsResponse> RenameActionPointJointsAsync(RenameActionPointJointsRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionPointJointsRequest(id, "RenameActionPointJoints", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameActionPointJointsResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to rename orientation of an action point.
        /// </summary>
        /// <param name="args">Orientation ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RenameActionPointOrientationResponse> RenameActionPointOrientationAsync(RenameActionPointOrientationRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionPointOrientationRequest(id, "RenameActionPointOrientation", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameActionPointOrientationResponse>(response)!;
        }

        public async Task<MoveToActionPointResponse> MoveToActionPointAsync(MoveToActionPointRequestArgs args) {
            // TODO: Joints x Orientation
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new MoveToActionPointRequest(id, "MoveToActionPoint", args), id);
            return JsonConvert.DeserializeObject<MoveToActionPointResponse>(response)!;
        }

        public async Task<MoveToPoseResponse> MoveToPoseAsync(MoveToPoseRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new MoveToPoseRequest(id, "MoveToPose", args), id);
            return JsonConvert.DeserializeObject<MoveToPoseResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to remove joints of an action point.
        /// </summary>
        /// <param name="args">Joints ID.</param>
        /// <returns>The response.</returns>
        public async Task<RemoveActionPointJointsResponse> RemoveActionPointJointsAsync(RemoveActionPointJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointJointsRequest(id, "RemoveActionPointJoints", args), id);
            return JsonConvert.DeserializeObject<RemoveActionPointJointsResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to add an action to action point.
        /// </summary>
        /// <param name="args">Action point ID, name, action type, parameters, and flows.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<AddActionResponse> AddActionAsync(AddActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionRequest(id, "AddAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update an action.
        /// </summary>
        /// <param name="args">Action ID and updated parameters and flows.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateActionResponse> UpdateActionAsync(UpdateActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionRequest(id, "UpdateAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateActionResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to remove an action.
        /// </summary>
        /// <param name="args">Action ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RemoveActionResponse> RemoveActionAsync(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionRequest(id, "RemoveAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveActionResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to rename an action.
        /// </summary>
        /// <param name="args">Action ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RenameActionResponse> RenameActionAsync(RenameActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionRequest(id, "RenameAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameActionResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to add a logic item to the project.
        /// </summary>
        /// <param name="args">Start, end, and an optional condition for the logic item.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<AddLogicItemResponse> AddLogicItemAsync(AddLogicItemRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddLogicItemRequest(id, "AddLogicItem", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddLogicItemResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update a logic item.
        /// </summary>
        /// <param name="args">Logic item ID, start, end, and an optional condition for the logic item.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateLogicItemResponse> UpdateLogicItemAsync(UpdateLogicItemRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateLogicItemRequest(id, "UpdateLogicItem", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateLogicItemResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to remove a logic item.
        /// </summary>
        /// <param name="args">Logic item ID.</param>
        /// <returns>The response.</returns>
        public async Task<RemoveLogicItemResponse> RemoveLogicItemAsync(RemoveLogicItemRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveLogicItemRequest(id, "RemoveLogicItem", args), id);
            return JsonConvert.DeserializeObject<RemoveLogicItemResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to rename a project.
        /// </summary>
        /// <param name="args">Project ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RenameProjectResponse> RenameProjectAsync(RenameProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameProjectRequest(id, "RenameProject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameProjectResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to remove an action point.
        /// </summary>
        /// <param name="args">Action point ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RemoveActionPointResponse> RemoveActionPointAsync(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointRequest(id, "RemoveActionPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveActionPointResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to close a project.
        /// </summary>
        /// <param name="args">Should the action be forced (e.g. in case of unsaved changes).</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<CloseProjectResponse> CloseProjectAsync(CloseProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CloseProjectRequest(id, "CloseProject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<CloseProjectResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to get a pose of robot's end effector.
        /// </summary>
        /// <param name="args">Robot, end effector (and arm) ID.</param>
        /// <returns>The response.</returns>
        public async Task<GetEndEffectorPoseResponse> GetEndEffectorPoseAsync(GetEndEffectorPoseRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetEndEffectorPoseRequest(id, "GetEndEffectorPose", args), id);
            return JsonConvert.DeserializeObject<GetEndEffectorPoseResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to register/unregister itself for robot's end effector/joints update events.
        /// </summary>
        /// <param name="args">Robot ID, type (eef/joints), and if the request is registering or unregistering.</param>
        /// <returns>The response.</returns>
        public async Task<RegisterForRobotEventResponse> RegisterForRobotEventAsync(RegisterForRobotEventRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RegisterForRobotEventRequest(id, "RegisterForRobotEvent", args), id);
            return JsonConvert.DeserializeObject<RegisterForRobotEventResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to get information about a robot.
        /// </summary>
        /// <returns>The response.</returns>
        public async Task<GetRobotMetaResponse> GetRobotMetaAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetRobotMetaRequest(id, "GetRobotMeta"), id);
            return JsonConvert.DeserializeObject<GetRobotMetaResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to get a list of end effectors of a robot.
        /// </summary>
        /// <param name="args">Robot (and arm) ID.</param>
        /// <returns>The response.</returns>
        public async Task<GetEndEffectorsResponse> GetEndEffectorsAsync(GetEndEffectorsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetEndEffectorsRequest(id, "GetEndEffectors", args), id);
            return JsonConvert.DeserializeObject<GetEndEffectorsResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to get a list of arms of a robot.
        /// </summary>
        /// <param name="args">Robot ID.</param>
        /// <returns>The response.</returns>
        public async Task<GetRobotArmsResponse> GetRobotArmsAsync(GetRobotArmsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetRobotArmsRequest(id, "GetRobotArms", args), id);
            return JsonConvert.DeserializeObject<GetRobotArmsResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to start an offline scene.
        /// </summary>
        /// <remarks>All locks must be freed before starting a scene.</remarks>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<StartSceneResponse> StartSceneAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StartSceneRequest(id, "StartScene", isDryRun), id);
            return JsonConvert.DeserializeObject<StartSceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to stop an online scene.
        /// </summary>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<StopSceneResponse> StopSceneAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StopSceneRequest(id, "StopScene", isDryRun), id);
            return JsonConvert.DeserializeObject<StopSceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update action object's parameters.
        /// </summary>
        /// <param name="args">Action object ID and a list of Name-Type-Value parameters.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateObjectParametersResponse> UpdateObjectParametersAsync(UpdateObjectParametersRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectParametersRequest(id, "UpdateObjectParameters", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateObjectParametersResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to add an override to project's action object.
        /// </summary>
        /// <param name="args">Action object ID and a new parameter override.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<AddOverrideResponse> AddOverrideAsync(AddOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddOverrideRequest(id, "AddOverride", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddOverrideResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update an override of project's action object.
        /// </summary>
        /// <param name="args">Action object ID and the parameter override.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateOverrideResponse> UpdateOverrideAsync(UpdateOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateOverrideRequest(id, "UpdateOverride", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateOverrideResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to remove an override of project's action object.
        /// </summary>
        /// <param name="args">Action object ID and the parameter override.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<DeleteOverrideResponse> DeleteOverrideAsync(DeleteOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteOverrideRequest(id, "DeleteOverride", args, isDryRun), id);
            return JsonConvert.DeserializeObject<DeleteOverrideResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to calculate the inverse kinematics for a robot's end-effector and update it.
        /// </summary>
        /// <param name="args">Robot ID, end effector ID, target pose, optional start joints, collision avoidance flag, and optional arm ID.</param>
        /// <returns>The response.</returns>
        public async Task<InverseKinematicsResponse> InverseKinematicsAsync(InverseKinematicsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new InverseKinematicsRequest(id, "InverseKinematics", args), id);
            return JsonConvert.DeserializeObject<InverseKinematicsResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to calculate the forward kinematics for a robot's joints and update it.
        /// </summary>
        /// <param name="args">Robot ID, end effector ID, joint positions, and optional arm ID.</param>
        /// <returns>The response.</returns>
        public async Task<ForwardKinematicsResponse> ForwardKinematicsAsync(ForwardKinematicsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ForwardKinematicsRequest(id, "ForwardKinematics", args), id);
            return JsonConvert.DeserializeObject<ForwardKinematicsResponse>(response)!;
        }

        public async Task<CalibrateRobotResponse> CalibrateRobotAsync(CalibrateRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CalibrateRobotRequest(id, "CalibrateRobot", args), id);
            return JsonConvert.DeserializeObject<CalibrateRobotResponse>(response)!;
        }

        public async Task<CalibrateCameraResponse> CalibrateCameraAsync(CalibrateCameraRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CalibrateCameraRequest(id, "CalibrateCamera", args), id);
            return JsonConvert.DeserializeObject<CalibrateCameraResponse>(response)!;
        }

        public async Task<CameraColorImageResponse> GetCameraColorImageAsync(CameraColorImageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CameraColorImageRequest(id, "CameraColorImage", args), id);
            return JsonConvert.DeserializeObject<CameraColorImageResponse>(response)!;
        }

        public async Task<GetCameraPoseResponse> GetCameraPoseAsync(GetCameraPoseRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetCameraPoseRequest(id, "GetCameraPose", args), id);
            return JsonConvert.DeserializeObject<GetCameraPoseResponse>(response)!;
        }

        public async Task<MarkersCornersResponse> GetMarkersCornersAsync(MarkersCornersRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new MarkersCornersRequest(id, "MarkersCorners", args), id);
            return JsonConvert.DeserializeObject<MarkersCornersResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to lock an object for writing.
        /// </summary>
        /// <param name="args">Object ID and if the whole object subtree should be locked.</param>
        /// <returns>The response.</returns>
        public async Task<WriteLockResponse> WriteLockAsync(WriteLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new WriteLockRequest(id, "WriteLock", args), id);
            return JsonConvert.DeserializeObject<WriteLockResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to unlock an object for writing.
        /// </summary>
        /// <param name="args">Object ID.</param>
        /// <returns>The response.</returns>
        public async Task<WriteUnlockResponse> WriteUnlockAsync(WriteUnlockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new WriteUnlockRequest(id, "WriteUnlock", args), id);
            return JsonConvert.DeserializeObject<WriteUnlockResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to lock an object for reading.
        /// </summary>
        /// <param name="args">Object ID.</param>
        /// <returns>The response.</returns>
        [Obsolete("Current ARCOR2 implementation (1.5.0) does not have a real use for client read-locking.")]
        public async Task<ReadLockResponse> ReadLockAsync(ReadLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ReadLockRequest(id, "ReadLock", args), id);
            return JsonConvert.DeserializeObject<ReadLockResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to unlock an object for reading.
        /// </summary>
        /// <param name="args">Object ID.</param>
        /// <returns>The response.</returns>
        [Obsolete("Current ARCOR2 implementation (1.5.0) does not have a real use for client read-locking.")]
        public async Task<ReadUnlockResponse> ReadUnlockAsync(ReadUnlockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ReadUnlockRequest(id, "ReadUnlock", args), id);
            return JsonConvert.DeserializeObject<ReadUnlockResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update lock type (object/tree).
        /// </summary>
        /// <param name="args">Object ID and new lock type.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateLockResponse> UpdateLockAsync(UpdateLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateLockRequest(id, "UpdateLock", args), id);
            return JsonConvert.DeserializeObject<UpdateLockResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to toggle hand teaching mode.
        /// </summary>
        /// <param name="args">Robot ID, toggle.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<HandTeachingModeResponse> HandTeachingModeAsync(HandTeachingModeRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new HandTeachingModeRequest(id, "HandTeachingMode", args, isDryRun), id);
            return JsonConvert.DeserializeObject<HandTeachingModeResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to duplicate an action point.
        /// </summary>
        /// <param name="args">Object ID and boolean if the object tree should be locked.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<CopyActionPointResponse> CopyActionPointAsync(CopyActionPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CopyActionPointRequest(id, "CopyActionPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<CopyActionPointResponse>(response)!;

        }

        public async Task<StepRobotEefResponse> StepRobotEefAsync(StepRobotEefRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StepRobotEefRequest(id, "StepRobotEef", args, isDryRun), id);
            return JsonConvert.DeserializeObject<StepRobotEefResponse>(response)!;
        }


        /// <summary>
        /// Sends a request to set the end effector perpendicular to the world frame.
        /// </summary>
        /// <param name="args">Robot ID, end effector ID, safety flag, speed, linear movement flag, and optional arm ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The result.</returns>
        public async Task<SetEefPerpendicularToWorldResponse> SetEefPerpendicularToWorldAsync(SetEefPerpendicularToWorldRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SetEefPerpendicularToWorldRequest(id, "SetEefPerpendicularToWorld", args, isDryRun), id);
            return JsonConvert.DeserializeObject<SetEefPerpendicularToWorldResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to register user for this session.
        /// </summary>
        /// <param name="args">Username.</param>
        /// <returns>The response.</returns>
        public async Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RegisterUserRequest(id, "RegisterUser", args), id);
            return JsonConvert.DeserializeObject<RegisterUserResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to add a project parameter.
        /// </summary>
        /// <param name="args">Parameter in Name-Type-Value format.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<AddProjectParameterResponse> AddProjectParameterAsync(AddProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddProjectParameterRequest(id, "AddProjectParameter", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddProjectParameterResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update a value of project parameter.
        /// </summary>
        /// <param name="args">Project parameter ID and a new value.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateProjectParameterResponse> UpdateProjectParameterAsync(UpdateProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateProjectParameterRequest(id, "UpdateProjectParameter", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateProjectParameterResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to remove project parameter.
        /// </summary>
        /// <param name="args">Project parameter ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<RemoveProjectParameterResponse> RemoveProjectParameterAsync(RemoveProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveProjectParameterRequest(id, "RemoveProjectParameter", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveProjectParameterResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update object model of an object type.
        /// </summary>
        /// <param name="args">Object type ID and the object model.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateObjectModelResponse> UpdateObjectModelAsync(UpdateObjectModelRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectModelRequest(id, "UpdateObjectModel", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateObjectModelResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to add a virtual collision object to a scene.
        /// </summary>
        /// <param name="args">Name, pose, and the object.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<AddVirtualCollisionObjectToSceneResponse> AddVirtualCollisionObjectToSceneAsync(AddVirtualCollisionObjectToSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddVirtualCollisionObjectToSceneRequest(id, "AddVirtualCollisionObjectToScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddVirtualCollisionObjectToSceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to duplicate a scene.
        /// </summary>
        /// <param name="args">Scene ID and a new name.</param>
        /// <returns>The response.</returns>
        public async Task<CopySceneResponse> DuplicateSceneAsync(CopySceneRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CopySceneRequest(id, "CopyScene", args), id);
            return JsonConvert.DeserializeObject<CopySceneResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to duplicate a scene.
        /// </summary>
        /// <param name="args">Project ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        public async Task<CopyProjectResponse> DuplicateProjectAsync(CopyProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CopyProjectRequest(id, "CopyProject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<CopyProjectResponse>(response)!;
        }

        public async Task<StepActionResponse> StepActionAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StepActionRequest(id, "StepAction"));
            return JsonConvert.DeserializeObject<StepActionResponse>(response)!;
        }

        public async Task<CameraColorParametersResponse> CameraColorParametersAsync(CameraColorParametersRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CameraColorParametersRequest(id, "CameraColorParameters", args), id);
            return JsonConvert.DeserializeObject<CameraColorParametersResponse>(response)!;
        }

        public async Task<GetGrippersResponse> GetGrippersAsync(GetGrippersRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetGrippersRequest(id, "GetGrippers", args), id);
            return JsonConvert.DeserializeObject<GetGrippersResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to get a project using ID.
        /// </summary>
        /// <param name="args">Project ID.</param>
        /// <returns>The response.</returns>
        public async Task<GetProjectResponse> GetProjectAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetProjectRequest(id, "GetProject", args), id);
            return JsonConvert.DeserializeObject<GetProjectResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to get joints of a robot.
        /// </summary>
        /// <param name="args">Robot (and arm) ID.</param>
        /// <returns>The response.</returns>
        public async Task<GetRobotJointsResponse> GetRobotJointsAsync(GetRobotJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetRobotJointsRequest(id, "GetRobotJoints", args), id);
            return JsonConvert.DeserializeObject<GetRobotJointsResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to get a scene using ID.
        /// </summary>
        /// <param name="args">Scene ID.</param>
        /// <returns>The response.</returns>
        public async Task<GetSceneResponse> GetSceneAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetSceneRequest(id, "GetScene", args), id);
            return JsonConvert.DeserializeObject<GetSceneResponse>(response)!;
        }

        public async Task<GetSuctionsResponse> GetSuctionsAsync(GetSuctionsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetSuctionsRequest(id, "GetSuctions", args), id);
            return JsonConvert.DeserializeObject<GetSuctionsResponse>(response)!;
        }

        public async Task<MoveToJointsResponse> MoveToJointsAsync(MoveToJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new MoveToJointsRequest(id, "MoveToJoints", args), id);
            return JsonConvert.DeserializeObject<MoveToJointsResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to get scene IDs that use specified object type.
        /// </summary>
        /// <param name="args">Object type ID.</param>
        /// <returns>The response.</returns>
        public async Task<ObjectTypeUsageResponse> ObjectTypeUsageAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectTypeUsageRequest(id, "ObjectTypeUsage", args), id);
            return JsonConvert.DeserializeObject<ObjectTypeUsageResponse>(response)!;
        }

        public async Task<SceneObjectUsageResponse> SceneObjectUsageAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SceneObjectUsageRequest(id, "SceneObjectUsage", args), id);
            return JsonConvert.DeserializeObject<SceneObjectUsageResponse>(response)!;
        }

        public async Task<StopRobotResponse> StopRobotAsync(StopRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StopRobotRequest(id, "StopRobot", args), id);
            return JsonConvert.DeserializeObject<StopRobotResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update project's description.
        /// </summary>
        /// <param name="args">Project ID and new description.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateProjectDescriptionResponse> UpdateProjectDescriptionAsync(UpdateProjectDescriptionRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateProjectDescriptionRequest(id, "UpdateProjectDescription", args), id);
            return JsonConvert.DeserializeObject<UpdateProjectDescriptionResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update if project contains logic.
        /// </summary>
        /// <param name="args">Project ID and boolean value indicating if project should have logic.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateProjectHasLogicResponse> UpdateProjectHasLogicAsync(UpdateProjectHasLogicRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateProjectHasLogicRequest(id, "UpdateProjectHasLogic", args), id);
            return JsonConvert.DeserializeObject<UpdateProjectHasLogicResponse>(response)!;
        }

        /// <summary>
        /// Sends a request to update scene's description.
        /// </summary>
        /// <param name="args">Scene ID and new description.</param>
        /// <returns>The response.</returns>
        public async Task<UpdateSceneDescriptionResponse> UpdateSceneDescriptionAsync(UpdateSceneDescriptionRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateSceneDescriptionRequest(id, "UpdateSceneDescription", args), id);
            return JsonConvert.DeserializeObject<UpdateSceneDescriptionResponse>(response)!;
        }

        #endregion

        #endregion
    }
}