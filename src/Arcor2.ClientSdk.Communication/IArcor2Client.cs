using Arcor2.ClientSdk.Communication.Design;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Arcor2.ClientSdk.Communication {
    /// <summary>
    ///     Client interface for communication with ARCOR2 servers.
    /// </summary>
    public interface IArcor2Client {
        /// <summary>
        ///     The URI of the current server.
        /// </summary>
        Uri? Uri { get; }

        /// <summary>
        ///     Represents the state of a connection to server.
        /// </summary>
        WebSocketState ConnectionState { get; }

        /// <summary>
        ///     Resets the client state, allowing reconnection. Does not unregister event handlers or reset request ID.
        /// </summary>
        /// <remarks>
        ///     This method exists for compatibility support with architectures registering events only once at startup. Using new
        ///     instance is recommended.
        /// </remarks>
        void Reset();

        /// <summary>
        ///     Resets the client state, allowing reconnection. Does not unregister event handlers or reset request ID.
        /// </summary>
        /// <param name="websocket">New WebSocket instance.</param>
        /// <remarks>
        ///     This method exists for compatibility support with architectures registering events only once at startup. Using new
        ///     instance is recommended.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     If the provided WebSocket instance is not in the
        ///     <see cref="WebSocketState.None" /> state.
        /// </exception>
        void Reset(IWebSocket websocket);

        /// <summary>
        ///     Gets the WebSocket used by the client.
        /// </summary>
        /// <returns>The WebSocket used by the client.</returns>
        IWebSocket GetUnderlyingWebSocket();

        /// <summary>
        ///     Raised when any connection-related error occurs.
        /// </summary>
        event EventHandler<Exception>? ConnectionError;

        /// <summary>
        ///     Raised when connection is closed.
        /// </summary>
        event EventHandler<WebSocketCloseEventArgs>? ConnectionClosed;

        /// <summary>
        ///     Raised when connection is successfully opened.
        /// </summary>
        event EventHandler? ConnectionOpened;

        /// <summary>
        ///     Raised when scene is removed.
        /// </summary>
        event EventHandler<BareSceneEventArgs>? SceneRemoved;

        /// <summary>
        ///     Raised when scene base is updated (e.g. duplication or name/description update).
        /// </summary>
        event EventHandler<BareSceneEventArgs>? SceneBaseUpdated;

        /// <summary>
        ///     Raised when state of scene changes (stopping/stopped/starting/started).
        /// </summary>
        event EventHandler<SceneStateEventArgs>? SceneState;

        /// <summary>
        ///     Raised when action object is added.
        /// </summary>
        event EventHandler<ActionObjectEventArgs>? ActionObjectAdded;

        /// <summary>
        ///     Raised when action object is removed.
        /// </summary>
        event EventHandler<ActionObjectEventArgs>? ActionObjectRemoved;

        /// <summary>
        ///     Raised when action object is updated (e.g. translated).
        /// </summary>
        event EventHandler<ActionObjectEventArgs>? ActionObjectUpdated;

        /// <summary>
        ///     Raised when action point is added.
        /// </summary>
        event EventHandler<BareActionPointEventArgs>? ActionPointAdded;

        /// <summary>
        ///     Raised when action point is updated (e.g. translated).
        /// </summary>
        event EventHandler<BareActionPointEventArgs>? ActionPointUpdated;

        /// <summary>
        ///     Raised when action point base is updated (e.g. renamed).
        /// </summary>
        event EventHandler<BareActionPointEventArgs>? ActionPointBaseUpdated;

        /// <summary>
        ///     Raised when action point is removed.
        /// </summary>
        event EventHandler<BareActionPointEventArgs>? ActionPointRemoved;

        /// <summary>
        ///     Raised when project override is added.
        /// </summary>
        event EventHandler<ParameterEventArgs>? ProjectOverrideAdded;

        /// <summary>
        ///     Raised when project override is updated (existing named value changed).
        /// </summary>
        event EventHandler<ParameterEventArgs>? ProjectOverrideUpdated;

        /// <summary>
        ///     Raised when project override is removed.
        /// </summary>
        event EventHandler<ParameterEventArgs>? ProjectOverrideRemoved;

        /// <summary>
        ///     Raised when action is added.
        /// </summary>
        event EventHandler<ActionEventArgs>? ActionAdded;

        /// <summary>
        ///     Raised when action is updated (e.g. parameters or flows).
        /// </summary>
        event EventHandler<ActionEventArgs>? ActionUpdated;

        /// <summary>
        ///     Raised when action base is updated (e.g. rename).
        /// </summary>
        event EventHandler<ActionEventArgs>? ActionBaseUpdated;

        /// <summary>
        ///     Raised when action is removed.
        /// </summary>
        event EventHandler<BareActionEventArgs>? ActionRemoved;

        /// <summary>
        ///     Raised when logic item is added.
        /// </summary>
        event EventHandler<LogicItemEventArgs>? LogicItemAdded;

        /// <summary>
        ///     Raised when logic item is updated.
        /// </summary>
        event EventHandler<LogicItemEventArgs>? LogicItemUpdated;

        /// <summary>
        ///     Raised when logic item is removed.
        /// </summary>
        event EventHandler<LogicItemEventArgs>? LogicItemRemoved;

        /// <summary>
        ///     Raised when new action point orientation is added.
        /// </summary>
        event EventHandler<OrientationEventArgs>? OrientationAdded;

        /// <summary>
        ///     Raised when action point orientation is updated.
        /// </summary>
        event EventHandler<OrientationEventArgs>? OrientationUpdated;

        /// <summary>
        ///     Raised when action point orientation base is updated (e.g. rename).
        /// </summary>
        event EventHandler<OrientationEventArgs>? OrientationBaseUpdated;

        /// <summary>
        ///     Raised when action point orientation is removed.
        /// </summary>
        event EventHandler<OrientationEventArgs>? OrientationRemoved;

        /// <summary>
        ///     Raised when new action point joints are added.
        /// </summary>
        event EventHandler<JointsEventArgs>? JointsAdded;

        /// <summary>
        ///     Raised when action point joints are updated.
        /// </summary>
        event EventHandler<JointsEventArgs>? JointsUpdated;

        /// <summary>
        ///     Raised when action point joints base is updated (e.g. rename).
        /// </summary>
        event EventHandler<JointsEventArgs>? JointsBaseUpdated;

        /// <summary>
        ///     Raised when action point joints are removed.
        /// </summary>
        event EventHandler<JointsEventArgs>? JointsRemoved;

        /// <summary>
        ///     Raised when new object type is added.
        /// </summary>
        /// <remarks>
        ///     Be careful that this event doesn't represent an instance of object type (action object) being added/removed from a
        ///     scene - for that see <see cref="IArcor2Client.ActionObjectAdded" /> and related events.
        ///     This event is rather used for signaling dynamic changes to the object type database (such as is the case with
        ///     virtual objects <see cref="IArcor2Client.AddVirtualCollisionObjectToSceneAsync" />).
        /// </remarks>
        event EventHandler<ObjectTypesEventArgs>? ObjectTypeAdded;

        /// <summary>
        ///     Raised when new object type is updated.
        /// </summary>
        /// <remarks>
        ///     Be careful that this event doesn't represent an instance of object type (action object) being added/removed from a
        ///     scene - for that see <see cref="IArcor2Client.ActionObjectAdded" /> and related events.
        ///     This event is rather used for signaling dynamic changes to the object type database (such as is the case with
        ///     virtual objects <see cref="IArcor2Client.AddVirtualCollisionObjectToSceneAsync" />).
        /// </remarks>
        event EventHandler<ObjectTypesEventArgs>? ObjectTypeUpdated;

        /// <summary>
        ///     Raised when new object type is removed.
        /// </summary>
        /// <remarks>
        ///     Be careful that this event doesn't represent an instance of object type (action object) being added/removed from a
        ///     scene - for that see <see cref="IArcor2Client.ActionObjectAdded" /> and related events.
        ///     This event is rather used for signaling dynamic changes to the object type database (such as is the case with
        ///     virtual objects <see cref="IArcor2Client.AddVirtualCollisionObjectToSceneAsync" />).
        /// </remarks>
        event EventHandler<ObjectTypesEventArgs>? ObjectTypeRemoved;

        /// <summary>
        ///     Raised when robot moves to a pose (start/end).
        /// </summary>
        event EventHandler<RobotMoveToPoseEventArgs>? RobotMoveToPose;

        /// <summary>
        ///     Raised when robot moves to a joint (start/end).
        /// </summary>
        event EventHandler<RobotMoveToJointsEventArgs>? RobotMoveToJoints;

        /// <summary>
        ///     Raised when robot moves to action point orientation (start/end).
        /// </summary>
        event EventHandler<RobotMoveToActionPointOrientationEventArgs>? RobotMoveToActionPointOrientation;

        /// <summary>
        ///     Raised when robot moves to action point joints (start/end).
        /// </summary>
        event EventHandler<RobotMoveToActionPointJointsEventArgs>? RobotMoveToActionPointJoints;

        /// <summary>
        ///     Raised when hand teaching mode is enabled/disabled.
        /// </summary>
        event EventHandler<HandTeachingModeEventArgs>? HandTeachingMode;

        /// <summary>
        ///     Raised when new end effector poses.
        /// </summary>
        event EventHandler<RobotEndEffectorUpdatedEventArgs>? RobotEndEffectorUpdated;

        /// <summary>
        ///     Raised on new joints values.
        /// </summary>
        event EventHandler<RobotJointsUpdatedEventArgs>? RobotJointsUpdated;

        /// <summary>
        ///     Raised when project is saved by the server.
        /// </summary>
        event EventHandler? ProjectSaved;

        /// <summary>
        ///     Raised when server finds open project for the user, and it is requesting the client UI to open it (e.g. such as
        ///     when the user quickly reconnects).
        /// </summary>
        event EventHandler<OpenProjectEventArgs>? ProjectOpened;

        /// <summary>
        ///     Raised when server closes a project, and it is requesting the client UI to close it.
        /// </summary>
        event EventHandler? ProjectClosed;

        /// <summary>
        ///     Raised when project base is updated (e.g. rename).
        /// </summary>
        event EventHandler<BareProjectEventArgs>? ProjectBaseUpdated;

        /// <summary>
        ///     Raised when project is removed.
        /// </summary>
        event EventHandler<BareProjectEventArgs>? ProjectRemoved;

        /// <summary>
        ///     Raised when project parameter is added.
        /// </summary>
        event EventHandler<ProjectParameterEventArgs>? ProjectParameterAdded;

        /// <summary>
        ///     Raised when project parameter is updated.
        /// </summary>
        event EventHandler<ProjectParameterEventArgs>? ProjectParameterUpdated;

        /// <summary>
        ///     Raised when project parameter is removed.
        /// </summary>
        event EventHandler<ProjectParameterEventArgs>? ProjectParameterRemoved;

        /// <summary>
        ///     Raised when scene is saved by the server.
        /// </summary>
        event EventHandler? SceneSaved;

        /// <summary>
        ///     Raised when server finds open scene for the user, and it is requesting the client UI to open it (e.g. such as when
        ///     the user quickly reconnects).
        /// </summary>
        event EventHandler<OpenSceneEventArgs>? SceneOpened;

        /// <summary>
        ///     Raised when server closes a scene, and it is requesting the client UI to close it.
        /// </summary>
        event EventHandler? SceneClosed;

        /// <summary>
        ///     Raised when the server is requesting the client UI to show the main screen (e.g. after project/scene is closed).
        /// </summary>
        event EventHandler<ShowMainScreenEventArgs>? ShowMainScreen;

        /// <summary>
        ///     Raised when objects get locked by a user.
        /// </summary>
        event EventHandler<ObjectsLockEventArgs>? ObjectsLocked;

        /// <summary>
        ///     Raised when objects get unlocked.
        /// </summary>
        event EventHandler<ObjectsLockEventArgs>? ObjectsUnlocked;

        /// <summary>
        ///     Raised when server notifies beginning of the action execution triggered while editing a project.
        /// </summary>
        event EventHandler<ActionExecutionEventArgs>? ActionExecution;

        /// <summary>
        ///     Raised when server notifies that action execution was cancelled.
        /// </summary>
        event EventHandler? ActionCancelled;

        /// <summary>
        ///     Raised when server notifies the result of the action execution triggered while editing a project.
        /// </summary>
        event EventHandler<ActionResultEventArgs>? ActionResult;

        /// <summary>
        ///     Raised when the state of long-running process changes.
        /// </summary>
        event EventHandler<ProcessStateEventArgs>? ProcessState;

        /// <summary>
        ///     Raised when new package is added.
        /// </summary>
        event EventHandler<PackageEventArgs>? PackageAdded;

        /// <summary>
        ///     Raised when package is updated (e.g. renamed)
        /// </summary>
        event EventHandler<PackageEventArgs>? PackageUpdated;

        /// <summary>
        ///     Raised when package is removed.
        /// </summary>
        event EventHandler<PackageEventArgs>? PackageRemoved;

        /// <summary>
        ///     Raised when package is initialized and ready to execute.
        /// </summary>
        event EventHandler<PackageInfoEventArgs>? PackageInfo;

        /// <summary>
        ///     Raised when execution status of a package changes.
        /// </summary>
        event EventHandler<PackageStateEventArgs>? PackageState;

        /// <summary>
        ///     Raised when error occurs while running a package.
        /// </summary>
        event EventHandler<PackageExceptionEventArgs>? PackageException;

        /// <summary>
        ///     Raised while running a package before an execution of an action (parameters and other information).
        /// </summary>
        event EventHandler<ActionStateBeforeEventArgs>? ActionStateBefore;

        /// <summary>
        ///     Raised while running a package after n execution of an action (returned value and other information).
        /// </summary>
        event EventHandler<ActionStateAfterEventArgs>? ActionStateAfter;

        /// <summary>
        ///     Establishes a connection to ARCOR2 server.
        /// </summary>
        /// <param name="domain">Domain of the ARCOR2 server</param>
        /// <param name="port">Port od the ARCOR2 server</param>
        /// <exception cref="UriFormatException" />
        /// <exception cref="InvalidOperationException" />
        Task ConnectAsync(string domain, ushort port);

        /// <summary>
        ///     Establishes a connection to ARCOR2 server.
        /// </summary>
        /// <param name="uri">Full WebSocket URI</param>
        /// <exception cref="UriFormatException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="Arcor2ConnectionException"> When inner WebSocket fails to connect.</exception>
        Task ConnectAsync(Uri uri);

        /// <summary>
        ///     Closes a connection to ARCOR2 sever.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        Task CloseAsync();

        /// <summary>
        ///     Sends a request to remove specified object type.
        /// </summary>
        /// <remarks>For bulk removal, use <see cref="IArcor2Client.RemoveObjectTypesAsync" />.</remarks>
        /// <param name="args">Object Type.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        Task<DeleteObjectTypesResponse> RemoveObjectTypeAsync(string args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to remove specified object types.
        /// </summary>
        /// <param name="args">A list of object types.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<DeleteObjectTypesResponse> RemoveObjectTypesAsync(List<string> args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to retrieve object types supported by the server.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetObjectTypesResponse> GetObjectTypesAsync();

        /// <summary>
        ///     Sends a request to retrieve list of available actions for an object type.
        /// </summary>
        /// <param name="args">The object type.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetActionsResponse> GetActionsAsync(TypeArgs args);

        /// <summary>
        ///     Sends a request to save the current scene.
        /// </summary>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<SaveSceneResponse> SaveSceneAsync(bool isDryRun = false);

        /// <summary>
        ///     Sends a request to save the current project.
        /// </summary>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<SaveProjectResponse> SaveProjectAsync(bool isDryRun = false);

        /// <summary>
        ///     Sends a request to open a project.
        /// </summary>
        /// <param name="args">The project ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<OpenProjectResponse> OpenProjectAsync(IdArgs args);

        /// <summary>
        ///     Sends a request to run a package.
        /// </summary>
        /// <param name="args">The run parameters.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RunPackageResponse> RunPackageAsync(RunPackageRequestArgs args);

        /// <summary>
        ///     Sends a request to build the current project into temporary package and run it.
        ///     This package is not saved on execution unit and is removed immediately after package execution.
        /// </summary>
        /// <param name="args">The debugging execution parameters.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<TemporaryPackageResponse> RunTemporaryPackageAsync(TemporaryPackageRequestArgs args);

        /// <summary>
        ///     Sends a request to terminate a running package.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<StopPackageResponse> StopPackageAsync();

        /// <summary>
        ///     Sends a request to pause a running package.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<PausePackageResponse> PausePackageAsync();

        /// <summary>
        ///     Sends a request to resume a pause package.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ResumePackageResponse> ResumePackageAsync();

        /// <summary>
        ///     Sends a request to upload a package.
        /// </summary>
        /// <param name="args">The package ID and its data.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UploadPackageResponse> UploadPackageAsync(UploadPackageRequestArgs args);

        /// <summary>
        ///     Sends a request to rename a package.
        /// </summary>
        /// <param name="args">The package ID and new name.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RenamePackageResponse> RenamePackageAsync(RenamePackageRequestArgs args);

        /// <summary>
        ///     Sends a request to retrieve a list of available packages.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ListPackagesResponse> ListPackagesAsync();

        /// <summary>
        ///     Sends a request to update pose of action point to the robot's end effector.
        /// </summary>
        /// <param name="args">Action point ID and a robot.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateActionPointUsingRobotResponse> UpdateActionPointUsingRobotAsync(
            UpdateActionPointUsingRobotRequestArgs args);

        /// <summary>
        ///     Sends a request to update pose (position and orientation) of an action object.
        /// </summary>
        /// <param name="args">Action object ID and a new pose.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateObjectPoseResponse> UpdateActionObjectPoseAsync(UpdateObjectPoseRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to update pose of action object to the robot's end effector.
        /// </summary>
        /// <param name="args">Robot and pivot option.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateObjectPoseUsingRobotResponse> UpdateObjectPoseUsingRobotAsync(
            UpdateObjectPoseUsingRobotRequestArgs args);

        /// <summary>
        ///     Sends a request to define a new object type.
        /// </summary>
        /// <param name="args">The object type definition.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<NewObjectTypeResponse> AddNewObjectTypeAsync(ObjectTypeMeta args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to start an object aiming process for a robot.
        /// </summary>
        /// <remarks>Only possible when the scene is online and all write locks for object and robot are acquired in advance.</remarks>
        /// <param name="args">Action object ID and a robot.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ObjectAimingStartResponse> ObjectAimingStartAsync(ObjectAimingStartRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to save current position of selected robot's end effector during the object aiming process as the
        ///     specified index.
        /// </summary>
        /// <param name="args">ID of currently selected focus point.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ObjectAimingAddPointResponse> ObjectAimingAddPointAsync(ObjectAimingAddPointRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to finish object aiming process and compute a new pose of the object.
        /// </summary>
        /// <remarks>On failure, you can do another attempt or invoke <see cref="IArcor2Client.ObjectAimingCancelAsync" />.</remarks>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ObjectAimingDoneResponse> ObjectAimingDoneAsync(bool isDryRun = false);

        /// <summary>
        ///     Sends a request to cancel current object aiming process.
        /// </summary>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ObjectAimingCancelResponse> ObjectAimingCancelAsync(bool isDryRun = false);

        /// <summary>
        ///     Sends a request to retrieve a list of available scenes.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ListScenesResponse> ListScenesAsync();

        /// <summary>
        ///     Sends a request to retrieve a list of available projects.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ListProjectsResponse> ListProjectsAsync();

        /// <summary>
        ///     Sends a request to add a new action object to a scene.
        /// </summary>
        /// <param name="args">The name, type, pose and parameters of the action object.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<AddObjectToSceneResponse> AddActionObjectToSceneAsync(AddObjectToSceneRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to remove an action object from scene.
        /// </summary>
        /// <param name="args">Action Object ID and if the removal should be forced.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RemoveFromSceneResponse> RemoveActionObjectFromSceneAsync(RemoveFromSceneRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to open a scene.
        /// </summary>
        /// <param name="args">Scene ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<OpenSceneResponse> OpenSceneAsync(IdArgs args);

        /// <summary>
        ///     Sends a request to get all available values for selected parameter.
        /// </summary>
        /// <param name="args">Action object ID, parameter ID, and a list of parent parameters. </param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ActionParamValuesResponse> GetActionParameterValuesAsync(ActionParamValuesRequestArgs args);

        /// <summary>
        ///     Sends a request to execute selected action.
        /// </summary>
        /// <param name="args">Action ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ExecuteActionResponse> ExecuteActionAsync(ExecuteActionRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to cancel an execution of currently running action.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<CancelActionResponse> CancelActionAsync();

        /// <summary>
        ///     Sends a request to retrieve information about the server (server version, API version, supported parameter types
        ///     and RPCs).
        /// </summary>
        /// <returns>THe response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<SystemInfoResponse> GetSystemInfoAsync();

        /// <summary>
        ///     Sends a request to build a project into a package.
        /// </summary>
        /// <param name="args">The project ID and resulting package name.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<BuildProjectResponse> BuildProjectAsync(BuildProjectRequestArgs args);

        /// <summary>
        ///     Sends a request to create a new scene.
        /// </summary>
        /// <param name="args">Name and description.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<NewSceneResponse> AddNewSceneAsync(NewSceneRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to remove a scene.
        /// </summary>
        /// <param name="args">ID of the scene.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<DeleteSceneResponse> RemoveSceneAsync(IdArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to rename a scene.
        /// </summary>
        /// <param name="args">ID and a new name of the scene.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RenameSceneResponse> RenameSceneAsync(RenameArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to rename an action object.
        /// </summary>
        /// <remarks>This RPC automatically releases write lock.</remarks>
        /// <param name="args">The action object ID and new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RenameObjectResponse> RenameActionObjectAsync(RenameArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to close a scene.
        /// </summary>
        /// <param name="args">Should the action be forced (e.g. in case of unsaved changes).</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<CloseSceneResponse> CloseSceneAsync(CloseSceneRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to retrieve existing project of a scene.
        /// </summary>
        /// <param name="args">Scene ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ProjectsWithSceneResponse> GetProjectsWithSceneAsync(IdArgs args);

        /// <summary>
        ///     Sends a request to create a new project.
        /// </summary>
        /// <param name="args">Parent scene ID, project name, description, and if it should have its own logic.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<NewProjectResponse> AddNewProjectAsync(NewProjectRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to remove a project.
        /// </summary>
        /// <param name="args">Project ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<DeleteProjectResponse> RemoveProjectAsync(IdArgs args);

        /// <summary>
        ///     Sends a request to remove a package.
        /// </summary>
        /// <param name="args">Package ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<DeletePackageResponse> RemovePackageAsync(IdArgs args);

        /// <summary>
        ///     Sends a request to create a new action point.
        /// </summary>
        /// <param name="args">Name, position, and optional parent.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<AddActionPointResponse> AddActionPointAsync(AddActionPointRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to create a new action point for robot's end effector.
        /// </summary>
        /// <param name="args">Robot (action object) ID, name, end effector ID, and optional arm ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<AddApUsingRobotResponse> AddActionPointUsingRobotAsync(AddApUsingRobotRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to update position of an action point.
        /// </summary>
        /// <param name="args">Action point ID and a new position.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateActionPointPositionResponse> UpdateActionPointPositionAsync(
            UpdateActionPointPositionRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to change the parent of an action point.
        /// </summary>
        /// <remarks>
        ///     Only the child has to be locked manually. The parent is locked automatically and then both child and parent
        ///     are unlocked automatically.
        /// </remarks>
        /// <param name="args">Action point ID and the ID of the new parent.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateActionPointParentResponse> UpdateActionPointParentAsync(
            UpdateActionPointParentRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to create a new orientation of an action point.
        /// </summary>
        /// <param name="args">Action point ID, orientation and a name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<AddActionPointOrientationResponse> AddActionPointOrientationAsync(
            AddActionPointOrientationRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to remove orientation from an action point.
        /// </summary>
        /// <param name="args">Orientation ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RemoveActionPointOrientationResponse> RemoveActionPointOrientationAsync(
            RemoveActionPointOrientationRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to update orientation of an action point.
        /// </summary>
        /// <param name="args">Orientation ID and a new orientation data.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateActionPointOrientationResponse> UpdateActionPointOrientationAsync(
            UpdateActionPointOrientationRequestArgs args);

        /// <summary>
        ///     Sends a request to create a new orientation of robot end effector's action point.
        /// </summary>
        /// <param name="args">Action point ID, robot information, and a name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<AddActionPointOrientationUsingRobotResponse> AddActionPointOrientationUsingRobotAsync(
            AddActionPointOrientationUsingRobotRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to update orientation of robot end effector's action point.
        /// </summary>
        /// <param name="args">Orientation ID and robot information.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateActionPointOrientationUsingRobotResponse> UpdateActionPointOrientationUsingRobotAsync(
            UpdateActionPointOrientationUsingRobotRequestArgs args);

        /// <summary>
        ///     Sends a request to create new joints of robot's (end effector) action point.
        /// </summary>
        /// <param name="args">Action point ID, robot/arm/end effector ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<AddActionPointJointsUsingRobotResponse> AddActionPointJointsUsingRobotAsync(
            AddActionPointJointsUsingRobotRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to update joints of an action point.
        /// </summary>
        /// <param name="args">Joints ID and a list of joint names and values.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateActionPointJointsResponse> UpdateActionPointJointsAsync(
            UpdateActionPointJointsRequestArgs args);

        /// <summary>
        ///     Sends a request to update joints of robot's (end effector) action point.
        /// </summary>
        /// <param name="args">Joints ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateActionPointJointsUsingRobotResponse> UpdateActionPointJointsUsingRobotAsync(
            UpdateActionPointJointsUsingRobotRequestArgs args);

        /// <summary>
        ///     Sends a request to rename an action point.
        /// </summary>
        /// <param name="args">Action point ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RenameActionPointResponse> RenameActionPointAsync(RenameActionPointRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to rename joints of an action point.
        /// </summary>
        /// <param name="args">Joints ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RenameActionPointJointsResponse> RenameActionPointJointsAsync(
            RenameActionPointJointsRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to rename orientation of an action point.
        /// </summary>
        /// <param name="args">Orientation ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RenameActionPointOrientationResponse> RenameActionPointOrientationAsync(
            RenameActionPointOrientationRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to move selected robot to an action point.
        /// </summary>
        /// <param name="args">
        ///     Robot ID, speed (0-1f), optional end effector ID, either an orientation or joints ID, safe flag
        ///     (collision checks), linear flag, and optional arm ID.
        /// </param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<MoveToActionPointResponse> MoveToActionPointAsync(MoveToActionPointRequestArgs args);

        /// <summary>
        ///     Sends a request to move selected robot to a pose.
        /// </summary>
        /// <remarks>
        ///     Either position or orientation must be filled.
        /// </remarks>
        /// <param name="args">
        ///     Robot ID, end effector ID, speed (0-1f), optional position or orientation, safe flag (collision
        ///     checks), linear flag, and optional arm ID.
        /// </param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<MoveToPoseResponse> MoveToPoseAsync(MoveToPoseRequestArgs args);

        /// <summary>
        ///     Sends a request to remove joints of an action point.
        /// </summary>
        /// <param name="args">Joints ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RemoveActionPointJointsResponse> RemoveActionPointJointsAsync(
            RemoveActionPointJointsRequestArgs args);

        /// <summary>
        ///     Sends a request to add an action to action point.
        /// </summary>
        /// <param name="args">Action point ID, name, action type, parameters, and flows.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<AddActionResponse> AddActionAsync(AddActionRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to update an action.
        /// </summary>
        /// <param name="args">Action ID and updated parameters and flows.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateActionResponse> UpdateActionAsync(UpdateActionRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to remove an action.
        /// </summary>
        /// <param name="args">Action ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RemoveActionResponse> RemoveActionAsync(IdArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to rename an action.
        /// </summary>
        /// <param name="args">Action ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RenameActionResponse> RenameActionAsync(RenameActionRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to add a new logic item (a connection between two actions) to the project.
        /// </summary>
        /// <remarks>
        ///     Only the first action has to be locked manually. The second action is locked automatically by the server and
        ///     then both are also unlocked automatically.
        /// </remarks>
        /// <param name="args">Start (ID of first action), end (ID of second action), and an optional condition for the logic item.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<AddLogicItemResponse> AddLogicItemAsync(AddLogicItemRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to update a logic item (a connection between two actions).
        /// </summary>
        /// <param name="args">
        ///     Logic item ID, start (ID of first action), end (ID of second action), and an optional condition for
        ///     the logic item.
        /// </param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateLogicItemResponse> UpdateLogicItemAsync(UpdateLogicItemRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to remove a logic item (a connection between two actions).
        /// </summary>
        /// <param name="args">Logic item ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RemoveLogicItemResponse> RemoveLogicItemAsync(RemoveLogicItemRequestArgs args);

        /// <summary>
        ///     Sends a request to rename a project.
        /// </summary>
        /// <param name="args">Project ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RenameProjectResponse> RenameProjectAsync(RenameProjectRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to remove an action point.
        /// </summary>
        /// <param name="args">Action point ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RemoveActionPointResponse> RemoveActionPointAsync(IdArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to close a project.
        /// </summary>
        /// <param name="args">Should the action be forced (e.g. in case of unsaved changes).</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<CloseProjectResponse> CloseProjectAsync(CloseProjectRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to get a pose of robot's end effector.
        /// </summary>
        /// <param name="args">Robot, end effector (and arm) ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetEndEffectorPoseResponse> GetEndEffectorPoseAsync(GetEndEffectorPoseRequestArgs args);

        /// <summary>
        ///     Sends a request to register/unregister itself for robot's end effector/joints update events.
        /// </summary>
        /// <param name="args">Robot ID, type (eef/joints), and if the request is registering or unregistering.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RegisterForRobotEventResponse> RegisterForRobotEventAsync(
            RegisterForRobotEventRequestArgs args);

        /// <summary>
        ///     Sends a request to get information about a robot.
        /// </summary>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetRobotMetaResponse> GetRobotMetaAsync();

        /// <summary>
        ///     Sends a request to get a list of end effectors of a robot.
        /// </summary>
        /// <param name="args">Robot (and arm) ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetEndEffectorsResponse> GetRobotEndEffectorsAsync(GetEndEffectorsRequestArgs args);

        /// <summary>
        ///     Sends a request to get a list of arms of a robot.
        /// </summary>
        /// <param name="args">Robot ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetRobotArmsResponse> GetRobotArmsAsync(GetRobotArmsRequestArgs args);

        /// <summary>
        ///     Sends a request to start an offline scene.
        /// </summary>
        /// <remarks>All locks must be freed before starting a scene.</remarks>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<StartSceneResponse> StartSceneAsync(bool isDryRun = false);

        /// <summary>
        ///     Sends a request to stop an online scene.
        /// </summary>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<StopSceneResponse> StopSceneAsync(bool isDryRun = false);

        /// <summary>
        ///     Sends a request to update action object's parameters.
        /// </summary>
        /// <param name="args">Action object ID and a list of Name-Type-Value parameters.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateObjectParametersResponse> UpdateActionObjectParametersAsync(
            UpdateObjectParametersRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to add an override to project's action object.
        /// </summary>
        /// <param name="args">Action object ID and a new parameter override.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<AddOverrideResponse> AddOverrideAsync(AddOverrideRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to update an override of project's action object.
        /// </summary>
        /// <param name="args">Action object ID and the parameter override.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateOverrideResponse> UpdateOverrideAsync(UpdateOverrideRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to remove an override of project's action object.
        /// </summary>
        /// <param name="args">Action object ID and the parameter override.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<DeleteOverrideResponse> RemoveOverrideAsync(DeleteOverrideRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to calculate the inverse kinematics for a robot's end-effector and update it.
        /// </summary>
        /// <param name="args">
        ///     Robot ID, end effector ID, target pose, optional start joints, collision avoidance flag, and
        ///     optional arm ID.
        /// </param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<InverseKinematicsResponse> InverseKinematicsAsync(InverseKinematicsRequestArgs args);

        /// <summary>
        ///     Sends a request to calculate the forward kinematics for a robot's joints and update it.
        /// </summary>
        /// <param name="args">Robot ID, end effector ID, joint positions, and optional arm ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ForwardKinematicsResponse> ForwardKinematicsAsync(ForwardKinematicsRequestArgs args);

        /// <summary>
        ///     Sends a request to calibrate a robot.
        /// </summary>
        /// <remarks>Robot with a model and calibrated camera is required.</remarks>
        /// <param name="args">Robot ID, camera ID, and if the robot should move into the calibration pose flag.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<CalibrateRobotResponse> CalibrateRobotAsync(CalibrateRobotRequestArgs args);

        /// <summary>
        ///     Sends a request to calibrate a camera action object.
        /// </summary>
        /// <param name="args">Camera ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<CalibrateCameraResponse> CalibrateCameraAsync(CalibrateCameraRequestArgs args);

        /// <summary>
        ///     Sends a request to get a color image from camera.
        /// </summary>
        /// <remarks>
        ///     The image is encoded as a Latin-1 string representation of JPEG image data,
        ///     where each byte of the JPEG is mapped directly to its corresponding Latin-1 character
        /// </remarks>
        /// <param name="args">Camera ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<CameraColorImageResponse> GetCameraColorImageAsync(CameraColorImageRequestArgs args);

        /// <summary>
        ///     Sends a request to estimate the pose of a camera.
        /// </summary>
        /// <param name="args">Camera parameters, image (latin-1 encoded), and inverse flag.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetCameraPoseResponse> GetCameraPoseAsync(GetCameraPoseRequestArgs args);

        /// <summary>
        ///     Sends a request to estimate markers corners.
        /// </summary>
        /// <param name="args">Camera parameters, image (latin-1 encoded).</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<MarkersCornersResponse> GetMarkersCornersAsync(MarkersCornersRequestArgs args);

        /// <summary>
        ///     Sends a request to lock an object for writing.
        /// </summary>
        /// <param name="args">Object ID and if the whole object subtree should be locked.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<WriteLockResponse> WriteLockAsync(WriteLockRequestArgs args);

        /// <summary>
        ///     Sends a request to unlock an object for writing.
        /// </summary>
        /// <param name="args">Object ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<WriteUnlockResponse> WriteUnlockAsync(WriteUnlockRequestArgs args);

        /// <summary>
        ///     Sends a request to lock an object for reading.
        /// </summary>
        /// <param name="args">Object ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ReadLockResponse> ReadLockAsync(ReadLockRequestArgs args);

        /// <summary>
        ///     Sends a request to unlock an object for reading.
        /// </summary>
        /// <param name="args">Object ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ReadUnlockResponse> ReadUnlockAsync(ReadUnlockRequestArgs args);

        /// <summary>
        ///     Sends a request to update lock type (object/tree).
        /// </summary>
        /// <param name="args">Object ID and new lock type.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateLockResponse> UpdateLockAsync(UpdateLockRequestArgs args);

        /// <summary>
        ///     Sends a request to toggle hand teaching mode.
        /// </summary>
        /// <param name="args">Robot ID, toggle.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<HandTeachingModeResponse> SetHandTeachingModeAsync(HandTeachingModeRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to duplicate an action point.
        /// </summary>
        /// <param name="args">Object ID and boolean if the object tree should be locked.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<CopyActionPointResponse> DuplicateActionPointAsync(CopyActionPointRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to step robot's end effector.
        /// </summary>
        /// <remarks>Mode.User is not yet supported as of ARCOR2 server v1.5.0.</remarks>
        /// <param name="args">
        ///     Robot ID, end effector ID, axis, what (position/orientation), mode, step size, safe flag,
        ///     optional pose (e.g. if relative), speed (0-1f), linear movement flag, and optional arm ID.
        /// </param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<StepRobotEefResponse> StepRobotEndEffectorAsync(StepRobotEefRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to set the end effector perpendicular to the world frame.
        /// </summary>
        /// <param name="args">Robot ID, end effector ID, safety flag, speed, linear movement flag, and optional arm ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The result.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<SetEefPerpendicularToWorldResponse> SetEndEffectorPerpendicularToWorldAsync(
            SetEefPerpendicularToWorldRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to register user for this session.
        /// </summary>
        /// <param name="args">Username.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequestArgs args);

        /// <summary>
        ///     Sends a request to add a project parameter.
        /// </summary>
        /// <param name="args">Parameter in Name-Type-Value format.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<AddProjectParameterResponse> AddProjectParameterAsync(AddProjectParameterRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to update a value of project parameter.
        /// </summary>
        /// <param name="args">Project parameter ID and a new value.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateProjectParameterResponse> UpdateProjectParameterAsync(
            UpdateProjectParameterRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to remove project parameter.
        /// </summary>
        /// <param name="args">Project parameter ID.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<RemoveProjectParameterResponse> RemoveProjectParameterAsync(
            RemoveProjectParameterRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to update object model of an object type.
        /// </summary>
        /// <param name="args">Object type ID and the object model.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateObjectModelResponse> UpdateObjectTypeModelAsync(UpdateObjectModelRequestArgs args,
            bool isDryRun = false);

        /// <summary>
        ///     Sends a request to add a virtual collision object to a scene.
        /// </summary>
        /// <param name="args">Name, pose, and the object.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<AddVirtualCollisionObjectToSceneResponse> AddVirtualCollisionObjectToSceneAsync(
            AddVirtualCollisionObjectToSceneRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to duplicate a scene.
        /// </summary>
        /// <param name="args">Scene ID and a new name.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<CopySceneResponse> DuplicateSceneAsync(CopySceneRequestArgs args);

        /// <summary>
        ///     Sends a request to duplicate a scene.
        /// </summary>
        /// <param name="args">Project ID and a new name.</param>
        /// <param name="isDryRun">If true, the request will be a dry run and have no persistent effect.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<CopyProjectResponse>
            DuplicateProjectAsync(CopyProjectRequestArgs args, bool isDryRun = false);

        /// <summary>
        ///     Sends a request to step currently executing package by actions.
        /// </summary>
        /// <remarks>The execution must be paused before calling.</remarks>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<StepActionResponse> StepActionAsync();

        /// <summary>
        ///     Sends a request to get camera's color parameters.
        /// </summary>
        /// <param name="args">Camera ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<CameraColorParametersResponse> GetCameraColorParametersAsync(
            CameraColorParametersRequestArgs args);

        /// <summary>
        ///     Sends a request to get a list of robot's gripper IDs.
        /// </summary>
        /// <param name="args">Robot ID and optional arm ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetGrippersResponse> GetGrippersAsync(GetGrippersRequestArgs args);

        /// <summary>
        ///     Sends a request to get a project using ID.
        /// </summary>
        /// <param name="args">Project ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetProjectResponse> GetProjectAsync(IdArgs args);

        /// <summary>
        ///     Sends a request to get joints of a robot.
        /// </summary>
        /// <param name="args">Robot (and arm) ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetRobotJointsResponse> GetRobotJointsAsync(GetRobotJointsRequestArgs args);

        /// <summary>
        ///     Sends a request to get a scene using ID.
        /// </summary>
        /// <param name="args">Scene ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetSceneResponse> GetSceneAsync(IdArgs args);

        /// <summary>
        ///     Sends a request to get a list of robot's suctions IDs.
        /// </summary>
        /// <param name="args">Robot ID and optional arm ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<GetSuctionsResponse> GetSuctionsAsync(GetSuctionsRequestArgs args);

        /// <summary>
        ///     Sends a request to move robot joints.
        /// </summary>
        /// <param name="args">Robot ID, speed (0-1f), list of joints, safe flag, and optional arm ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<MoveToJointsResponse> MoveToJointsAsync(MoveToJointsRequestArgs args);

        /// <summary>
        ///     Sends a request to get scene IDs that use specified object type.
        /// </summary>
        /// <param name="args">Object type ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<ObjectTypeUsageResponse> GetObjectTypeUsageAsync(IdArgs args);

        /// <summary>
        ///     Sends a request to get project IDs that use specified action object from a scene.
        /// </summary>
        /// <param name="args">Action object ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<SceneObjectUsageResponse> GetSceneActionObjectUsageAsync(IdArgs args);

        /// <summary>
        ///     Sends a request to stop a robot.
        /// </summary>
        /// <param name="args">Robot ID.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<StopRobotResponse> StopRobotAsync(StopRobotRequestArgs args);

        /// <summary>
        ///     Sends a request to update project's description.
        /// </summary>
        /// <param name="args">Project ID and new description.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateProjectDescriptionResponse> UpdateProjectDescriptionAsync(
            UpdateProjectDescriptionRequestArgs args);

        /// <summary>
        ///     Sends a request to update if project contains logic.
        /// </summary>
        /// <param name="args">Project ID and boolean value indicating if project should have logic.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateProjectHasLogicResponse> UpdateProjectHasLogicAsync(
            UpdateProjectHasLogicRequestArgs args);

        /// <summary>
        ///     Sends a request to update scene's description.
        /// </summary>
        /// <param name="args">Scene ID and new description.</param>
        /// <returns>The response.</returns>
        /// <exception cref="TimeoutException">
        ///     When the response is not received within
        ///     <see cref="Arcor2ClientSettings.RpcTimeout" /> (10 seconds by default).
        /// </exception>
        /// <exception cref="Arcor2ConnectionException">
        ///     When connection fails or the in case of ARCOR2 protocol violation (e.g.
        ///     matching IDs, but mismatching RPC names).
        /// </exception>
        Task<UpdateSceneDescriptionResponse> UpdateSceneDescriptionAsync(
            UpdateSceneDescriptionRequestArgs args);
    }
}