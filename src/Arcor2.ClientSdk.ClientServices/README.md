# ClientServices Library

The `Arcor2.ClientSdk.ClientServices` is an ARCOR2 client library designed to be a complete business logic solution. In contrast to `Arcor2.ClientSdk.Communication`, this library
fully holds the client state and offers a wide range of helper methods.

## Basic Architecture

The library is contrived as a tree of manager objects representing and managing the internal state of some resource (e.g., scene, action point, ...). 
The root element is the `Arcor2Session` class, which offers connection methods, general and utility RPCs, and maintains a collection of scenes, projects, object types, and packages.
Scene is then represented by the `SceneManager` class and offers scene-related RPCs, and maintains a collection of its action objects. A similar pattern continues until you get to the leaf elements.

Sessions can not be reused. A new `Arcor2Session` instance must be initialized for each session.
```
// Initialization of a session
var session = new Arcor2Session(new ConsoleLogger());
await session.ConnectAsync(new Uri("ws://localhost:6789");
await session.InitalizeAsync();
await session.RegisterAndSubscribeAsync("username");

Console.WriteLine("This server has {session.Scenes.Length} scenes!");
await session.CloseAsync();
```

Each manager object represents and holds some data (e.g., for a scene, it is the scene metadata) in its `Data` property. 
When that data is updated in any form, the manager object will always raise a notification in the form of an `PropertyChanged` event. This event is provided by the `INotifyPropertyChanged` and such are automatically supported by WPF and similar binding mechanisms.  Please note that this only applies to the data in the `Data` property.
Changes concerning the manager object's existence as a whole (removal and addition) are communicated by the `ReadOnlyObservableCollection` that holds them.
Each manager will also raise a `Removing` event shortly before deleting itself.

>  **NOTE -- Subscribing to collections**  
> To subscribe to the `CollectionChangedEvent` of `ReadOnlyObservableCollection`, you need to directly cast it into the `INotifyCollectionChanged` interface.

All managers inherit either from the `Arcor2ObjectManager` class or the extended `LockableArcor2ObjectManager` class. 
The `Arcor2ObjectManager` class provides the aforementioned notification mechanisms, while `LockableArcor2ObjectManager` adds
unique identification `Id` and locking capabilities, namely the `Locked` and `Unlocked` events.

```
var scene = session.Scenes.First();
scene.ActionObjects(actionObject => {
	object.Locked += actionObject.MakeUninteractable
	object.Unlocks += actionObject.MakeInteractable
});
```

The manager objects in `ReadOnlyObservableCollection` and their data in the `Data` property are always purely **read-only**. 
Changing them will not communicate the changes to the server and only results in an inconsistent state of the client.
All changes must be done using the offered RPC methods.

```
// This is wrong
sceneManager.Data.Name = "ExampleScene"

// This is right
await sceneManager.RenameAsync("ExampleScene");
```

The managers form the following hierarchy, similar to the ownership hierarchy in the ARCOR2 system:
```
Arcor2Session HOLDS SceneManager, ProjectManager, PackageManager, ObjectTypeManager
SceneManager HOLDS ActionObjectManager
ProjectManager HOLDS ActionPointManager, ProjectParameterManager, ProjectOverrideManager, LogicItemManager
ActionPointManager HOLDS ActionManager, JointsManager, OrientationManager
```

## RPCs
The session and manager objects offer a wide range of different RPCs. 
The library generally does not do client-side checks on the parameters 
but offers a commentary about the requirements in the form of XAML comments.

Each RPC can thus fail with two exceptions:
- `Arcor2ConnectionException` - When any connection-related errors occur, such as a response timeout, connection dropping, or a protocol violation. This should be handled high in the call stack.
- `Arcor2Exception` - When the server denies the request. This represents a wide range of errors. The ARCOR2 server currently only offers human-readable error messages, but no concrete error codes. 

Also, note that any changes by the RPCs take a while to be reflected. The server needs to accept and process the request and send an event to each client (see `Updated` event).

