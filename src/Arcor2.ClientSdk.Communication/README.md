# Communication Library

The `Arcor2.ClientSdk.Communication` is a stand-alone communication library designed for a wide range of different ARCOR2 clients.

## Introduction

The ARCOR2 server implements an event-driven API using WebSockets for real-time bidirectional communication between clients and the server. This can make associating request-response messages and generating clients difficult.
The `Arcor2.ClientSdk.Communication` library provides a strongly typed client interface in C# for seamless interaction with ARCOR2 API. 

The library transforms RPCs (request-response exchanges) into Task-based asynchronous methods, providing native support for C# exception handling and integration with .NET's modern TPL library. Events are handled through .NET's event system.

## Usage

The `Arcor2Client` class serves as a primary facade for the library, offering three categories of functionality:

### Control members

These members manage the communication lifecycle and faciliate other features:

```
WebSocketState State { get; }
Task ConnectAsync()
Task CloseAsync()
EventHandler ConnectionOpened
EventHandler<WebSocketCloseEventArgs>? ConnectionClosed
EventHandler<Exception>? ConnectionError
IWebSocket GetUnderlyingWebSocket()
```

The `ConnectAsync()` method is stateful and can thus only be called once (in the `WebSocketState.None` state). New communication sessions require a new instance of `Arcor2Client`.
Make sure to register event handlers before calling this method, as the client will begin accepting messages after the `ConnectionOpened` event is raised.

The `CloseAsync()` method can only be in the `WebSocketState.Open` state. The invocation will close the WebSocket, release all unmanaged resources, and raise the `ConnectionClosed` event.

The `ConnectionError` event is raised for any connection-related issues, with the relevant exception as an argument. Unrecoverable errors also trigger the ConnectionClosed event.

The `GetUnderlyingWebSocket()` method can be used to retrieve the used WebSocket instance for advanced use cases.

### RPC methods

RPC methods abstract the event-driven nature of ARCOR2 RPCs (request-response exchanges) as an asynchronous Task-based method by using the `TaskCompletionSource`.
RPC methods generally follow a pattern:
```
public Task<FooResult> GetFooAsync(GetFooRequestArgs args, isDryRun = false);
```
The `isDryRun` parameter allows simulation of the RPC execution without affecting the server state. This can be useful for testing potential failures.

The returned result types follow this structure:
```
public class FooResult {
	// Internal Message ID
	public int Id { get; set; }

	// Internal name of the RPC
	public string Response { get; set; }

	// Boolean value indicating the success of the request
	public bool Result { get; set; }

	// List of error messages, if unsuccessful (Result==false)
	public List<string> Messages { get; set; }

	// The data of the response, existence and content depend on the specific RPC
	public FooData Data { get; set; }
}
```

The resulting task will be faulted if the client fails to receive a response from the server, with the default timeout being 10 seconds.

### Events

Server events are mapped to .NET events. When the client decodes an event message from the server, the appropriate event will be raised with the corresponding data and, if applicable, parent ID.

The library provides special handling for messages containing the `change_type` field, which have up to four mapped C# events depending on the possible change types (`Added`, `Updated`, `BaseUpdated`, and `Removed`).

```
// Registration of handlers
arcorClient.OpenScene += (sender, args) => NavigateToScene(args);

// Note that the SceneAdded and SceneUpdated are (currently) not possible according to ARCOR2 protocol
arcorClient.SceneBaseUpdated += (sender, args) => UpdateScene(args);
arcorClient.SceneRemove += (sender, args) => RemoveScene(args);

```

### WebSocket Implementations

The support for WebSockets within the .NET ecosystem presents uncertainty, particularly regarding the `System.Net`'s `ClientWebSocket` implementation. 
While Microsoft's documentation explicitly states that `ClientWebSocket` is only available for Windows 8 or later, it seems to work on a wide range of platforms, including Windows, Linux, Android, and iOS. 
For example, Unity's multiplatform `NativeWebSockets` package internally uses the `ClientWebSocket`. The only known unsupported platform is Unity's WebGL.

