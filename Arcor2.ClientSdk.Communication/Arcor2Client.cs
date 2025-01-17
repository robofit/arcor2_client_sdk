﻿using System;
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
        private int requestId = 0;

        /// <summary>
        /// The URI of the current server.
        /// </summary>
        public Uri? Uri { get; private set; }

        #region Callback Event Definitions
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
        public event EventHandler<RobotJointsEventArgs>? OnJointsAdded;
        /// <summary>
        /// Raised when action point joints are updated.
        /// </summary>
        public event EventHandler<RobotJointsEventArgs>? OnJointsUpdated;
        /// <summary>
        /// Raised when action point joints base is updated (e.g. rename).
        /// </summary>
        public event EventHandler<RobotJointsEventArgs>? OnJointsBaseUpdated;
        /// <summary>
        /// Raised when action point joints are removed.
        /// </summary>
        public event EventHandler<RobotJointsEventArgs>? OnJointsRemoved;

        public event EventHandler<ObjectTypesEventArgs>? OnObjectTypeRemoved;
        public event EventHandler<ObjectTypesEventArgs>? OnObjectTypeAdded;
        public event EventHandler<ObjectTypesEventArgs>? OnObjectTypeUpdated;

        public event EventHandler<RobotMoveToPoseEventArgs>? OnRobotMoveToPoseEvent;
        public event EventHandler<RobotMoveToJointsEventArgs>? OnRobotMoveToJointsEvent;
        public event EventHandler<RobotMoveToActionPointOrientationEventArgs>? OnRobotMoveToActionPointOrientationEvent;
        public event EventHandler<RobotMoveToActionPointJointsEventArgs>? OnRobotMoveToActionPointJointsEvent;

        public event EventHandler<RobotEefUpdatedEventArgs>? OnRobotEefUpdated;
        public event EventHandler<RobotJointsUpdatedEventArgs>? OnRobotJointsUpdated;

        public event EventHandler? OnProjectSaved;
        public event EventHandler<OpenProjectEventArgs>? OnOpenProject;
        public event EventHandler? OnProjectClosed;

        public event EventHandler<ProjectExceptionEventArgs>? OnProjectException;
        public event EventHandler<BareProjectEventArgs>? OnProjectRemoved;
        public event EventHandler<BareProjectEventArgs>? OnProjectBaseUpdated;

        public event EventHandler<ProjectParameterEventArgs>? OnProjectParameterAdded;
        public event EventHandler<ProjectParameterEventArgs>? OnProjectParameterUpdated;
        public event EventHandler<ProjectParameterEventArgs>? OnProjectParameterRemoved;

        public event EventHandler? OnSceneSaved;
        public event EventHandler<OpenSceneEventArgs>? OnOpenScene;
        public event EventHandler? OnSceneClosed;

        public event EventHandler<ActionExecutionEventArgs>? OnActionExecution;
        public event EventHandler? OnActionCancelled;
        public event EventHandler<ActionResultEventArgs>? OnActionResult;

        public event EventHandler<ShowMainScreenEventArgs>? OnShowMainScreen;

        public event EventHandler<ObjectsLockEventArgs>? OnObjectsLocked;
        public event EventHandler<ObjectsLockEventArgs>? OnObjectsUnlocked;

        public event EventHandler<ProcessStateEventArgs>? OnProcessState;

        public event EventHandler<PackageInfoEventArgs>? OnPackageInfo;
        [Obsolete("Officially deprecated in ARCOR2 version 0.10.0.")]
        public event EventHandler<PackageStateEventArgs>? OnPackageState;
        public event EventHandler<ActionStateBeforeEventArgs>? OnActionStateBefore;
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
                        HandleChangedObjectTypesEvent(data);
                        break;
                    case "RobotMoveToActionPointOrientation":
                        HandleRobotMoveToActionPointOrientation(data);
                        break;
                    case "RobotMoveToPose":
                        HandleRobotMoveToPoseEvent(data);
                        break;
                    case "RobotMoveToJoints":
                        HandleRobotMoveToJointsEvent(data);
                        break;
                    case "RobotMoveToActionPointJoints":
                        HandleRobotMoveToActionPointJointsEvent(data);
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
                    default:
                        // We probably do not want this to be fatal, due to the real possibility of some minor version mismatch
                        ConnectionError?.Invoke(this, new NotImplementedException($"Unknown event received: {dispatch.@event}"));
                        break;
                }
            }
        }

        #region Server Event Handlers

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
                    OnJointsAdded?.Invoke(this, new RobotJointsEventArgs(jointsChanged.Data, jointsChanged.ParentId));
                    break;
                case JointsChanged.ChangeTypeEnum.Remove:
                    OnJointsRemoved?.Invoke(this, new RobotJointsEventArgs(jointsChanged.Data));
                    break;
                case JointsChanged.ChangeTypeEnum.Update:
                    OnJointsUpdated?.Invoke(this, new RobotJointsEventArgs(jointsChanged.Data));
                    break;
                case JointsChanged.ChangeTypeEnum.UpdateBase:
                    OnJointsBaseUpdated?.Invoke(this, new RobotJointsEventArgs(jointsChanged.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown change type.");
            }
        }

        private void HandleChangedObjectTypesEvent(string data) {
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
            OnRobotMoveToActionPointOrientationEvent?.Invoke(this, new RobotMoveToActionPointOrientationEventArgs(robotMoveToActionPointOrientation.Data));
        }

        private void HandleRobotMoveToPoseEvent(string data) {
            var robotMoveToPose = JsonConvert.DeserializeObject<RobotMoveToPose>(data)!;
            OnRobotMoveToPoseEvent?.Invoke(this, new RobotMoveToPoseEventArgs(robotMoveToPose.Data));
        }

        private void HandleRobotMoveToJointsEvent(string data) {
            var robotMoveToJoints = JsonConvert.DeserializeObject<RobotMoveToJoints>(data)!;
            OnRobotMoveToJointsEvent?.Invoke(this, new RobotMoveToJointsEventArgs(robotMoveToJoints.Data));
        }

        private void HandleRobotMoveToActionPointJointsEvent(string data) {
            var robotMoveToActionPointJoints = JsonConvert.DeserializeObject<RobotMoveToActionPointJoints>(data)!;
            OnRobotMoveToActionPointJointsEvent?.Invoke(this, new RobotMoveToActionPointJointsEventArgs(robotMoveToActionPointJoints.Data));
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

        public async Task<GetObjectTypesResponse> GetObjectTypesAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetObjectTypesRequest(id, "GetObjectTypes"), id);
            return JsonConvert.DeserializeObject<GetObjectTypesResponse>(response)!;
        }


        public async Task<GetActionsResponse> GetActionsAsync(TypeArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetActionsRequest(id, "GetActions", args), id);
            return JsonConvert.DeserializeObject<GetActionsResponse>(response)!;
        }

        public async Task<SaveSceneResponse> SaveSceneAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SaveSceneRequest(id, "SaveScene", isDryRun), id);
            return JsonConvert.DeserializeObject<SaveSceneResponse>(response)!;
        }

        public async Task<SaveProjectResponse> SaveProjectAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SaveProjectRequest(id, "SaveProject", isDryRun), id);
            return JsonConvert.DeserializeObject<SaveProjectResponse>(response)!;
        }

        public async Task<OpenProjectResponse> OpenProjectAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new OpenProjectRequest(id, "OpenProject", args), id);
            return JsonConvert.DeserializeObject<OpenProjectResponse>(response)!;
        }

        public async Task<RunPackageResponse> RunPackageAsync(RunPackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RunPackageRequest(id, "RunPackage", args), id);
            return JsonConvert.DeserializeObject<RunPackageResponse>(response)!;
        }

        public async Task<TemporaryPackageResponse> TemporaryPackageAsync(TemporaryPackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new TemporaryPackageRequest(id, "TemporaryPackage", args), id);
            return JsonConvert.DeserializeObject<TemporaryPackageResponse>(response)!;
        }

        public async Task<StopPackageResponse> StopPackageAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StopPackageRequest(id, "StopPackage"), id);
            return JsonConvert.DeserializeObject<StopPackageResponse>(response)!;
        }

        public async Task<PausePackageResponse> PausePackageAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new PausePackageRequest(id, "PausePackage"), id);
            return JsonConvert.DeserializeObject<PausePackageResponse>(response)!;
        }

        public async Task<ResumePackageResponse> ResumePackageAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ResumePackageRequest(id, "ResumePackage"), id);
            return JsonConvert.DeserializeObject<ResumePackageResponse>(response)!;
        }

        public async Task<UpdateActionPointUsingRobotResponse> UpdateActionPointUsingRobotAsync(UpdateActionPointUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointUsingRobotRequest(id, "UpdateActionPointUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointUsingRobotResponse>(response)!;
        }

        public async Task<UpdateObjectPoseResponse> UpdateObjectPoseAsync(UpdateObjectPoseRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectPoseRequest(id, "UpdateObjectPose", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateObjectPoseResponse>(response)!;
        }

        public async Task<UpdateObjectPoseUsingRobotResponse> UpdateObjectPoseUsingRobotAsync(UpdateObjectPoseUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectPoseUsingRobotRequest(id, "UpdateObjectPoseUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateObjectPoseUsingRobotResponse>(response)!;
        }

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

        public async Task<ListScenesResponse> ListScenesAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListScenesRequest(id, "ListScenes"), id);
            return JsonConvert.DeserializeObject<ListScenesResponse>(response)!;
        }

        public async Task<ListProjectsResponse> ListProjectsAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListProjectsRequest(id, "ListProjects"), id);
            return JsonConvert.DeserializeObject<ListProjectsResponse>(response)!;
        }

        public async Task<ListPackagesResponse> ListPackagesAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListPackagesRequest(id, "ListPackages"), id);
            return JsonConvert.DeserializeObject<ListPackagesResponse>(response)!;
        }

        public async Task<AddObjectToSceneResponse> AddObjectToSceneAsync(AddObjectToSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddObjectToSceneRequest(id, "AddObjectToScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddObjectToSceneResponse>(response)!;
        }

        public async Task<RemoveFromSceneResponse> RemoveFromSceneAsync(RemoveFromSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveFromSceneRequest(id, "RemoveFromScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveFromSceneResponse>(response)!;
        }

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

        public async Task<SystemInfoResponse> SystemInfoAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SystemInfoRequest(id, "SystemInfo"), id);
            return JsonConvert.DeserializeObject<SystemInfoResponse>(response)!;
        }

        public async Task<BuildProjectResponse> BuildProjectAsync(BuildProjectRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new BuildProjectRequest(id, "BuildProject", args), id);
            return JsonConvert.DeserializeObject<BuildProjectResponse>(response)!;
        }

        public async Task<NewSceneResponse> NewSceneAsync(NewSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new NewSceneRequest(id, "NewScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<NewSceneResponse>(response)!;
        }

        public async Task<DeleteSceneResponse> DeleteSceneAsync(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteSceneRequest(id, "DeleteScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<DeleteSceneResponse>(response)!;
        }

        public async Task<RenameSceneResponse> RenameSceneAsync(RenameArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameSceneRequest(id, "RenameScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameSceneResponse>(response)!;
        }

        public async Task<RenameObjectResponse> RenameObjectAsync(RenameArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameObjectRequest(id, "RenameObject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameObjectResponse>(response)!;
        }

        public async Task<CloseSceneResponse> CloseSceneAsync(CloseSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CloseSceneRequest(id, "CloseScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<CloseSceneResponse>(response)!;
        }

        public async Task<ProjectsWithSceneResponse> ProjectsWithSceneAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ProjectsWithSceneRequest(id, "ProjectsWithScene", args), id);
            return JsonConvert.DeserializeObject<ProjectsWithSceneResponse>(response)!;
        }

        public async Task<NewProjectResponse> NewProjectAsync(NewProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new NewProjectRequest(id, "NewProject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<NewProjectResponse>(response)!;
        }
        public async Task<DeleteProjectResponse> DeleteProjectAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteProjectRequest(id, "DeleteProject", args), id);
            return JsonConvert.DeserializeObject<DeleteProjectResponse>(response)!;
        }

        public async Task<DeletePackageResponse> DeletePackageAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeletePackageRequest(id, "DeletePackage", args), id);
            return JsonConvert.DeserializeObject<DeletePackageResponse>(response)!;
        }

        public async Task<AddActionPointResponse> AddActionPointAsync(AddActionPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointRequest(id, "AddActionPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointResponse>(response)!;
        }

        public async Task<AddApUsingRobotResponse> AddApUsingRobotAsync(AddApUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddApUsingRobotRequest(id, "AddApUsingRobot", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddApUsingRobotResponse>(response)!;
        }

        public async Task<UpdateActionPointPositionResponse> UpdateActionPointPositionAsync(UpdateActionPointPositionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointPositionRequest(id, "UpdateActionPointPosition", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateActionPointPositionResponse>(response)!;
        }

        public async Task<UpdateActionPointParentResponse> UpdateActionPointParentAsync(UpdateActionPointParentRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointParentRequest(id, "UpdateActionPointParent", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateActionPointParentResponse>(response)!;
        }

        public async Task<AddActionPointOrientationResponse> AddActionPointOrientationAsync(AddActionPointOrientationRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointOrientationRequest(id, "AddActionPointOrientation", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointOrientationResponse>(response)!;
        }

        public async Task<RemoveActionPointOrientationResponse> RemoveActionPointOrientationAsync(RemoveActionPointOrientationRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointOrientationRequest(id, "RemoveActionPointOrientation", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveActionPointOrientationResponse>(response)!;
        }

        public async Task<UpdateActionPointOrientationResponse> UpdateActionPointOrientationAsync(UpdateActionPointOrientationRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointOrientationRequest(id, "UpdateActionPointOrientation", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointOrientationResponse>(response)!;
        }

        public async Task<AddActionPointOrientationUsingRobotResponse> AddActionPointOrientationUsingRobotAsync(AddActionPointOrientationUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointOrientationUsingRobotRequest(id, "AddActionPointOrientationUsingRobot", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointOrientationUsingRobotResponse>(response)!;
        }

        public async Task<UpdateActionPointOrientationUsingRobotResponse> UpdateActionPointOrientationUsingRobotAsync(UpdateActionPointOrientationUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointOrientationUsingRobotRequest(id, "UpdateActionPointOrientationUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointOrientationUsingRobotResponse>(response)!;
        }

        public async Task<AddActionPointJointsUsingRobotResponse> AddActionPointJointsUsingRobotAsync(AddActionPointJointsUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointJointsUsingRobotRequest(id, "AddActionPointJointsUsingRobot", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointJointsUsingRobotResponse>(response)!;
        }

        public async Task<UpdateActionPointJointsResponse> UpdateActionPointJointsAsync(UpdateActionPointJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointJointsRequest(id, "UpdateActionPointJoints", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointJointsResponse>(response)!;
        }

        public async Task<UpdateActionPointJointsUsingRobotResponse> UpdateActionPointJointsUsingRobotAsync(UpdateActionPointJointsUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointJointsUsingRobotRequest(id, "UpdateActionPointJointsUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointJointsUsingRobotResponse>(response)!;
        }

        public async Task<RenameActionPointResponse> RenameActionPointAsync(RenameActionPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionPointRequest(id, "RenameActionPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameActionPointResponse>(response)!;
        }

        public async Task<RenameActionPointJointsResponse> RenameActionPointJointsAsync(RenameActionPointJointsRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionPointJointsRequest(id, "RenameActionPointJoints", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameActionPointJointsResponse>(response)!;
        }

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

        public async Task<RemoveActionPointJointsResponse> RemoveActionPointJointsAsync(RemoveActionPointJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointJointsRequest(id, "RemoveActionPointJoints", args), id);
            return JsonConvert.DeserializeObject<RemoveActionPointJointsResponse>(response)!;
        }

        public async Task<AddActionResponse> AddActionAsync(AddActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionRequest(id, "AddAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionResponse>(response)!;
        }

        public async Task<UpdateActionResponse> UpdateActionAsync(UpdateActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionRequest(id, "UpdateAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateActionResponse>(response)!;
        }

        public async Task<RemoveActionResponse> RemoveActionAsync(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionRequest(id, "RemoveAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveActionResponse>(response)!;
        }

        public async Task<RenameActionResponse> RenameActionAsync(RenameActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionRequest(id, "RenameAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameActionResponse>(response)!;
        }

        public async Task<AddLogicItemResponse> AddLogicItemAsync(AddLogicItemRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddLogicItemRequest(id, "AddLogicItem", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddLogicItemResponse>(response)!;
        }

        public async Task<UpdateLogicItemResponse> UpdateLogicItemAsync(UpdateLogicItemRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateLogicItemRequest(id, "UpdateLogicItem", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateLogicItemResponse>(response)!;
        }

        public async Task<RemoveLogicItemResponse> RemoveLogicItemAsync(RemoveLogicItemRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveLogicItemRequest(id, "RemoveLogicItem", args), id);
            return JsonConvert.DeserializeObject<RemoveLogicItemResponse>(response)!;
        }

        public async Task<RenameProjectResponse> RenameProjectAsync(RenameProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameProjectRequest(id, "RenameProject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameProjectResponse>(response)!;
        }

        public async Task<RenamePackageResponse> RenamePackageAsync(RenamePackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenamePackageRequest(id, "RenamePackage", args), id);
            return JsonConvert.DeserializeObject<RenamePackageResponse>(response)!;
        }

        public async Task<RemoveActionPointResponse> RemoveActionPointAsync(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointRequest(id, "RemoveActionPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveActionPointResponse>(response)!;
        }

        public async Task<CloseProjectResponse> CloseProjectAsync(CloseProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CloseProjectRequest(id, "CloseProject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<CloseProjectResponse>(response)!;
        }

        public async Task<GetEndEffectorPoseResponse> GetEndEffectorPoseAsync(GetEndEffectorPoseRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetEndEffectorPoseRequest(id, "GetEndEffectorPose", args), id);
            return JsonConvert.DeserializeObject<GetEndEffectorPoseResponse>(response)!;
        }

        public async Task<RegisterForRobotEventResponse> RegisterForRobotEventAsync(RegisterForRobotEventRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RegisterForRobotEventRequest(id, "RegisterForRobotEvent", args), id);
            return JsonConvert.DeserializeObject<RegisterForRobotEventResponse>(response)!;
        }

        public async Task<GetRobotMetaResponse> GetRobotMetaAsync() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetRobotMetaRequest(id, "GetRobotMeta"), id);
            return JsonConvert.DeserializeObject<GetRobotMetaResponse>(response)!;
        }

        public async Task<GetEndEffectorsResponse> GetEndEffectorsAsync(GetEndEffectorsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetEndEffectorsRequest(id, "GetEndEffectors", args), id);
            return JsonConvert.DeserializeObject<GetEndEffectorsResponse>(response)!;
        }

        public async Task<GetRobotArmsResponse> GetRobotArmsAsync(GetRobotArmsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetRobotArmsRequest(id, "GetRobotArms", args), id);
            return JsonConvert.DeserializeObject<GetRobotArmsResponse>(response)!;
        }

        public async Task<StartSceneResponse> StartSceneAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StartSceneRequest(id, "StartScene", isDryRun), id);
            return JsonConvert.DeserializeObject<StartSceneResponse>(response)!;
        }

        public async Task<StopSceneResponse> StopSceneAsync(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StopSceneRequest(id, "StopScene", isDryRun), id);
            return JsonConvert.DeserializeObject<StopSceneResponse>(response)!;
        }

        public async Task<UpdateObjectParametersResponse> UpdateObjectParametersAsync(UpdateObjectParametersRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectParametersRequest(id, "UpdateObjectParameters", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateObjectParametersResponse>(response)!;
        }

        public async Task<AddOverrideResponse> AddOverrideAsync(AddOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddOverrideRequest(id, "AddOverride", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddOverrideResponse>(response)!;
        }

        public async Task<UpdateOverrideResponse> UpdateOverrideAsync(UpdateOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateOverrideRequest(id, "UpdateOverride", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateOverrideResponse>(response)!;
        }

        public async Task<DeleteOverrideResponse> DeleteOverrideAsync(DeleteOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteOverrideRequest(id, "DeleteOverride", args, isDryRun), id);
            return JsonConvert.DeserializeObject<DeleteOverrideResponse>(response)!;
        }

        public async Task<InverseKinematicsResponse> InverseKinematicsAsync(InverseKinematicsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new InverseKinematicsRequest(id, "InverseKinematics", args), id);
            return JsonConvert.DeserializeObject<InverseKinematicsResponse>(response)!;
        }

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

        public async Task<WriteLockResponse> WriteLockAsync(WriteLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new WriteLockRequest(id, "WriteLock", args), id);
            return JsonConvert.DeserializeObject<WriteLockResponse>(response)!;
        }

        public async Task<WriteUnlockResponse> WriteUnlockAsync(WriteUnlockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new WriteUnlockRequest(id, "WriteUnlock", args), id);
            return JsonConvert.DeserializeObject<WriteUnlockResponse>(response)!;
        }

        public async Task<ReadLockResponse> ReadLockAsync(ReadLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ReadLockRequest(id, "ReadLock", args), id);
            return JsonConvert.DeserializeObject<ReadLockResponse>(response)!;
        }

        public async Task<ReadUnlockResponse> ReadUnlockAsync(ReadUnlockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ReadUnlockRequest(id, "ReadUnlock", args), id);
            return JsonConvert.DeserializeObject<ReadUnlockResponse>(response)!;
        }

        public async Task<UpdateLockResponse> UpdateLockAsync(UpdateLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateLockRequest(id, "UpdateLock", args), id);
            return JsonConvert.DeserializeObject<UpdateLockResponse>(response)!;
        }

        public async Task<HandTeachingModeResponse> HandTeachingModeAsync(HandTeachingModeRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new HandTeachingModeRequest(id, "HandTeachingMode", args, isDryRun), id);
            return JsonConvert.DeserializeObject<HandTeachingModeResponse>(response)!;
        }

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

        public async Task<SetEefPerpendicularToWorldResponse> SetEefPerpendicularToWorldAsync(SetEefPerpendicularToWorldRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SetEefPerpendicularToWorldRequest(id, "SetEefPerpendicularToWorld", args, isDryRun), id);
            return JsonConvert.DeserializeObject<SetEefPerpendicularToWorldResponse>(response)!;
        }

        public async Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RegisterUserRequest(id, "RegisterUser", args), id);
            return JsonConvert.DeserializeObject<RegisterUserResponse>(response)!;
        }

        public async Task<AddProjectParameterResponse> AddProjectParameterAsync(AddProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddProjectParameterRequest(id, "AddProjectParameter", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddProjectParameterResponse>(response)!;
        }

        public async Task<UpdateProjectParameterResponse> UpdateProjectParameterAsync(UpdateProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateProjectParameterRequest(id, "UpdateProjectParameter", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateProjectParameterResponse>(response)!;
        }

        public async Task<RemoveProjectParameterResponse> RemoveProjectParameterAsync(RemoveProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveProjectParameterRequest(id, "RemoveProjectParameter", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveProjectParameterResponse>(response)!;
        }

        public async Task<UpdateObjectModelResponse> UpdateObjectModelAsync(UpdateObjectModelRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectModelRequest(id, "UpdateObjectModel", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateObjectModelResponse>(response)!;
        }

        public async Task<AddVirtualCollisionObjectToSceneResponse> AddVirtualCollisionObjectToSceneAsync(AddVirtualCollisionObjectToSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddVirtualCollisionObjectToSceneRequest(id, "AddVirtualCollisionObjectToScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddVirtualCollisionObjectToSceneResponse>(response)!;
        }

        public async Task<CopySceneResponse> DuplicateSceneAsync(CopySceneRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CopySceneRequest(id, "CopyScene", args), id);
            return JsonConvert.DeserializeObject<CopySceneResponse>(response)!;
        }

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

        public async Task<GetProjectResponse> GetProjectAsync(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetProjectRequest(id, "GetProject", args), id);
            return JsonConvert.DeserializeObject<GetProjectResponse>(response)!;
        }

        public async Task<GetRobotJointsResponse> GetRobotJointsAsync(GetRobotJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetRobotJointsRequest(id, "GetRobotJoints", args), id);
            return JsonConvert.DeserializeObject<GetRobotJointsResponse>(response)!;
        }

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

        public async Task<UpdateProjectDescriptionResponse> UpdateProjectDescriptionAsync(UpdateProjectDescriptionRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateProjectDescriptionRequest(id, "UpdateProjectDescription", args), id);
            return JsonConvert.DeserializeObject<UpdateProjectDescriptionResponse>(response)!;
        }

        public async Task<UpdateProjectHasLogicResponse> UpdateProjectHasLogicAsync(UpdateProjectHasLogicRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateProjectHasLogicRequest(id, "UpdateProjectHasLogic", args), id);
            return JsonConvert.DeserializeObject<UpdateProjectHasLogicResponse>(response)!;
        }

        public async Task<UpdateSceneDescriptionResponse> UpdateSceneDescriptionAsync(UpdateSceneDescriptionRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateSceneDescriptionRequest(id, "UpdateSceneDescription", args), id);
            return JsonConvert.DeserializeObject<UpdateSceneDescriptionResponse>(response)!;
        }

        public async Task<UploadPackageResponse> UploadPackageAsync(UploadPackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UploadPackageRequest(id, "UploadPackage", args), id);
            return JsonConvert.DeserializeObject<UploadPackageResponse>(response)!;
        }

        #endregion

        #endregion
    }
}