## Locking
A portion of RPCs require the client to lock a set of resources and objects first. The library automatically manages these locks, except during the object aiming process, 
where users must manually acquire and release them themselves. 
This is because a lock has to be held across multiple object aiming process RPC exchanges, and to allow the client code to perform retries without unlocking. 
Locks are held only as long as necessary — typically for a single operation — and are released if an operation fails.

If you wish to manage locks manually, you can disable automatic locking by setting `LockingMode` to `LockingMode.NoLocks` in `Arcor2SessionSettings` when initializing the session.
This is not recommended unless you are familiar with the ARCOR2 API, as the locking requirements can vary with each RPC. 
 Alternatively, you can use the `PauseAutoLock` flag to temporarily disable automatic locking per object.

With automatic locking disabled, you can use the `LockAsync`, `UnlockAsync`, and `TryUnlockAsync` methods on any lockable manager. 
The `TryUnlockAsync` method works similarly to the `UnlockAsync` method but does not throw exceptions on failure.
Its usage is recommended to prevent locks from being held indefinitely in case of failures.

```
// No locks mode
var session = new Arcor2Session(settings: new Arcor2SessionSettings {
    LockingMode = LockingMode.NoLocks
});
...
await objectManager.LockAsync();
await objectManager.UpdateObjectModel(newBoxModel);
await objectManager.UnlockAsync();

// Temporarily disable auto-locking
objectManager.PauseAutoLock = true;
await objectManager.LockAsync();
await objectManager.UpdateObjectModel(newBoxModel)
await objectManager.UnlockAsync();
objectManager.PauseAutoLock = false;
```

## Practical Usage
>  **NOTE - Existing example**  
> See `Arcor2.ClientSdk.ClientServices.ConsoleTestApp` project for trivial implementation of a CLI client.

Developing a client application using this package largely consists of creating a presentation layer and correctly connecting it with the library.

The following section describes the package's API in the typical development order.
Individual RPCs or events won't be discussed, as almost all members are fully documented using XML comments.
To learn more about different members, use IntelliSense or generate documentation using a documentation generator.

A basic knowledge of the ARCOR2 system is assumed.
### Initialization

The `Arcor2Session` class represents the connection itself. It is a single-use object and a new one must be created for each connection.

To ease debugging, it is recommended to inject an instance of `IArcor2Logger` into the `Arcor2Session` on initialization.
No implementation of the interface is provided by the package, but creating your own is straightforward. You need to implement
three methods: `LogInfo`, `LogWarn`, and `LogError`. The first captures general information, most notably all sent and received messages and connection events.
The `LogWarn` method captures fully recoverable errors, such as unexpected internal states or messages. `LogError` indicates issues that lead to failure of operations.

The connection can be opened and closed using the `ConnectAsync` and `CloseAsync` methods.  
Upon a successful connection, a long-running `Task` is spawned to listen for messages from the server.  
Since this listening process runs within a `Task` 
(and may execute on a non-main thread), modifying the UI in most frameworks (Unity, WPF, WinForms, etc.) is inherently unsafe, requiring a suitable synchronization method. 

You may inject synchronization action using `Arcor2SessionSettings`. This action will be executed as soon as message arrives with argument action being the rest of the pipeline
This is useful as most frameworks offer global dispatcher objects which can easily make the whole library safe to use within UI context.
```
Session = new Arcor2Session(
	new Arcor2SessionSettings {
		SynchronizationAction = @continue => {
			Application.Current.Dispatcher.BeginInvoke(
				DispatcherPriority.Background,
				@continue);
		}
	});
```
Closing the connection automatically releases all resources associated with the session.  
The `Dispose` method functions identically to `CloseAsync`, but is idempotent, thus ensuring it does not throw exceptions when called multiple times or in an invalid state.  

A `ConnectionOpened` event is raised upon a successful connection, and `ConnectionClosed` whenever the connection is closed.
The client is tolerant towards non-graceful disconnections from the server but marks them with a `ProtocolViolation` closing code.
A `ConnectionException` event is also available and is raised whenever a connection-related error occurs. Its arguments contain the exception and it may or may not be followed by the `ConnectionClosed` event. 
The current state of the connection is listed in the `ConnectionState` property (with added `Initialized` and `Registered` states).