The library uses the aforementioned `ClientWebSocket` by default. 
In case the implementation is not supported by your platform, you can use your own by implementing the `IWebSocket` interface and injecting it into the `Arcor2Client` class.

```
var client = new Arcor2Client(
    websocket: new CustomWebSocket()
)
```

The requirements for each WebSocket member are listed in the interface's XAML comments. Most importantly, the implementation should fully support concurrency and have a parameterless constructor.
Related classes can be found in the `Arcor2.ClientSdk.Communication.Design` namespace.

### Unity Support and Usage

The library is designed for .NET Standard 2.1, which means it is fully compatible with Unity 2021.2 and later.

For earlier Unity versions (2018+), the library will not work out of the box unless it is retargeted to .NET Standard 2.0.
If needed, the retargeting process is practically feasible and will involve removing or refactoring certain C# features that rely on .NET Standard 2.1, 
such as nullable reference types or read-only members.

The library leverages .NET's Task Parallel Library (TPL) for asynchronous programming, which is supported in Unity with some important caveats discussed below.

Unity APIs (e.g. GameObject, Transform, Debug.Log) are not thread-safe and must be accessed only from the main thread. 
Consider the following method, which results in an `InvalidOperationException`. Exception handling logic is omitted for conciseness.

```
private void RegisterUser() {
    var registrationTask = arcor2Client.RegisterUserAsync(new RegisterUserRequestArgs("john"));

    registrationTask.ContinueWith(task => 
    {
        if(task.Result.Result) {
           NavigateToMenu();
        }
        else {
           Debug.Log($"Error while registering a user. {task.Result.Messages.First()}");
        }
    });

    Task.Run(registrationTask);
}
```

The issue is that the continuation `Task` accesses Unity APIs from a background thread. This can be generally fixed using any of the following approaches:
- The best approach is to convert the synchronous methods to asynchronous methods. The `await` automatically resumes on the main thread after the `Task` completes. Note that this may not always be a feasible approach, especially in large legacy code bases.
```
private async void RegisterUser() {
    var registrationTask = await arcor2Client.RegisterUserAsync(new RegisterUserRequestArgs("john"));

    // This will run on the main thread
    if(registrationTask.Result) {
        NavigateToMenu();
    }
    else {
        Debug.Log($"Error while registering a user. {task.Result.Messages.First()}");
    }
}
```
- Scheduling the continuation `Task` to run on the main thread using `TaskScheduler`.
```
private void RegisterUser() {
    var registrationTask = arcor2Client.RegisterUserAsync(new RegisterUserRequestArgs("john"));

    // This will run on the main thread
    registrationTask.ContinueWith(task => 
    {
        if(task.Result.Result) {
           NavigateToMenu();
        }
        else {
           Debug.Log($"Error while registering a user. {task.Result.Messages.First()}");
        }
    }, TaskScheduler.FromCurrentSynchronizationContext());

    Task.Run(registrationTask);
}
```
- Using a main thread dispatcher, such as the popular `UnityMainThreadDispatcher` library.

```
private void RegisterUser() {
    var registrationTask = arcor2Client.RegisterUserAsync(new RegisterUserRequestArgs("john"));

    registrationTask.ContinueWith(task => 
    {   
        UnityMainThreadDispatcher.Instance.Enqueue(() => 
         {
            // This will run on the main thread
            if(task.Result.Result) {
               NavigateToMenu();
            }
            else {
               Debug.Log($"Error while registering a user. {task.Result.Messages.First()}");
            }
        });
    });

    Task.Run(registrationTask);
}
```
Event consumers of the library must use a similar synchronization method, as the events may be generated from a background thread.

## Contributing

This library is a simple, typed, and most importantly - **maintainable** - interface for the ARCOR2 protocol and all changes should reflect that. 
Complex convenience features (such as a single method performing multiple RPC exchanges) should be implemented in different projects extending, subtyping, or wrapping the `Arcor2Client` class.

### Naming

The ARCOR2 protocol currently lacks consistency in naming conventions across its RPCs and models.
This consistency is particularly important for developers using statically-typed languages like C#, where code completion is a crucial tool to discover or recall method names by keywords.

