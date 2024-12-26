using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcor2.ClientSdk.Communication.Design;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Newtonsoft.Json;
using Action = System.Action;
using WebSocketState = Arcor2.ClientSdk.Communication.Design.WebSocketState;

namespace Arcor2.ClientSdk.Communication {
    /// <summary>
    /// Client for communication with ARCOR2 server. Default implementation using the System.Net.WebSockets.ClientWebSocket.
    /// </summary>
    public class Arcor2Client : Arcor2Client<SystemNetWebSocket> { };

    /// <summary>
    /// Client for communication with ARCOR2 server with custom WebSocket implementation.
    /// </summary>
    /// <typeparam name="TWebSocket">WebSocket implementation</typeparam>
    public class Arcor2Client<TWebSocket> where TWebSocket : class, IWebSocket, new() {
        private readonly TWebSocket webSocket = new TWebSocket();

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
        private int requestId = 0;

        public Uri? Uri { get; private set; }

        #region Callback Event Definitions
        /// <summary>
        /// Invoked when any connection-related error occurs.
        /// </summary>
        public EventHandler<Exception>? ConnectionError;
        /// <summary>
        /// Invoked when connection is closed.
        /// </summary>
        public EventHandler<WebSocketCloseEventArgs>? ConnectionClosed;
        /// <summary>
        /// Invoked when connection is successfully opened.
        /// </summary>
        public EventHandler? ConnectionOpened;

        /// <summary>
        /// Invoked when new end effector pose is received from server. Contains eef pose.
        /// </summary>
        public event EventHandler<RobotEefUpdatedEventArgs>? OnRobotEefUpdated;
        /// <summary>
        /// Invoked when new joints values are received from server. Contains joint values.
        /// </summary>
        public event EventHandler<RobotJointsUpdatedEventArgs>? OnRobotJointsUpdated;
        /// <summary>
        /// Invoked when main screen should be opened. Contains info of which list (scenes, projects, packages)
        /// should be opened and which tile should be highlighted.
        /// </summary>
        public event EventHandler<ShowMainScreenEventArgs>? OnShowMainScreen;
        /// <summary>
        /// Invoked when action item added. Contains info about the logic item.
        /// </summary>
        public event EventHandler<LogicItemChangedEventArgs>? OnLogicItemAdded;
        /// <summary>
        /// Invoked when logic item removed. Contains UUID of removed item.
        /// </summary>
        public event EventHandler<StringEventArgs>? OnLogicItemRemoved;
        /// <summary>
        /// Invoked when logic item updated. Contains info of updated logic item. 
        /// </summary>
        public event EventHandler<LogicItemChangedEventArgs>? OnLogicItemUpdated;
        public event EventHandler<StringEventArgs>? OnProjectRemoved;
        public event EventHandler<BareProjectEventArgs>? OnProjectBaseUpdated;

        public event EventHandler<StringListEventArgs>? OnObjectTypeRemoved;
        public event EventHandler<ObjectTypesEventArgs>? OnObjectTypeAdded;
        public event EventHandler<ObjectTypesEventArgs>? OnObjectTypeUpdated;

        public event EventHandler<StringEventArgs>? OnSceneRemoved;
        public event EventHandler<BareSceneEventArgs>? OnSceneBaseUpdated;

        public event EventHandler<BareActionEventArgs>? OnActionBaseUpdated;
        public event EventHandler<StringEventArgs>? OnActionRemoved;

        public event EventHandler<ProjectActionPointEventArgs>? OnActionPointAdded;
        public event EventHandler<ProjectActionPointEventArgs>? OnActionPointUpdated;
        public event EventHandler<BareActionPointEventArgs>? OnActionPointBaseUpdated;
        public event EventHandler<StringEventArgs>? OnActionPointRemoved;

        public event EventHandler<ActionPointOrientationEventArgs>? OnActionPointOrientationAdded;
        public event EventHandler<ActionPointOrientationEventArgs>? OnActionPointOrientationUpdated;
        public event EventHandler<ActionPointOrientationEventArgs>? OnActionPointOrientationBaseUpdated;
        public event EventHandler<StringEventArgs>? OnActionPointOrientationRemoved;

        public event EventHandler<RobotJointsEventArgs>? OnActionPointJointsAdded;
        public event EventHandler<RobotJointsEventArgs>? OnActionPointJointsUpdated;
        public event EventHandler<RobotJointsEventArgs>? OnActionPointJointsBaseUpdated;
        public event EventHandler<StringEventArgs>? OnActionPointJointsRemoved;

        public event EventHandler<ParameterEventArgs>? OnOverrideAdded;
        public event EventHandler<ParameterEventArgs>? OnOverrideUpdated;
        public event EventHandler<ParameterEventArgs>? OnOverrideBaseUpdated;
        public event EventHandler<ParameterEventArgs>? OnOverrideRemoved;

        public event EventHandler<RobotMoveToPoseEventArgs>? OnRobotMoveToPoseEvent;
        public event EventHandler<RobotMoveToJointsEventArgs>? OnRobotMoveToJointsEvent;
        public event EventHandler<RobotMoveToActionPointOrientationEventArgs>? OnRobotMoveToActionPointOrientationEvent;
        public event EventHandler<RobotMoveToActionPointJointsEventArgs>? OnRobotMoveToActionPointJointsEvent;
        public event EventHandler<SceneStateEventArgs>? OnSceneStateEvent;

        public event EventHandler<ProjectParameterEventArgs>? OnProjectParameterAdded;
        public event EventHandler<ProjectParameterEventArgs>? OnProjectParameterUpdated;
        public event EventHandler<ProjectParameterEventArgs>? OnProjectParameterRemoved;

        /// <summary>
        /// Invoked for actions regarding calibration of camera or robot
        /// </summary>
        public event EventHandler<ProcessStateEventArgs>? OnProcessStateEvent;

        #endregion

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
            webSocket.OnMessage += async (_, args) => {
                await OnMessage(args);
            };

        }

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

        /// <summary>
        /// Sends a message without waiting for a response
        /// </summary>
        private async Task SendAsync(string message) {
            if(webSocket.State != WebSocketState.Open) {
                throw new InvalidOperationException("Cannot send message when connection is not open.");
            }

            await webSocket.SendAsync(message);
        }

        /// <summary>
        /// Sends a request and waits for a response with the matching ID
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

        private async Task OnMessage(WebSocketMessageEventArgs args) {
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
        }

