# Client Services Library Integration Tests

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
The chosen approach reuses server instances across tests within the same class, with each test class receiving its own server instance.
However, care must be taken to ensure proper cleanup after each test, as leftover objects could interfere with subsequent tests and cascade issues.

Nevertheless, **the current configuration uses one server instance per one test due to server bugs**.
If the future state of the server allows it, 
the test setup can be easily modified to initialize server per test class by replacing direct server initialization in constructor for `IClassFixture<Arcor2ServerFixture>` within the test setup.

## What to Test

**The tests are meant to validate the client library, not the server.**

There is no need to test the server for obscure errors (e.g., recursive action point parents), as such tests do not contribute to the library's quality.
This is because the client library does not perform any client-side validation for RPC parameters; it only provides comments indicating known limitations on them.
The primary reason for this approach is that the server is in active development, and these constraints may change over time.
The addded maintanance time in the future easily outwieghts the little practical value which they add.

However, RPC parameter configurations that are and will always be unquestionably invalid (e.g., attempting to delete a non-existent scene) may be tested. 
In such cases keep in mind that the library still merely propagates all server errors without additional processing.

In the end, testing should focus on verifying that the library raises the expected events and correctly updates its internal state.

## What is Tested

Tests for majority of RPCs and events already exist.

There are no calibration-related RPC tests due to the lack of mock support.
A large number of overloads also remain untested. That is because they internally just invoke another overload with transformed arguments.

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

Note that RPC method invocations do not change the inner state of the library immediately, as it updates its state only when the server broadcasts an event. 
It is strongly recommended to use the existing `EventAwaiter` and `CollectionChangedAwaiter` classes to ensure events were raised before proceeding.

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