```
var session = new Arcor2Session(
	new Arcor2SessionSettings(),
	new ConsoleLogger()
);

session.ConnectionOpened += (_, _) => Console.WriteLine("Open!");
session.ConnectionClosed += (_, _) => Console.WriteLine("Closed!");
session.ConnectionException += (_, args) => Console.WriteLine("Error: {args.Exception}");

await session.ConnectAsync("localhost", "6789");
var info = await session.InitializeAsync();
await session.RegisterAndSubscribeAsync("user");
// Do something...
await session.CloseAsync();
```

Right after connecting, there already may be an open scene, project, or package (from now on collectively referred to as a *workplace*).
Interacting with the workplace at that point is unsafe. You must first invoke the `InitializeAsync` method to load the required information, specifically:
- Object types and their actions
- Metadata of all scenes, projects, and packages
- Server information (such as the API and server version)

>  **NOTE - Maximum efficiency**  
> During initialization, you may set `LoadData` to `false`, which skips loading the required information.
> Methods prefixed with `Reload` can then be used to load this data later (for normal usage, these methods can be safely ignored). 
> This can be useful for specific use-case applications to maximize I/O efficiency.
> Note that doing this invalidates all `null` type hints of the library.

If your client application is not purely read-only and will modify the data, 
you will also have to invoke the `RegisterAndSubscribeAsync` method to register a user.
This will allow you to acquire object locks and allow you to invoke all RPC methods.
This method will also register you for robot joint and end effector pose updates if a workplace is open.

>  **NOTE - Locked robots and event updates**  
> If a robot is locked during registration, the library will be unable to subscribe to its joints and end effector pose events.
> It will schedule an automatic resubscription after its unlock. 

### Navigation and Session Members
Apart from object types, the `Arcor2Session` class maintains a collection of scenes, projects, and packages.  
The session object may request you to open or close a workspace or a view by raising the `NavigationStateChanged` event.  
The session class properties (or the arguments of the event) provide the type of the open view in the `NavigationState` property and, if applicable, the related object ID in the `NavigationId` property.  
For example, if a scene is open, `NavigationState` will be set to `Scene`, and `NavigationId` will contain the scene ID.

You may notice that the menu is not a single state but is divided into three: `MenuListOfScenes`, `MenuListOfProjects`, and `MenuListOfPackages`.  
This distinction originates from the UI design of the AREditor client, which influenced the architecture of the server.  
However, you can ignore this distinction, as the client behaves identically in all these states regarding allowed RPCs and other functionality.  
If `NavigationId` is set in a menu state, it represents the highlighted item (e.g., the closed scene).  

The `SceneClosed` and `ProjectClosed` states indicate that the server requested the workspace to close but has not yet requested to show the menu.  
These states can practically be either ignored or used to show a loading screen.

```
session.NavigationStateChanged += (_, args) => {
	switch(args.State) {
		case NavigationState.Scene:
			ShowScene(args.Id);
			break;
		case NavigationState.Project:
			ShowProject(args.Id);
			break;
		case NavigationState.Package:
			ShowPackage(args.Id);
			break;
		cases NavigationState.MenuListOfScenes:
		cases NavigationState.MenuListOfProjects:
		cases NavigationState.MenuListOfPackages:
			ShowMenu();
			break;
		default:
			// Ignore scene and project closed events
			break;
	}
}
```

Additionally, the session also provides a range of RPCs.  
These primarily include utility RPCs that do not belong anywhere else.  
Notably, the available RPCs provide the creation of workplaces and object types, upload of packages, and estimatión of a pose or a position of camera and marker corners from images.

>  **NOTE - Images**  
> The library-wide format of images is currently JPEG.

### Object Types
The session manages all available object types through the `ObjectTypeManager` class, along with their associated actions.
Robot metadata is automatically loaded if the object type inherits from the `Robot` class.

