# Communication Library Intergration Tests

The `Arcor2.ClientSdk.ClientServices.IntegrationTests` project contains integration tests for the `Arcor2.ClientSdk.ClientServices` library, implemented using xUnit.

The tests utilize the ARCOR2 server from the FIT demo (version 1.3.1), which includes Dobot robots.

## Requirements
- Docker
	- Ensure that your Docker daemon is configured with sufficient address pools for `N` networks, each requiring around 30 IP addresses. `N` is the number of test classes present in this project.
	- Make sure the Docker deamon is up and running.

## Server
The ARCOR2 server Docker containers used for testing are automatically created and disposed using the `Testcontainers` library. 
The original `docker-compose.yml` file from the demo was faithfully adapted to work with the `Testcontainers` fluent definition process. 
To support parallel test execution, both container names and ports are randomized.

A major issue is balancing test isolation with the resource overhead of recreating the server for each test (which takes around a minute on a moderately powered machine). 
The current approach reuses server instances across tests within the same class, with each test class receiving its own server instance.
However, care must be taken to ensure proper cleanup after each test, as leftover objects could interfere with subsequent tests and cascade issue.

If issues arise with this configuration in a future, 
the test setup can be easily modified to initialize per test by replacing IClassFixture<Arcor2ServerFixture> with direct initialization within the test setup.

## Testing Format

Most test use the following format.

```
public async Task UnitOrUseCases_ParametersOrState_ExpectedBehavior() {
	await SetupAsync();

	// Arrange the parameters and state
	...

	try {
		// Run the testee sequence
		...

		// Assert the state and outputs
		...
	}
	finally {
		// Cleanup to initial state
		...
		await TeardownAsync();
	}
}
```

RPC method invocations do not change state immediately, as the client updates its state only when the server broadcasts an event. 
It is strongly recommended to use the `EventAwaiter` and `CollectionChangedAwaiter` classes to ensure the event was raised before proceeding.

```
// Create and register the awaiter before action
var removeAwaiter = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove).WaitForEventAsync();

// Act
var record = await Session.Scenes.First().CloseAsync(true);

// Await the event
await removeAwaiter;

// Assert
Assert.Empty(Session.Scenes);
```

```
// Create and register the awaiter before action
var navigationEventAwaiter = new EventAwaiter<NavigationStateEventArgs>();
Session.NavigationStateChanged += navigationState.EventHandler;
var navigationAwaiter = navigationEventAwaiter.WaitForEventAsync();

// Act
await Session.CreateSceneAsync("name");

// Await the event
await navigationAwaiter;

// Assert
Assert.Equal(NavigationState.Scene, Session.NavigationState)
```