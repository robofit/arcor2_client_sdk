# Communication Library

The `Arcor2.ClientSdk.Communication` is a stand-alone communication library designed for ARCOR2 clients.

## Introduction

The ARCOR2 server implements an event-driven API by utilizing WebSockets for real-time bidirectional communication between clients and the server. 
The `Arcor2.ClientSdk.Communication` library provides a strongly-typed client interface for interaction with ARCOR2 API. 

The library exposes RPCs (request-response exchanges) as Task-based asynchronous methods, streamlining exception handling and integration with .NET's modern TPL library. Events are handled through .NET's event system.

## Basic Usage

To start using the library, you can initialize `Arcor2Client`. Here's how you can do that:

```csharp
var client = new Arcor2Client();
```
The non-generic `Arcor2Client` internally uses .NET's [ClientWebSocket](https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.clientwebsocket?view=net-9.0). 
If your target platform does not support this implementation, you have to option to provide your own implementation derived from the `IWebSocket` interface (see its XAML docs for specific behavior requirements).

```csharp
// Custom WebSocket implementation
var client = new Arcor2Client<WebGlWebSocket>();
```

The user should now subscribe handles to specific events.

```csharp
client.ConnectionError += LogException;
client.ConnectionClosed += Quit;
client.ConnectionOpened += ChangeView;

client.ProjectParameterAdded += SomeOtherHandler
// And so on...
```

Client can now connect to the server and start sending requests.

```csharp
await client.ConnectAsync(serverUri);
await client.SystemInfoAsync();
//...
await client.CloseAsync();
```

## Contributing

### Implementing API Changes

The library is built upon the models generated from OpenApi specification of the ARCOR2 server (current version 1.2.0). 
Addition, deletion or update of any properties from these models should only require a regeneration of the `Arcor2.ClientSdk.Communication.OpenApi` project. 
A shell script for generating these models using OpenApi CodeGen is provided.

Modification of RPC endpoints also requires the corresponding change to methods (or in case of Event, a corresponding C# event). Please see existing methods in `Arcor2Client.cs` for the exact structure of method or event. Snippets for creating large batches of methods in Visual Studio are included in `arcor2.snippet` file.