In addition to the standard CRUD RPCs, the library provides helper methods for easier type handling. The `Parent` property stores a reference to the direct ancestor, reaching `null` at the most generic base type, `Generic`. The `IsTypeOf` and `IsSubtypeOf` methods checks whether an object type is type of or derived from another type.
Convenience methods for built-in types are also available (`IsRobot`, `IsCamera`, etc.).

Each object maintains a reference to the required scene action object parent type in the `SceneParent` property.

### Scenes and Action Objects
The session initially loads only the metadata of all scenes into the `SceneManager` class.  
A scene is fully loaded only when opened. Alternatively, calling the `GetAsync` method loads the scene without opening it.
This can be useful for implementing advanced previews within the client's menu.
You can use the `IsOpen` method to check whether a scene is currently open. This is identical to checking the navigation properties on the session object.

```
foreach(var scene in Session.Scenes) {
	await scene.GetAsync();
}

var byActionObjects = Session.Scenes.SortBy(s => s.ActionObjects.Count());
```

The scene raises the `OnlineStateChanged` event whenever its online state changes. Its last known state, including an optional error message, is then stored in the `State` property.  
Keep in mind that this event is only available on the `SceneManager` object. To know the online scene of a project, you must look at its parent scene!
The scene also offers a `Saved` event.

Each scene manages a collection of action objects through the `ActionObjectManager` class.  
An action object represents an instance of an object type, with its type referenced in the `ObjectType` property.

If the object is a robot, the library automatically loads its available arms and end effectors. 
The library also handles automatic updates of joint values and end effector pose and continuously updates them when possible.

Working with action objects requires careful consideration, as they offer a wide set of RPCs, each with specific constraints and requirements.  
These include general action object RPCs, as well as specialized RPCs and events for cameras and robots. 
Always verify the action object's capabilities (such as the object type, robot metadata, and available end effectors and arms) before invoking any RPC.
The requirements for invoking different RPCs are thoroughly documented via XML comments. 

```
if(actionObject.ObjectType.IsRobot()){
	if(actionObject.ObjectType.Data.RobotMeta.MoveToPose){
		CreateMovementSliders();
	}
}
```

The package also provides extension methods for working with object type parameters.  
A method for creating parameter instances from their definitions (`ParameterMeta`) has been added: `ToParameter`.  
This method uses the parameter's default value if none is specified. The provided value must be correctly formatted.

> **NOTE – Parameter Values**  
> The ARCOR2 system serializes all parameter values to a string, regardless of type.  
> This creates a need to differentiate between a string representation of a numeric value and a string just containing a number.
> Thus, all string values must be enclosed within double quotes. An `InvariantCulture` formatting is recommended for numeric types.

A `GetValidator()` method is available on parameter metadata, returning an instance of `ParameterValidator`.  
This class validates a parameter value based on the constraints defined in the `ParameterMeta.Extra` property.  
If no constraints exist, `null` is returned.

The returned validator is an abstract base type. If additional details are needed,  
it can be cast to either `RangeParameterValidator` or `ValuesParameterValidator`, depending on the `Type` property.  
These concrete validators expose extra members, such as the allowed values or range itself,  
or methods with more parameters.

```
private void Button_SubmitParameter(ParameterMeta meta, string input){
	// String representation of the string type in ARCOR2
	var value = "\"{input}\"";
	var validator = meta.GetValidator();
	if(validator?.Validate(value) ?? true){
		var parameter = meta.ToParameter(value);
		await actionObject.AddParameter(parameter);
	}

}
```

### Projects
Projects are managed by the `ProjectManager` class. Like scenes, only the project's metadata is initially loaded.

In a typical client application, you will also interact with the parent scene and its action objects. This often involves composing action objects, action points, and other project elements into a unified virtual workspace.  
The parent scene is referenced through the `Scene` property.  