        /// <summary>
        /// Handles changes on project
        /// </summary>
        private void HandleProjectChanged(string data) {
            var eventProjectChanged = JsonConvert.DeserializeObject<ProjectChanged>(data);
            switch(eventProjectChanged!.ChangeType) {
                case ProjectChanged.ChangeTypeEnum.Add:
                    throw new NotImplementedException("ProjectChanged add should never occur!");
                case ProjectChanged.ChangeTypeEnum.Remove:
                    OnProjectRemoved?.Invoke(this, new StringEventArgs(eventProjectChanged.Data.Id));
                    break;
                case ProjectChanged.ChangeTypeEnum.Update:
                    throw new NotImplementedException("ProjectChanged update should never occur!");
                case ProjectChanged.ChangeTypeEnum.UpdateBase:
                    OnProjectBaseUpdated?.Invoke(this, new BareProjectEventArgs(eventProjectChanged.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown ProjectChanged change type.!");
            }
        }

        /// <summary>
        /// Handles changes on project
        /// </summary>
        private void HandleOverrideUpdated(string data) {
            var overrideUpdated = JsonConvert.DeserializeObject<OverrideUpdated>(data);
            switch(overrideUpdated!.ChangeType) {
                case OverrideUpdated.ChangeTypeEnum.Add:
                    OnOverrideAdded?.Invoke(this, new ParameterEventArgs(overrideUpdated.ParentId, overrideUpdated.Data));
                    break;
                case OverrideUpdated.ChangeTypeEnum.Remove:
                    OnOverrideRemoved?.Invoke(this, new ParameterEventArgs(overrideUpdated.ParentId, overrideUpdated.Data));
                    break;
                case OverrideUpdated.ChangeTypeEnum.Update:
                    OnOverrideUpdated?.Invoke(this, new ParameterEventArgs(overrideUpdated.ParentId, overrideUpdated.Data));
                    break;
                case OverrideUpdated.ChangeTypeEnum.UpdateBase:
                    OnOverrideBaseUpdated?.Invoke(this, new ParameterEventArgs(overrideUpdated.ParentId, overrideUpdated.Data));
                    break;
                default:
                    throw new NotImplementedException("Unknown OverrideUpdated change type.!");
            }
        }

        /// <summary>
        /// Handles message with info about robots end effector
        /// </summary>
        private void HandleRobotEef(string data) {
            var robotEef = JsonConvert.DeserializeObject<RobotEef>(data);
            OnRobotEefUpdated?.Invoke(this, new RobotEefUpdatedEventArgs(robotEef!.Data));
        }

        /// <summary>
        /// Handles message with info about robots joints
        /// </summary>
        private void HandleRobotJoints(string data) {
            var robotJoints = JsonConvert.DeserializeObject<RobotJoints>(data);
            OnRobotJointsUpdated?.Invoke(this, new RobotJointsUpdatedEventArgs(robotJoints!.Data));
        }

        /// <summary>
        /// Handles message with info for main screen
        /// </summary>
        private void HandleShowMainScreen(string data) {
            var showMainScreen = JsonConvert.DeserializeObject<ShowMainScreen>(data);
            OnShowMainScreen?.Invoke(this, new ShowMainScreenEventArgs(showMainScreen!.Data));
            // TODO: This was here, move out: GameManager.Instance.ShowMainScreen();
        }

        private void HandleRobotMoveToActionPointOrientation(string data) {
            var robotMoveToActionPointOrientation = JsonConvert.DeserializeObject<RobotMoveToActionPointOrientation>(data);
            OnRobotMoveToActionPointOrientationEvent?.Invoke(this, new RobotMoveToActionPointOrientationEventArgs(robotMoveToActionPointOrientation!));
        }

        private void HandleRobotMoveToPoseEvent(string data) {
            var robotMoveToPose = JsonConvert.DeserializeObject<RobotMoveToPose>(data);
            OnRobotMoveToPoseEvent?.Invoke(this, new RobotMoveToPoseEventArgs(robotMoveToPose!));
        }

        private void HandleRobotMoveToJointsEvent(string data) {
            var robotMoveToJoints = JsonConvert.DeserializeObject<RobotMoveToJoints>(data);
            OnRobotMoveToJointsEvent?.Invoke(this, new RobotMoveToJointsEventArgs(robotMoveToJoints!));
        }

        private void HandleRobotMoveToActionPointJointsEvent(string data) {
            var robotMoveToActionPointJoints = JsonConvert.DeserializeObject<RobotMoveToActionPointJoints>(data);
            OnRobotMoveToActionPointJointsEvent?.Invoke(this, new RobotMoveToActionPointJointsEventArgs(robotMoveToActionPointJoints!));
        }

        private void HandleStateBefore(string obj) {
            string puck_id;

            try {

                ActionStateBefore actionStateBefore = JsonConvert.DeserializeObject<ActionStateBefore>(obj);
                if(!string.IsNullOrEmpty(actionStateBefore.Data.ActionId)) {
                    puck_id = actionStateBefore.Data.ActionId;

                    GameManager.Instance.ActionStateBefore(actionStateBefore.Data);
                    /*if (!ProjectManager.Instance.Valid) {
                        Debug.LogWarning("Project not yet loaded, ignoring current action");
                        GameManager.Instance.ActionRunningOnStartupId = puck_id;
                        return;
                    }*/
                    // Stop previously running action (change its color to default)
                    /*if (ActionsManager.Instance.CurrentlyRunningAction != null)
                        ActionsManager.Instance.CurrentlyRunningAction.StopAction();*/

                    /*Action puck = ProjectManager.Instance.GetAction(puck_id);
                    ActionsManager.Instance.CurrentlyRunningAction = puck;
                    // Run current action (set its color to running)
                    puck.RunAction();*/
                }
                else {
                    /*if (ActionsManager.Instance.CurrentlyRunningAction != null)
                        ActionsManager.Instance.CurrentlyRunningAction.StopAction();
                    ActionsManager.Instance.CurrentlyRunningAction = null;*/
                }



            }
            catch(NullReferenceException e) {
                Debug.Log("Parse error in HandleCurrentAction()");
                return;
            } /*catch (ItemNotFoundException e) {
                Debug.LogError(e);
            }*/

        }

        private void HandleActionStateAfter(string obj) {
            string puck_id;
            /*if (!ProjectManager.Instance.Valid) {
                return;
            }*/
            try {

                ActionStateAfter actionStateBefore = JsonConvert.DeserializeObject<ActionStateAfter>(obj);
                /*if (ActionsManager.Instance.CurrentlyRunningAction != null)
                    ActionsManager.Instance.CurrentlyRunningAction.StopAction();
                ActionsManager.Instance.CurrentlyRunningAction = null;*/

            }
            catch(NullReferenceException e) {
                Debug.Log("Parse error in HandleCurrentAction()");
                return;
            }

        }


        /// <summary>
        /// Handles result of recently executed action
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleActionResult(string data) {

            /*if (!ProjectManager.Instance.Valid) {
                return;
            }*/
            ActionResult actionResult = JsonConvert.DeserializeObject<ActionResult>(data);
            //GameManager.Instance.HandleActionResult(actionResult.Data);
        }

        public bool IsWebsocketNull() {
            return websocket == null;
        }

        /// <summary>
        /// Informs that execution of action was canceled
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleActionCanceled(string data) {
            //GameManager.Instance.HandleActionCanceled();
        }

        /// <summary>
        /// Informs that action is being executed
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleActionExecution(string data) {
            ActionExecution actionExecution = JsonConvert.DeserializeObject<ActionExecution>(data);
            //GameManager.Instance.HandleActionExecution(actionExecution.Data.ActionId);
        }

        /// <summary>
        /// Decodes project exception 
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleProjectException(string data) {
            ProjectException projectException = JsonConvert.DeserializeObject<ProjectException>(data);
            //GameManager.Instance.HandleProjectException(projectException.Data);
        }

        /// <summary>
        /// Decodes package state
        /// </summary>
        /// <param name="obj"></param>
        private void HandlePackageState(string obj) {
            PackageState projectState = JsonConvert.DeserializeObject<PackageState>(obj);
            GameManager.Instance.PackageStateUpdated(projectState.Data);
        }

        /// <summary>
        /// Decodes package info
        /// </summary>
        /// <param name="obj">Message from server</param>
        private void HandlePackageInfo(string obj) {
            PackageInfo packageInfo = JsonConvert.DeserializeObject<PackageInfo>(obj);
            GameManager.Instance.packageInfo = packageInfo.Data;
            GameManager.Instance.PackageInfoUpdated();
        }

        /// <summary>
        /// Decodes changes on scene and invoke proper callback
        /// </summary>
        /// <param name="obj">Message from server</param>
        private void HandleSceneChanged(string obj) {
            SceneChanged sceneChangedEvent = JsonConvert.DeserializeObject<SceneChanged>(obj);
            switch(sceneChangedEvent.ChangeType) {
                case SceneChanged.ChangeTypeEnum.Add:
                    throw new NotImplementedException("Scene add should never occure");
                case SceneChanged.ChangeTypeEnum.Remove:
                    OnSceneRemoved?.Invoke(this, new StringEventArgs(sceneChangedEvent.Data.Id));
                    break;
                case SceneChanged.ChangeTypeEnum.Update:
                    throw new NotImplementedException("Scene update should never occure");
                case SceneChanged.ChangeTypeEnum.Updatebase:
                    OnSceneBaseUpdated?.Invoke(this, new BareSceneEventArgs(sceneChangedEvent.Data));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void HandleSceneState(string obj) {
            SceneState sceneState = JsonConvert.DeserializeObject<SceneState>(obj);
            OnSceneStateEvent?.Invoke(this, new SceneStateEventArgs(sceneState.Data));
        }

        /// <summary>
        /// Enables invoking of scene state event from other classes - temporary HACK to enable
        /// invokation upon scene openning
        /// </summary>
        public void InvokeSceneStateEvent(SceneStateData sceneStateData) {
            OnSceneStateEvent?.Invoke(this, new SceneStateEventArgs(sceneStateData));
        }

        /// <summary>
        /// Decodes changes on object types and invoke proper callback
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleChangedObjectTypesEvent(string data) {
            ChangedObjectTypes objectTypesChangedEvent = JsonConvert.DeserializeObject<ChangedObjectTypes>(data);
            switch(objectTypesChangedEvent.ChangeType) {
                case ChangedObjectTypes.ChangeTypeEnum.Add:
                    //ActionsManager.Instance.ActionsReady = false;
                    OnObjectTypeAdded?.Invoke(this, new ObjectTypesEventArgs(objectTypesChangedEvent.Data));
                    break;
                case ChangedObjectTypes.ChangeTypeEnum.Remove:
                    List<string> removed = new List<string>();
                    foreach(ObjectTypeMeta type in objectTypesChangedEvent.Data) {
                        removed.Add(type.Type);
                    }

                    OnObjectTypeRemoved?.Invoke(this, new StringListEventArgs(removed));
                    break;

                case ChangedObjectTypes.ChangeTypeEnum.Update:
                    //ActionsManager.Instance.ActionsReady = false;
                    OnObjectTypeUpdated?.Invoke(this, new ObjectTypesEventArgs(objectTypesChangedEvent.Data));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Decodes changes on actions and invokes proper callback 
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleActionChanged(string data) {
            ActionChanged actionChanged = JsonConvert.DeserializeObject<ActionChanged>(data);
            var actionChangedFields = new {
                data = new Action(id: "", name: "", type: "", flows: new List<Flow>(), parameters: new List<ActionParameter>())
            };
            //ProjectManager.Instance.ProjectChanged = true;
            switch(actionChanged.ChangeType) {
                case ActionChanged.ChangeTypeEnum.Add:
                    var action = JsonConvert.DeserializeAnonymousType(data, actionChangedFields);
                    GameManager.Instance.ActionAdded(action.data, actionChanged.ParentId);
                    break;
                case ActionChanged.ChangeTypeEnum.Remove:
                    GameManager.Instance.ActionRemoved(actionChanged.Data);
                    break;
                case ActionChanged.ChangeTypeEnum.Update:
                    var actionUpdate = JsonConvert.DeserializeAnonymousType(data, actionChangedFields);
                    //ProjectManager.Instance.ActionUpdated(actionUpdate.data);
                    break;
                case ActionChanged.ChangeTypeEnum.Updatebase:
                    GameManager.Instance.ActionBaseUpdated(actionChanged.Data);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Decodes changes in program logic
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleLogicItemChanged(string data) {
            LogicItemChanged logicItemChanged = JsonConvert.DeserializeObject<LogicItemChanged>(data);
            //ProjectManager.Instance.ProjectChanged = true;
            switch(logicItemChanged.ChangeType) {
                case LogicItemChanged.ChangeTypeEnum.Add:
                    OnLogicItemAdded?.Invoke(this, new LogicItemChangedEventArgs(logicItemChanged.Data));
                    break;
                case LogicItemChanged.ChangeTypeEnum.Remove:
                    OnLogicItemRemoved?.Invoke(this, new StringEventArgs(logicItemChanged.Data.Id));
                    break;
                case LogicItemChanged.ChangeTypeEnum.Update:
                    OnLogicItemUpdated?.Invoke(this, new LogicItemChangedEventArgs(logicItemChanged.Data));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Decodes changes of action points
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleActionPointChanged(string data) {
            //ProjectManager.Instance.ProjectChanged = true;
            ActionPointChanged actionPointChangedEvent = JsonConvert.DeserializeObject<ActionPointChanged>(data);
            var actionPointChangedFields = new {
                data = new ActionPoint(id: "", name: "string", parent: "", position: new Position(),
                    actions: new List<Action>(), orientations: new List<NamedOrientation>(),
                    robotJoints: new List<ProjectRobotJoints>())
            };

            switch(actionPointChangedEvent.ChangeType) {
                case ActionPointChanged.ChangeTypeEnum.Add:
                    var actionPoint = JsonConvert.DeserializeAnonymousType(data, actionPointChangedFields);
                    OnActionPointAdded?.Invoke(this, new ProjectActionPointEventArgs(actionPoint.data));
                    break;
                case ActionPointChanged.ChangeTypeEnum.Remove:
                    OnActionPointRemoved?.Invoke(this, new StringEventArgs(actionPointChangedEvent.Data.Id));
                    break;
                case ActionPointChanged.ChangeTypeEnum.Update:
                    var actionPointUpdate = JsonConvert.DeserializeAnonymousType(data, actionPointChangedFields);
                    OnActionPointUpdated?.Invoke(this, new ProjectActionPointEventArgs(actionPointUpdate.data));
                    break;
                case ActionPointChanged.ChangeTypeEnum.Updatebase:
                    OnActionPointBaseUpdated?.Invoke(this, new BareActionPointEventArgs(actionPointChangedEvent.Data));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Decodes changes on orientations
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleOrientationChanged(string data) {
            //ProjectManager.Instance.ProjectChanged = true;
            OrientationChanged orientationChanged = JsonConvert.DeserializeObject<OrientationChanged>(data);
            switch(orientationChanged.ChangeType) {
                case OrientationChanged.ChangeTypeEnum.Add:
                    OnActionPointOrientationAdded?.Invoke(this, new ActionPointOrientationEventArgs(orientationChanged.Data, orientationChanged.ParentId));
                    break;
                case OrientationChanged.ChangeTypeEnum.Remove:
                    OnActionPointOrientationRemoved?.Invoke(this, new StringEventArgs(orientationChanged.Data.Id));
                    break;
                case OrientationChanged.ChangeTypeEnum.Update:
                    OnActionPointOrientationUpdated?.Invoke(this, new ActionPointOrientationEventArgs(orientationChanged.Data, null));
                    break;
                case OrientationChanged.ChangeTypeEnum.Updatebase:
                    OnActionPointOrientationBaseUpdated?.Invoke(this, new ActionPointOrientationEventArgs(orientationChanged.Data, null));

                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Decodes changes on joints
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleJointsChanged(string data) {
            //ProjectManager.Instance.ProjectChanged = true;
            JointsChanged jointsChanged = JsonConvert.DeserializeObject<JointsChanged>(data);
            switch(jointsChanged.ChangeType) {
                case JointsChanged.ChangeTypeEnum.Add:
                    OnActionPointJointsAdded?.Invoke(this, new RobotJointsEventArgs(jointsChanged.Data, jointsChanged.ParentId));
                    break;
                case JointsChanged.ChangeTypeEnum.Remove:
                    OnActionPointJointsRemoved?.Invoke(this, new StringEventArgs(jointsChanged.Data.Id));
                    break;
                case JointsChanged.ChangeTypeEnum.Update:
                    OnActionPointJointsUpdated?.Invoke(this, new RobotJointsEventArgs(jointsChanged.Data, ""));
                    break;
                case JointsChanged.ChangeTypeEnum.Updatebase:
                    OnActionPointJointsBaseUpdated?.Invoke(this, new RobotJointsEventArgs(jointsChanged.Data, ""));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Decodes changes on scene objects
        /// </summary>
        /// <param name="data">Message from server</param>
        /// <returns></returns>
        private void HandleSceneObjectChanged(string data) {
            SceneObjectChanged sceneObjectChanged = JsonConvert.DeserializeObject<SceneObjectChanged>(data);
            switch(sceneObjectChanged.ChangeType) {
                case SceneObjectChanged.ChangeTypeEnum.Add:
                    GameManager.Instance.SceneObjectAdded(sceneObjectChanged.Data);
                    break;
                case SceneObjectChanged.ChangeTypeEnum.Remove:
                    GameManager.Instance.SceneObjectRemoved(sceneObjectChanged.Data);
                    break;
                case SceneObjectChanged.ChangeTypeEnum.Update:
                    GameManager.Instance.SceneObjectUpdated(sceneObjectChanged.Data);
                    break;
                case SceneObjectChanged.ChangeTypeEnum.Updatebase:
                    //SceneManager.Instance.SceneObjectBaseUpdated(sceneObjectChanged.Data);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Invoked when openning of project is requested
        /// </summary>
        /// <param name="data">Message from server</param>
        private async void HandleOpenProject(string data) {
            OpenProject openProjectEvent = JsonConvert.DeserializeObject<OpenProject>(data);
            GameManager.Instance.OpenProject(openProjectEvent.Data.Scene, openProjectEvent.Data.Project);
        }

        /// <summary>
        /// Invoked when openning of scene is requested
        /// </summary>
        /// <param name="data"Message from server></param>
        /// <returns></returns>
        private async Task HandleOpenScene(string data) {
            OpenScene openSceneEvent = JsonConvert.DeserializeObject<OpenScene>(data);
            GameManager.Instance.OpenScene(openSceneEvent.Data.Scene);
        }

        /// <summary>
        /// Invoked when closing of project is requested
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleCloseProject(string data) {
            GameManager.Instance.CloseProject();
        }

        /// <summary>
        /// Invoked when closing of scene is requested
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleCloseScene(string data) {
            GameManager.Instance.CloseScene();
        }

        /// <summary>
        /// Invoked when openning of package is requested
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleOpenPackage(string data) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Invoked when project was saved
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleProjectSaved(string data) {
            //ProjectManager.Instance.ProjectSaved();
        }

        /// <summary>
        /// Invoked when scene was saved
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleSceneSaved(string data) {
            //SceneManager.Instance.SceneSaved();
        }

        /// <summary>
        /// Invoked when an object was unlocked
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleObjectUnlocked(string data) {
            ObjectsUnlocked objectsUnlockedEvent = JsonConvert.DeserializeObject<ObjectsUnlocked>(data);
            //LockingEventsCache.Instance.Add(new ObjectLockingEventArgs(objectsUnlockedEvent.Data.ObjectIds, false, objectsUnlockedEvent.Data.Owner));
        }

        /// <summary>
        /// Invoked when an object was locked
        /// </summary>
        /// <param name="data">Message from server</param>
        private void HandleObjectLocked(string data) {
            ObjectsLocked objectsLockedEvent = JsonConvert.DeserializeObject<ObjectsLocked>(data);
            //LockingEventsCache.Instance.Add(new ObjectLockingEventArgs(objectsLockedEvent.Data.ObjectIds, true, objectsLockedEvent.Data.Owner));
        }

        private void HandleProcessState(string data) {
            ProcessState processState = JsonConvert.DeserializeObject<ProcessState>(data);
            OnProcessStateEvent?.Invoke(this, new ProcessStateEventArgs(processState.Data));
        }

        private void HandleProjectParameterChanged(string data) {
            ProjectParameterChanged projectParameterChanged = JsonConvert.DeserializeObject<ProjectParameterChanged>(data);
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

        #region Request-Response Methods

        public async Task<GetObjectTypesResponse> GetObjectTypes() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetObjectTypesRequest(id, "GetObjectTypes"), id);
            return JsonConvert.DeserializeObject<GetObjectTypesResponse>(response)!;
        }


        public async Task<GetActionsResponse> GetActions(TypeArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetActionsRequest(id, "GetActions", args), id);
            return JsonConvert.DeserializeObject<GetActionsResponse>(response)!;
        }

        public async Task<SaveSceneResponse> SaveScene(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SaveSceneRequest(id, "SaveScene", isDryRun), id);
            return JsonConvert.DeserializeObject<SaveSceneResponse>(response)!;
        }

        public async Task<SaveProjectResponse> SaveProject(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SaveProjectRequest(id, "SaveProject", isDryRun), id);
            return JsonConvert.DeserializeObject<SaveProjectResponse>(response)!;
        }
        
        public async Task<OpenProjectResponse> OpenProject(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new OpenProjectRequest(id, "OpenProject", args), id);
            return JsonConvert.DeserializeObject<OpenProjectResponse>(response)!;
        }

        public async Task<RunPackageResponse> RunPackage(RunPackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RunPackageRequest(id, "RunPackage", args), id);
            return JsonConvert.DeserializeObject<RunPackageResponse>(response)!;
        }

        public async Task<TemporaryPackageResponse> TemporaryPackage(TemporaryPackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new TemporaryPackageRequest(id, "TemporaryPackage", args), id);
            return JsonConvert.DeserializeObject<TemporaryPackageResponse>(response)!;
        }

        public async Task<StopPackageResponse> StopPackage() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StopPackageRequest(id, "StopPackage"), id);
            return JsonConvert.DeserializeObject<StopPackageResponse>(response)!;
        }

        public async Task<PausePackageResponse> PausePackage() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new PausePackageRequest(id, "PausePackage"), id);
            return JsonConvert.DeserializeObject<PausePackageResponse>(response)!;
        }

        public async Task<ResumePackageResponse> ResumePackage() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ResumePackageRequest(id, "ResumePackage"), id);
            return JsonConvert.DeserializeObject<ResumePackageResponse>(response)!;
        }

        public async Task<UpdateActionPointUsingRobotResponse> UpdateActionPointUsingRobot(UpdateActionPointUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointUsingRobotRequest(id, "UpdateActionPointUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointUsingRobotResponse>(response)!;
        }

        public async Task<UpdateObjectPoseResponse> UpdateObjectPose(UpdateObjectPoseRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectPoseRequest(id, "UpdateObjectPose", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateObjectPoseResponse>(response)!;
        }

        public async Task<UpdateObjectPoseUsingRobotResponse> UpdateObjectPoseUsingRobot(UpdateObjectPoseUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectPoseUsingRobotRequest(id, "UpdateObjectPoseUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateObjectPoseUsingRobotResponse>(response)!;
        }

        public async Task<NewObjectTypeResponse> NewObjectType(ObjectTypeMeta args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new NewObjectTypeRequest(id, "NewObjectType", args, isDryRun), id);
            return JsonConvert.DeserializeObject<NewObjectTypeResponse>(response)!;
        }

        public async Task<ObjectAimingStartResponse> ObjectAimingStart(ObjectAimingStartRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingStartRequest(id, "ObjectAimingStart", args, isDryRun), id);
            return JsonConvert.DeserializeObject<ObjectAimingStartResponse>(response)!;
        }

        public async Task<ObjectAimingAddPointResponse> ObjectAimingAddPoint(ObjectAimingAddPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingAddPointRequest(id, "ObjectAimingAddPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<ObjectAimingAddPointResponse>(response)!;
        }

        public async Task<ObjectAimingDoneResponse> ObjectAimingDone(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingDoneRequest(id, "ObjectAimingDone", isDryRun), id);
            return JsonConvert.DeserializeObject<ObjectAimingDoneResponse>(response)!;
        }

        public async Task<ObjectAimingCancelResponse> ObjectAimingCancel(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ObjectAimingCancelRequest(id, "ObjectAimingCancel", isDryRun), id);
            return JsonConvert.DeserializeObject<ObjectAimingCancelResponse>(response)!;
        }
        
        public async Task<ListScenesResponse> ListScenes() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListScenesRequest(id, "ListScenes"), id);
            return JsonConvert.DeserializeObject<ListScenesResponse>(response)!;
        }

        public async Task<ListProjectsResponse> ListProjects() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListProjectsRequest(id, "ListProjects"), id);
            return JsonConvert.DeserializeObject<ListProjectsResponse>(response)!;
        }

        public async Task<ListPackagesResponse> ListPackages() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ListPackagesRequest(id, "ListPackages"), id);
            return JsonConvert.DeserializeObject<ListPackagesResponse>(response)!;
        }

        public async Task<AddObjectToSceneResponse> AddObjectToScene(AddObjectToSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddObjectToSceneRequest(id, "AddObjectToScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddObjectToSceneResponse>(response)!;
        }

        public async Task<RemoveFromSceneResponse> RemoveFromScene(RemoveFromSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveFromSceneRequest(id, "RemoveFromScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveFromSceneResponse>(response)!;
        }

        public async Task<DeleteObjectTypesResponse> DeleteObjectTypes(List<string> args, bool isDryRun = false) {
            // Todo: Single version
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteObjectTypesRequest(id, "DeleteObjectTypes", args, isDryRun), id);
            return JsonConvert.DeserializeObject<DeleteObjectTypesResponse>(response)!;
        }

        public async Task<OpenSceneResponse> OpenScene(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new OpenSceneRequest(id, "OpenScene", args), id);
            return JsonConvert.DeserializeObject<OpenSceneResponse>(response)!;
        }

        public async Task<ActionParamValuesResponse> ActionParamValues(ActionParamValuesRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ActionParamValuesRequest(id, "ActionParamValues", args), id);
            return JsonConvert.DeserializeObject<ActionParamValuesResponse>(response)!;
        }

        public async Task<ExecuteActionResponse> ExecuteAction(ExecuteActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ExecuteActionRequest(id, "ExecuteAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<ExecuteActionResponse>(response)!;
        }

        public async Task<CancelActionResponse> CancelAction() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CancelActionRequest(id, "CancelAction"), id);
            return JsonConvert.DeserializeObject<CancelActionResponse>(response)!;
        }

        public async Task<SystemInfoResponse> SystemInfo() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SystemInfoRequest(id, "SystemInfo"), id);
            return JsonConvert.DeserializeObject<SystemInfoResponse>(response)!;
        }

        public async Task<BuildProjectResponse> BuildProject(BuildProjectRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new BuildProjectRequest(id, "BuildProject", args), id);
            return JsonConvert.DeserializeObject<BuildProjectResponse>(response)!;
        }

        public async Task<NewSceneResponse> NewScene(NewSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new NewSceneRequest(id, "NewScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<NewSceneResponse>(response)!;
        }

        public async Task<DeleteSceneResponse> DeleteScene(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteSceneRequest(id, "DeleteScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<DeleteSceneResponse>(response)!;
        }

        public async Task<RenameSceneResponse> RenameScene(RenameArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameSceneRequest(id, "RenameScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameSceneResponse>(response)!;
        }

        public async Task<RenameObjectResponse> RenameObject(RenameArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameObjectRequest(id, "RenameObject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameObjectResponse>(response)!;
        }

        public async Task<CloseSceneResponse> CloseScene(CloseSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CloseSceneRequest(id, "CloseScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<CloseSceneResponse>(response)!;
        }

        public async Task<ProjectsWithSceneResponse> ProjectsWithScene(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ProjectsWithSceneRequest(id, "ProjectsWithScene", args), id);
            return JsonConvert.DeserializeObject<ProjectsWithSceneResponse>(response)!;
        }

        public async Task<NewProjectResponse> NewProject(NewProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new NewProjectRequest(id, "NewProject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<NewProjectResponse>(response)!;
        }
        public async Task<DeleteProjectResponse> DeleteProject(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteProjectRequest(id, "DeleteProject", args), id);
            return JsonConvert.DeserializeObject<DeleteProjectResponse>(response)!;
        }

        public async Task<DeletePackageResponse> DeletePackage(IdArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeletePackageRequest(id, "DeletePackage", args), id);
            return JsonConvert.DeserializeObject<DeletePackageResponse>(response)!;
        }

        public async Task<AddActionPointResponse> AddActionPoint(AddActionPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointRequest(id, "AddActionPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointResponse>(response)!;
        }

        public async Task<AddApUsingRobotResponse> AddApUsingRobot(AddApUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddApUsingRobotRequest(id, "AddApUsingRobot", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddApUsingRobotResponse>(response)!;
        }

        public async Task<UpdateActionPointPositionResponse> UpdateActionPointPosition(UpdateActionPointPositionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointPositionRequest(id, "UpdateActionPointPosition", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateActionPointPositionResponse>(response)!;
        }

        public async Task<UpdateActionPointParentResponse> UpdateActionPointParent(UpdateActionPointParentRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointParentRequest(id, "UpdateActionPointParent", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateActionPointParentResponse>(response)!;
        }

        public async Task<AddActionPointOrientationResponse> AddActionPointOrientation(AddActionPointOrientationRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointOrientationRequest(id, "AddActionPointOrientation", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointOrientationResponse>(response)!;
        }

        public async Task<RemoveActionPointOrientationResponse> RemoveActionPointOrientation(RemoveActionPointOrientationRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointOrientationRequest(id, "RemoveActionPointOrientation", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveActionPointOrientationResponse>(response)!;
        }

        public async Task<UpdateActionPointOrientationResponse> UpdateActionPointOrientation(UpdateActionPointOrientationRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointOrientationRequest(id, "UpdateActionPointOrientation", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointOrientationResponse>(response)!;
        }

        public async Task<AddActionPointOrientationUsingRobotResponse> AddActionPointOrientationUsingRobot(AddActionPointOrientationUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointOrientationUsingRobotRequest(id, "AddActionPointOrientationUsingRobot", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointOrientationUsingRobotResponse>(response)!;
        }

        public async Task<UpdateActionPointOrientationUsingRobotResponse> UpdateActionPointOrientationUsingRobot(UpdateActionPointOrientationUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointOrientationUsingRobotRequest(id, "UpdateActionPointOrientationUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointOrientationUsingRobotResponse>(response)!;
        }

        public async Task<AddActionPointJointsUsingRobotResponse> AddActionPointJointsUsingRobot(AddActionPointJointsUsingRobotRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionPointJointsUsingRobotRequest(id, "AddActionPointJointsUsingRobot", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionPointJointsUsingRobotResponse>(response)!;
        }
       
        public async Task<UpdateActionPointJointsResponse> UpdateActionPointJoints(UpdateActionPointJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointJointsRequest(id, "UpdateActionPointJoints", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointJointsResponse>(response)!;
        }

        public async Task<UpdateActionPointJointsUsingRobotResponse> UpdateActionPointJointsUsingRobot(UpdateActionPointJointsUsingRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionPointJointsUsingRobotRequest(id, "UpdateActionPointJointsUsingRobot", args), id);
            return JsonConvert.DeserializeObject<UpdateActionPointJointsUsingRobotResponse>(response)!;
        }

        public async Task<RenameActionPointResponse> RenameActionPoint(RenameActionPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionPointRequest(id, "RenameActionPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameActionPointResponse>(response)!;
        }

        public async Task<RenameActionPointJointsResponse> RenameActionPointJoints(RenameActionPointJointsRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionPointJointsRequest(id, "RenameActionPointJoints", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameActionPointJointsResponse>(response)!;
        }

        public async Task<RenameActionPointOrientationResponse> RenameActionPointOrientation(RenameActionPointOrientationRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionPointOrientationRequest(id, "RenameActionPointOrientation", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameActionPointOrientationResponse>(response)!;
        }

        public async Task<MoveToActionPointResponse> MoveToActionPoint(MoveToActionPointRequestArgs args) {
            // TODO: Joints x Orientation
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new MoveToActionPointRequest(id, "MoveToActionPoint", args), id);
            return JsonConvert.DeserializeObject<MoveToActionPointResponse>(response)!;
        }

        public async Task<MoveToPoseResponse> MoveToPose(MoveToPoseRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new MoveToPoseRequest(id, "MoveToPose", args), id);
            return JsonConvert.DeserializeObject<MoveToPoseResponse>(response)!;
        }

        public async Task<RemoveActionPointJointsResponse> RemoveActionPointJoints(RemoveActionPointJointsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointJointsRequest(id, "RemoveActionPointJoints", args), id);
            return JsonConvert.DeserializeObject<RemoveActionPointJointsResponse>(response)!;
        }

        public async Task<AddActionResponse> AddAction(AddActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddActionRequest(id, "AddAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddActionResponse>(response)!;
        }

        public async Task<UpdateActionResponse> UpdateAction(UpdateActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateActionRequest(id, "UpdateAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateActionResponse>(response)!;
        }

        public async Task<RemoveActionResponse> RemoveAction(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionRequest(id, "RemoveAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveActionResponse>(response)!;
        }

        public async Task<RenameActionResponse> RenameAction(RenameActionRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameActionRequest(id, "RenameAction", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameActionResponse>(response)!;
        }

        public async Task<AddLogicItemResponse> AddLogicItem(AddLogicItemRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddLogicItemRequest(id, "AddLogicItem", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddLogicItemResponse>(response)!;
        }

        public async Task<UpdateLogicItemResponse> UpdateLogicItem(UpdateLogicItemRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateLogicItemRequest(id, "UpdateLogicItem", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateLogicItemResponse>(response)!;
        }

        public async Task<RemoveLogicItemResponse> RemoveLogicItem(RemoveLogicItemRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveLogicItemRequest(id, "RemoveLogicItem", args), id);
            return JsonConvert.DeserializeObject<RemoveLogicItemResponse>(response)!;
        }

        public async Task<RenameProjectResponse> RenameProject(RenameProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenameProjectRequest(id, "RenameProject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RenameProjectResponse>(response)!;
        }

        public async Task<RenamePackageResponse> RenamePackage(RenamePackageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RenamePackageRequest(id, "RenamePackage", args), id);
            return JsonConvert.DeserializeObject<RenamePackageResponse>(response)!;
        }

        public async Task<RemoveActionPointResponse> RemoveActionPoint(IdArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveActionPointRequest(id, "RemoveActionPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveActionPointResponse>(response)!;
        }

        public async Task<CloseProjectResponse> CloseProject(CloseProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CloseProjectRequest(id, "CloseProject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<CloseProjectResponse>(response)!;
        }

        public async Task<GetEndEffectorPoseResponse> GetEndEffectorPose(GetEndEffectorPoseRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetEndEffectorPoseRequest(id, "GetEndEffectorPose", args), id);
            return JsonConvert.DeserializeObject<GetEndEffectorPoseResponse>(response)!;
        }

        public async Task<RegisterForRobotEventResponse> RegisterForRobotEvent(RegisterForRobotEventRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RegisterForRobotEventRequest(id, "RegisterForRobotEvent", args), id);
            return JsonConvert.DeserializeObject<RegisterForRobotEventResponse>(response)!;
        }

        public async Task<GetRobotMetaResponse> GetRobotMeta() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetRobotMetaRequest(id, "GetRobotMeta"), id);
            return JsonConvert.DeserializeObject<GetRobotMetaResponse>(response)!;
        }

        public async Task<GetEndEffectorsResponse> GetEndEffectors(GetEndEffectorsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetEndEffectorsRequest(id, "GetEndEffectors", args), id);
            return JsonConvert.DeserializeObject<GetEndEffectorsResponse>(response)!;
        }

        public async Task<GetRobotArmsResponse> GetRobotArms(GetRobotArmsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetRobotArmsRequest(id, "GetRobotArms", args), id);
            return JsonConvert.DeserializeObject<GetRobotArmsResponse>(response)!;
        }

        public async Task<StartSceneResponse> StartScene(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StartSceneRequest(id, "StartScene", isDryRun), id);
            return JsonConvert.DeserializeObject<StartSceneResponse>(response)!;
        }

        public async Task<StopSceneResponse> StopScene(bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StopSceneRequest(id, "StopScene", isDryRun), id);
            return JsonConvert.DeserializeObject<StopSceneResponse>(response)!;
        }

        public async Task<UpdateObjectParametersResponse> UpdateObjectParameters(UpdateObjectParametersRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectParametersRequest(id, "UpdateObjectParameters", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateObjectParametersResponse>(response)!;
        }

        public async Task<AddOverrideResponse> AddOverride(AddOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddOverrideRequest(id, "AddOverride", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddOverrideResponse>(response)!;
        }

        public async Task<UpdateOverrideResponse> UpdateOverride(UpdateOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateOverrideRequest(id, "UpdateOverride", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateOverrideResponse>(response)!;
        }

        public async Task<DeleteOverrideResponse> DeleteOverride(DeleteOverrideRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new DeleteOverrideRequest(id, "DeleteOverride", args, isDryRun), id);
            return JsonConvert.DeserializeObject<DeleteOverrideResponse>(response)!;
        }

        public async Task<InverseKinematicsResponse> InverseKinematics(InverseKinematicsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new InverseKinematicsRequest(id, "InverseKinematics", args), id);
            return JsonConvert.DeserializeObject<InverseKinematicsResponse>(response)!;
        }

        public async Task<ForwardKinematicsResponse> ForwardKinematics(ForwardKinematicsRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ForwardKinematicsRequest(id, "ForwardKinematics", args), id);
            return JsonConvert.DeserializeObject<ForwardKinematicsResponse>(response)!;
        }

        public async Task<CalibrateRobotResponse> CalibrateRobot(CalibrateRobotRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CalibrateRobotRequest(id, "CalibrateRobot", args), id);
            return JsonConvert.DeserializeObject<CalibrateRobotResponse>(response)!;
        }

        public async Task<CalibrateCameraResponse> CalibrateCamera(CalibrateCameraRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CalibrateCameraRequest(id, "CalibrateCamera", args), id);
            return JsonConvert.DeserializeObject<CalibrateCameraResponse>(response)!;
        }

        public async Task<CameraColorImageResponse> GetCameraColorImage(CameraColorImageRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CameraColorImageRequest(id, "CameraColorImage", args), id);
            return JsonConvert.DeserializeObject<CameraColorImageResponse>(response)!;
        }

        public async Task<GetCameraPoseResponse> GetCameraPose(GetCameraPoseRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new GetCameraPoseRequest(id, "GetCameraPose", args), id);
            return JsonConvert.DeserializeObject<GetCameraPoseResponse>(response)!;
        }

        public async Task<MarkersCornersResponse> GetMarkersCorners(MarkersCornersRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new MarkersCornersRequest(id, "MarkersCorners", args), id);
            return JsonConvert.DeserializeObject<MarkersCornersResponse>(response)!;
        }

        public async Task<WriteLockResponse> WriteLock(WriteLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new WriteLockRequest(id, "WriteLock", args), id);
            return JsonConvert.DeserializeObject<WriteLockResponse>(response)!;
        }

        public async Task<WriteUnlockResponse> WriteUnlock(WriteUnlockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new WriteUnlockRequest(id, "WriteUnlock", args), id);
            return JsonConvert.DeserializeObject<WriteUnlockResponse>(response)!;
        }

        public async Task<ReadLockResponse> ReadLock(ReadLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ReadLockRequest(id, "ReadLock", args), id);
            return JsonConvert.DeserializeObject<ReadLockResponse>(response)!;
        }

        public async Task<ReadUnlockResponse> ReadUnlock(ReadUnlockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new ReadUnlockRequest(id, "ReadUnlock", args), id);
            return JsonConvert.DeserializeObject<ReadUnlockResponse>(response)!;
        }

        public async Task<UpdateLockResponse> UpdateLock(UpdateLockRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateLockRequest(id, "UpdateLock", args), id);
            return JsonConvert.DeserializeObject<UpdateLockResponse>(response)!;
        }

        public async Task<HandTeachingModeResponse> HandTeachingMode(HandTeachingModeRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new HandTeachingModeRequest(id, "HandTeachingMode", args, isDryRun), id);
            return JsonConvert.DeserializeObject<HandTeachingModeResponse>(response)!;
        }

        public async Task<CopyActionPointResponse> CopyActionPoint(CopyActionPointRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CopyActionPointRequest(id, "CopyActionPoint", args, isDryRun), id);
            return JsonConvert.DeserializeObject<CopyActionPointResponse>(response)!;

        }

        public async Task<StepRobotEefResponse> StepRobotEef(StepRobotEefRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StepRobotEefRequest(id, "StepRobotEef", args, isDryRun), id);
            return JsonConvert.DeserializeObject<StepRobotEefResponse>(response)!;
        }
        
        public async Task<SetEefPerpendicularToWorldResponse> SetEefPerpendicularToWorld(SetEefPerpendicularToWorldRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new SetEefPerpendicularToWorldRequest(id, "SetEefPerpendicularToWorld", args, isDryRun), id);
            return JsonConvert.DeserializeObject<SetEefPerpendicularToWorldResponse>(response)!;
        }

        public async Task<RegisterUserResponse> RegisterUser(RegisterUserRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RegisterUserRequest(id, "RegisterUser", args), id);
            return JsonConvert.DeserializeObject<RegisterUserResponse>(response)!;
        }

        public async Task<AddProjectParameterResponse> AddProjectParameter(AddProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddProjectParameterRequest(id, "AddProjectParameter", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddProjectParameterResponse>(response)!;
        }

        public async Task<UpdateProjectParameterResponse> UpdateProjectParameter(UpdateProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateProjectParameterRequest(id, "UpdateProjectParameter", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateProjectParameterResponse>(response)!;
        }

        public async Task<RemoveProjectParameterResponse> RemoveProjectParameter(RemoveProjectParameterRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new RemoveProjectParameterRequest(id, "RemoveProjectParameter", args, isDryRun), id);
            return JsonConvert.DeserializeObject<RemoveProjectParameterResponse>(response)!;
        }

        public async Task<UpdateObjectModelResponse> UpdateObjectModel(UpdateObjectModelRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new UpdateObjectModelRequest(id, "UpdateObjectModel", args, isDryRun), id);
            return JsonConvert.DeserializeObject<UpdateObjectModelResponse>(response)!;
        }

        public async Task<AddVirtualCollisionObjectToSceneResponse> AddVirtualCollisionObjectToScene(AddVirtualCollisionObjectToSceneRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new AddVirtualCollisionObjectToSceneRequest(id, "AddVirtualCollisionObjectToScene", args, isDryRun), id);
            return JsonConvert.DeserializeObject<AddVirtualCollisionObjectToSceneResponse>(response)!;
        }

        public async Task<CopySceneResponse> DuplicateScene(CopySceneRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CopySceneRequest(id, "CopyScene", args), id);
            return JsonConvert.DeserializeObject<CopySceneResponse>(response)!;
        }

        public async Task<CopyProjectResponse> DuplicateProject(CopyProjectRequestArgs args, bool isDryRun = false) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CopyProjectRequest(id, "CopyProject", args, isDryRun), id);
            return JsonConvert.DeserializeObject<CopyProjectResponse>(response)!;
        }

        public async Task<StepActionResponse> StepAction() {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new StepActionRequest(id, "StepAction"));
            return JsonConvert.DeserializeObject<StepActionResponse>(response)!;
        }


        public async Task<CameraColorParametersResponse> CameraColorParameters(CameraColorParametersRequestArgs args) {
            var id = Interlocked.Increment(ref requestId);
            var response = await SendAndWaitAsync(new CameraColorParametersRequest(id, "CameraColorParameters", args), id);
            return JsonConvert.DeserializeObject<CameraColorParametersResponse>(response)!;
        }

        #endregion
    }
}