While the RPC method names are often identical to their corresponding RPCs, we apply specific rules to improve consistency and usability.
Note that these adjustments are limited to method and event names only. 
We deliberately **exclude generated models** from these changes to avoid exponentially increasing maintenance overhead when regenerating OpenAPI models.

Please note that the following rules should not be taken as a dogma. If it makes sense, break them or change them.

- Expand shorthands (e.g. `AddApUsingRobot` => `AddActionPointUsingRobot`, `OnRobotEefUpdated` => `OnRobotEndEffectorUpdated`)
- Correct non-specific, inaccurate, or confusing ARCOR2 terminology (e.g. `ProjectException` => `PackageException`, `GetSceneObjectUsage` => `GetSceneActionObjectUsage`)
- Prefer `Duplicate` over `Copy` and other synonyms (e.g. `CopyProject` => `DuplicateProject`)
- Prefer `Remove` over `Delete` and other synonyms (e.g. `DeleteProject` => `RemoveProject`)
- Use the `Get` (or `List` and others if appropriate) prefix for query RPCs missing it (e.g. `ProjectsWithScene` => `GetProjectsWithScene`)
- Use the `Set` prefix for RPCs setting an option and missing it (e.g. `HandTeachingMode` => `SetHandTeachingMode`)
- Use the `Add` prefix for RPCs creating new entities and missing it (e.g.`NewProject` => `AddNewProject`)



### Implementing Protocol Updates

Reflecting most changes to the ARCOR2 protocol in the library is straightforward and will only require modifications to the `Arcor2Client` class. In general, follow the standard C# conventions and conventions of the existing code which are briefly discussed here.

#### Model properties changes

Most changes to model properties (the JSON data) can be implemented by a simple regeneration of the OpenApi models in the `Arcor2.ClientSdk.Communication.OpenApi` project using the included shell script `tools/generate_models.sh`. 
Changes to integral properties (such as the root `id`, `result`, `event`, etc.) can require larger library updates.


#### RPC Changes

The format of RPCs is as follows:

```
/// RPC for CopyProject
public async Task<CopyProjectResponse> DuplicateProjectAsync(CopyProjectRequestArgs args, bool isDryRun = false) {
    var id = Interlocked.Increment(ref requestId);
    var response = await SendAndWaitAsync(new CopyProjectRequest(id, "CopyProject", args, isDryRun), id);
    return JsonConvert.DeserializeObject<CopyProjectResponse>(response)!;
}
```

If possible, the RPC methods should always return and use the appropriate models as arguments. This allows the regeneration of models to suffice when the model's fields or structure changes.
If the protocol allows it, the optional `isDryRun` argument should always be present.

#### Event Changes

Each event message is mapped to a public C# event. If the event message utilizes the `change_type` property, each **possible** change type should be mapped to its own event. 

```
// The SceneChanged events
public event EventHandler<BareSceneEventArgs>? OnSceneRemoved;
public event EventHandler<BareSceneEventArgs>? OnSceneBaseUpdated;
```

Every event message has to have a handler method, that will deserialize the JSON string and raise the corresponding event.
Invalid change types should be considered protocol violations and throw an exception.

```
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

// Another example
private void HandleRobotMoveToJoints(string data) {
    var robotMoveToJoints = JsonConvert.DeserializeObject<RobotMoveToJoints>(data)!;
    OnRobotMoveToJoints?.Invoke(this, new RobotMoveToJointsEventArgs(robotMoveToJoints.Data));
}
```

As you can see in the example above, each event should have corresponding (or also empty) `EventArgs`, which should ideally contain the event's data (`event.Data` is in our example case of the `BareScene` data type).
They should be defined in the `Arcor2EventArgs.cs` file and have the following structure.

```
public class BareSceneEventArgs : EventArgs {
    public BareScene Scene { get; set; }

    public BareSceneEventArgs(BareScene scene) {
        Scene = scene;
    }
}
```

If the `parentId` is set and relevant to the specific message type (for example on change type `Add` of messages `OrientationChanges`, where the parent ID corresponds to the parent action point), it should be implemented by inheriting from the `ParentIdEventArgs` instead.

Make sure to not forget to map the `event` string name to the handler in the `OnMessage` method.