A project holds collections of these objects:  
- **Logic items** (`LogicItemManager`): Define connections between actions, optionally with conditions. The special constants `START` and `END` can be used to mark the beginning or end of an action sequence.  
- **Action points** (`ActionPointManager`): Represents named spatial points.  
- **Project parameters** (`ProjectParameterManager`): Hold the values of the different project parameters (e.g., the parent scene ID).   
- **Action object parameter overrides** (`ProjectOverrideManager`): Allow modifying parameters of action objects of the parent scene in a single project.  

Action points hold a collection of actions (`ActionManager`), robot orientations (`OrientationManagers`), and robot joints (`JointsManager`).
The `TryGetParentActionObject` and `TryGetParentActionPoint` methods can be used to obtain an instance of the action point parent.

### Action points

Actions can be executed either within a project or as part of a package.  

When executing an action within a project using the `ExecuteAsync` and `CancelAsync` methods, the library notifies different execution stages through the `Executing`, `Executed`, and `Cancelled` events.  

If an action runs automatically as part of a package, the `Starting` and `Finished` events are used to track execution, retrieve parameters, and access results.

### Packages

Packages are represented by the `PackageManager` class.

Whenever the running state of the package changes, the `StateChanged` event is raised. The last known state is stored in the `State` property and is, by default, `Undefined`.
A `ExceptionOccured` event is raised on package exception (such as invalid breakpoint ID).

## Accounting for Missing Features

If, in the future, the package is missing new RPCs, events, or features you need, you can use the `Arcor2Session.GetUnderlyingClient` method to access the underlying `Arcor2Client`. 
This client provides a more lightweight interface for library communication and will likely have a more up-to-date set of features compared to this package.
However, when using this feature, you are responsible for managing the data returned by the client yourself.
For even more low-level access, you can use the `Arcor2Client.GetUnderlyingWebSocket` method to directly access the used `IWebSocket`.
See the README of the `Arcor2.ClientSdk.Communication` project for more information.


## Updating the Library for Newer ARCOR2 Server Versions

Updating the library due to additions, renames, and removals of nonintegral JSON properties within RPC or event messages should be trivial.
Often a simple regeneration of models from a new OpenAPI specification within the `Arcor2.ClientSdk.Communication.OpenApi` project should be enough.
If the library does not directly expose the data model to the client code, but rather maps it to a new more user-friendly data model, you may need to update its properties too.

The addition of new RPCs and events is often also rather simple. 
Declare them and in the case of events don't forget to properly register them and unregister them. 

Updating existing RPCs and events can be tricky, as a degree of compatibility should be implemented if possible.
It is recommended to add a new server version to the `Arcor2ServerVersion` enum and, if needed, update the parsing logic. 
You may then create an alternative branch for the older server versions (`Session.Settings.ServerVersion`) within the RPC method or event body.
Removal of RPCs and Events should be reflected by adding an `Obsolete` attribute and stating the version of removal.
You may need to move some generated data models from the `Arcor2.ClientSdk.Communication.OpenApi` project `Models` folder to another folder (e.g., `LegacyModels`) so they are not removed on model regeneration, and also make similar updates to the underlying `Communication` library.

```
// This is an example fictional scenario showcasing two approaches for ARCOR2 compatibility solutions:

// ----- Deprecation of an old RPC -----
[Obsolete("This RPC has been deprecated in ARCOR2 server version 1.4.0. Use GetSceneInformation for newer versions. ")]
public async Task<List<ActionObject>> GetSceneActionObjects() { 
	#pragma warning disable 0618
	return await client.GetSceneActionObjects();
	#pragma warning restore 0618
}

public async Task<Scene> GetSceneInformation() {
	return await client.GetSceneInformation();
}

// ----- Alternative branch that simulates functionality for older server versions -----
public async Task<Scene> GetSceneInformation() { 
	if(Session.Settings.ServerVersion == 1.3.1) {
		var ao = await client.GetSceneActionObjects();
		var meta = await client.GetSceneMeta();
		return SceneExtensions.CreateSceneFromMetaAndActionObjects(meta, ao);
	}
	else {
		return await client.GetSceneInformation();
	}
}
```