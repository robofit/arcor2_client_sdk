# ClientServices Library

The `Arcor2.ClientSdk.ClientServices` is an ARCOR2 client library designed to be a complete business logic solution. In contrast to `Arcor2.ClientSdk.Communication`, this library
fully holds the client state and offers wide range of helper methods.

## Basic Architecture

The library is contrived as a tree of manager objects representing and managing some resource. 
The root element is the `Arcor2Session` class, which offers connection methods, general and utility RPCs, and also maintains collection of scenes, projects, object types, and packages.
Scene is then represented by `SceneManager` class and offers scene-related RPCs, and maintains collection of its action objects. A similar pattern continues until you get to the leaf elements.

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

Each manager objects represents and hold some data (e.g., for scene, it is the scene metadata) in its `Data` property. 
When that data is updated in any form, the  manager object will always raise a notification in the form of `Updated` event. Please not that this only applies to the data in `Data` property.
Changes of children manager objects (e.g. `ActionObjectManager` for a `SceneManager`) are communicated by themselves (updates), or by the `ObservableCollection` that holds them (additions and deletions).
Each manager will raise `Removing` event shortly before deleting itself.

All managers inherit either from the `Arcor2ObjectManager` class or the extended `LockableArcor2ObjectManager` class. 
The `Arcor2ObjectManager` class provides the aforementioned notification mechanisms, while `LockableArcor2ObjectManager` adds
unique identification `Id` and locking capabilities, namely the `Locked` and `Unlocked` events.

```
var scene = session.Scenes.First();
scene.ActionObjects(object => {
	object.Locked += MakeUninteractable
});
```

The manager objects in `ObservableCollection` and the data in the `Data` property are always purely **read-only**. 
Changing them will not communicate the changes to the server and only results in inconsistent state of the client.
All changes must be done using the offered RPC methods.

```
// This is wrong
sceneManager.Data.Name = "ExampleScene"

// This is right
await sceneManager.RenameAsync("ExampleScene");
```

The managers form a following hierarchy:
```
Arcor2Session MANAGES SceneManager, ProjectManager, PackageManager, ObjectTypeManager
SceneManager MANAGES ActionObjectManager
ProjectManager MANAGES ActionPointManager, ProjectParameterManager, ProjectOverrideManager, LogicItemManager
ActionPointManager MANAGES ActionManager, JointsManager, OrientationManager
```

## RPCs
The session object and managers offer a wide range of different RPCs. 
The library generally does not do client-side check the parameters, but offers a commentary about the requirements in a form of XAML comments.

Each RPC can thus fail with two exceptions:
- `Arcor2ConnectionException` - When connection-related error occurs. This should be handled high in the call-stack.
- `Arcor2Exception` - When server denies the request. This represents a wide range of errors. The ARCOR2 server currently only offers human-readable error messages, but no concrete error codes. 

Note that any changes by the RPCs take while to be reflected. The server needs to accept the request and sent an event to each client (see `Updated` event).

## Locking

The library automatically manages locks, except during the object aiming process, where users must manually acquire and release the lock.
Locks are held for the shortest time necessary—often just for the duration of an operation—and are released immediately if an operation fails.

If you need to manage locks manually, you can disable automatic locking by specifying `LockingMode` to `LockingMode.NoLocks` in the `Arcor2SessionSettings` when initializing the session.

```
var session = new Arcor2Session(settings: new Arcor2SessionSettings {
    LockingMode = LockingMode.NoLocks
});

//...

await objectManager.LockAsync();
await objectManager.UpdateObjectModel(newBoxModel)
await objectManager.UnlockAsync();
```


## Examples

The library usage is practically showcased in included `Arcor2.ClientSdk.ClientServices.ConsoleTestApp` and `Arcor2.ClientSdk.ClientServices.IntegrationTests` projects