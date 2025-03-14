using Arcor2.ClientSdk.Communication.Design;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Arcor2.ClientSdk.Communication.UnitTests.Mocks;
using Newtonsoft.Json;
using System.Collections;

namespace Arcor2.ClientSdk.Communication.UnitTests.Tests;

public class Arcor2ClientBasicRpcFunctionalityTests : TestBase {
    [Fact]
    public async Task RpcCall_Valid_Success() {
        // Send the request
        await Client.ConnectAsync(ValidUri);
        var args = new RegisterUserRequestArgs("John");

        var registerUserTask = Client.RegisterUserAsync(args);

        Assert.Single((IEnumerable) WebSocket.SentMessages);
        var request = JsonConvert.DeserializeObject<RegisterUserRequest>(WebSocket.SentMessages.First());
        Assert.NotNull(request);
        Assert.Equal("RegisterUser", request.Request);
        Assert.NotNull(request.Args);
        Assert.Equal("John", request.Args.UserName);

        // Send the response
        var response = JsonConvert.SerializeObject(new RegisterUserResponse(request.Id, "RegisterUser", true, []));
        WebSocket.ReceiveMockMessage(response);

        var result = await registerUserTask;

        Assert.NotNull(result);
        Assert.Equal("RegisterUser", result.Response);
        Assert.True(result.Result);
    }

    [Fact]
    public async Task RpcCall_ServerReject_Success() {
        // Send the request
        await Client.ConnectAsync(ValidUri);
        var args = new RegisterUserRequestArgs("John");

        var registerUserTask = Client.RegisterUserAsync(args);

        Assert.Single((IEnumerable) WebSocket.SentMessages);
        var request = JsonConvert.DeserializeObject<RegisterUserRequest>(WebSocket.SentMessages.First());
        Assert.NotNull(request);
        Assert.Equal("RegisterUser", request.Request);
        Assert.NotNull(request.Args);
        Assert.Equal("John", request.Args.UserName);

        // Send the response
        var response =
            JsonConvert.SerializeObject(new RegisterUserResponse(request.Id, "RegisterUser", false,
                ["Username already taken."]));
        WebSocket.ReceiveMockMessage(response);

        var result = await registerUserTask;

        Assert.NotNull(result);
        Assert.Equal("RegisterUser", result.Response);
        Assert.False(result.Result);
    }

    [Fact]
    public async Task RpcCall_NoResponse_ThrowsTimeout() {
        await Client.ConnectAsync(ValidUri);
        var args = new RegisterUserRequestArgs("John");

        await Assert.ThrowsAsync<Arcor2ConnectionException>(async () => await Client.RegisterUserAsync(args));
    }

    [Fact]
    public async Task RpcCall_MismatchedIdResponse_ThrowsTimeout() {
        await Client.ConnectAsync(ValidUri);
        var args = new RegisterUserRequestArgs("John");

        var registerUserTask = Client.RegisterUserAsync(args);

        var request = JsonConvert.DeserializeObject<RegisterUserRequest>(WebSocket.SentMessages.First())!;
        var response = JsonConvert.SerializeObject(new RegisterUserResponse(request.Id + 1, "RegisterUser", true, []));
        WebSocket.ReceiveMockMessage(response);

        await Assert.ThrowsAsync<Arcor2ConnectionException>(async () => await registerUserTask);
    }

    [Fact]
    public async Task RpcCall_MismatchedRpcResponseName_ThrowsTimeout() {
        await Client.ConnectAsync(ValidUri);
        var args = new RegisterUserRequestArgs("John");

        var registerUserTask = Client.RegisterUserAsync(args);

        var request = JsonConvert.DeserializeObject<RegisterUserRequest>(WebSocket.SentMessages.First())!;
        var response = JsonConvert.SerializeObject(new RegisterUserResponse(request.Id, "NotAValidRpcName", true, []));
        WebSocket.ReceiveMockMessage(response);

        await Assert.ThrowsAsync<Arcor2ConnectionException>(async () => await registerUserTask);
    }

    [Fact]
    public async Task RpcCall_MismatchedRpcResponseNameAllowed_Success() {
        var clientAllowedMismatch = new Arcor2Client(new MockWebSocket(),
            new Arcor2ClientSettings { RpcTimeout = 100, ValidateRpcResponseName = false });
        var websocketAllowedMismatch = (clientAllowedMismatch.GetUnderlyingWebSocket() as MockWebSocket)!;

        await clientAllowedMismatch.ConnectAsync(ValidUri);
        var args = new RegisterUserRequestArgs("John");

        var registerUserTask = clientAllowedMismatch.RegisterUserAsync(args);

        var request =
            JsonConvert.DeserializeObject<RegisterUserRequest>(websocketAllowedMismatch.SentMessages.First())!;
        var response = JsonConvert.SerializeObject(new RegisterUserResponse(request.Id, "NotAValidRpcName", true, []));
        websocketAllowedMismatch.ReceiveMockMessage(response);

        var exception = await Record.ExceptionAsync(async () => await registerUserTask);
        Assert.Null(exception);
    }

    [Fact]
    public async Task RpcCall_MalformedJsonResponse_ThrowsTimeout() {
        await Client.ConnectAsync(ValidUri);
        var args = new RegisterUserRequestArgs("John");

        var registerUserTask = Client.RegisterUserAsync(args);

        var request = JsonConvert.DeserializeObject<RegisterUserRequest>(WebSocket.SentMessages.First())!;
        var response = JsonConvert.SerializeObject(new RegisterUserResponse(request.Id, "RegisterUser", true, []));
        response = response[..3];

        WebSocket.ReceiveMockMessage(response);

        await Assert.ThrowsAsync<Arcor2ConnectionException>(async () => await registerUserTask);
        Assert.Equal(WebSocketState.Open, WebSocket.State);
        Assert.False(ConnectionClosedEventRaised);
    }

    [Fact]
    public async Task RpcCall_EmptyJsonResponse_ThrowsTimeout() {
        await Client.ConnectAsync(ValidUri);
        var args = new RegisterUserRequestArgs("John");

        var registerUserTask = Client.RegisterUserAsync(args);
        WebSocket.ReceiveMockMessage("{}");

        await Assert.ThrowsAsync<Arcor2ConnectionException>(async () => await registerUserTask);
        Assert.Equal(WebSocketState.Open, WebSocket.State);
        Assert.False(ConnectionClosedEventRaised);
    }

    [Fact]
    public async Task RpcCall_EmptyStringResponse_ThrowsTimeout() {
        await Client.ConnectAsync(ValidUri);
        var args = new RegisterUserRequestArgs("John");

        var registerUserTask = Client.RegisterUserAsync(args);
        WebSocket.ReceiveMockMessage("");

        await Assert.ThrowsAsync<Arcor2ConnectionException>(async () => await registerUserTask);
        Assert.Equal(WebSocketState.Open, WebSocket.State);
        Assert.False(ConnectionClosedEventRaised);
    }

    [Fact]
    public async Task RpcCall_ResponseOnlyId_ThrowsTimeout() {
        await Client.ConnectAsync(ValidUri);
        var args = new RegisterUserRequestArgs("John");

        var registerUserTask = Client.RegisterUserAsync(args);

        WebSocket.ReceiveMockMessage($"{{\"id\": {registerUserTask.Id}}}");

        await Assert.ThrowsAsync<Arcor2ConnectionException>(async () => await registerUserTask);
        Assert.Equal(WebSocketState.Open, WebSocket.State);
        Assert.False(ConnectionClosedEventRaised);
    }
}