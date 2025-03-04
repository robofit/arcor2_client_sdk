using Arcor2.ClientSdk.Communication.Design;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.Communication.UnitTests.Tests;

public class Arcor2ClientBasicEventFunctionalityTests : TestBase
{
    private ObjectsLocked sampleObjectsLockedEvent => new("ObjectsLocked", null, null!,
        new LockData([
            "objectId1",
            "objectId2"
        ], "John")
    );

    private SceneChanged sampleSceneRemovedEvent => new("SceneChanged", SceneChanged.ChangeTypeEnum.Remove, null!,
        new BareScene("SampleScene", "Description", DateTime.Parse("2020-01-01"), DateTime.Parse("2020-02-01"), DateTime.Parse("2020-02-01"), "sceneId1")
    );

    [Fact]
    public async Task Event_Valid_Success()
    {
        await Client.ConnectAsync(ValidUri);
        ObjectsLockEventArgs? raisedEventArgs = null;

        Client.ObjectsLocked += (_, args) => { raisedEventArgs = args; };

        WebSocket.ReceiveMockMessage(sampleObjectsLockedEvent.ToJson());

        Assert.NotNull(raisedEventArgs);
        Assert.Equal(sampleObjectsLockedEvent.Data.Owner, raisedEventArgs.Data.Owner);
        Assert.Equal(sampleObjectsLockedEvent.Data.ObjectIds, raisedEventArgs.Data.ObjectIds);
    }

    [Fact]
    public async Task ChangedEvent_Valid_Success()
    {
        await Client.ConnectAsync(ValidUri);
        BareSceneEventArgs? raisedEventArgs = null;

        Client.SceneRemoved += (_, args) => { raisedEventArgs = args; };

        WebSocket.ReceiveMockMessage(sampleSceneRemovedEvent.ToJson());

        Assert.NotNull(raisedEventArgs);
        Assert.Equal(sampleSceneRemovedEvent.Data.Name, raisedEventArgs.Data.Name);
        Assert.Equal(sampleSceneRemovedEvent.Data.Description, raisedEventArgs.Data.Description);
        Assert.Equal(sampleSceneRemovedEvent.Data.Created, raisedEventArgs.Data.Created);
        Assert.Equal(sampleSceneRemovedEvent.Data.Modified, raisedEventArgs.Data.Modified);
        Assert.Equal(sampleSceneRemovedEvent.Data.IntModified, raisedEventArgs.Data.IntModified);
        Assert.Equal(sampleSceneRemovedEvent.Data.Id, raisedEventArgs.Data.Id);
    }

    [Fact]
    public async Task Event_WrongEventName_Ignored()
    {
        await Client.ConnectAsync(ValidUri);
        ObjectsLockEventArgs? raisedEventArgs = null;

        Client.ObjectsLocked += (_, args) => { raisedEventArgs = args; };

        var sampleEventWrongName = sampleObjectsLockedEvent;
        sampleEventWrongName.Event = "WrongEventName";

        WebSocket.ReceiveMockMessage(sampleEventWrongName.ToJson());

        Assert.Null(raisedEventArgs);
    }

    [Fact]
    public async Task ChangedEvent_UnsupportedChangeType_Ignored()
    {
        await Client.ConnectAsync(ValidUri);

        var sampleEventWrongChangeType = sampleSceneRemovedEvent;
        sampleEventWrongChangeType.ChangeType = SceneChanged.ChangeTypeEnum.Add;

        var exception = Record.Exception(() => WebSocket.ReceiveMockMessage(sampleEventWrongChangeType.ToJson()));
        Assert.Null(exception);
        Assert.False(ConnectionClosedEventRaised);
        Assert.Equal(WebSocketState.Open, Client.ConnectionState);
    }

    [Fact]
    public async Task Event_EmptyJson_Ignored()
    {
        await Client.ConnectAsync(ValidUri);

        var exception = Record.Exception(() => WebSocket.ReceiveMockMessage("{}"));
        Assert.Null(exception);
        Assert.False(ConnectionClosedEventRaised);
        Assert.Equal(WebSocketState.Open, Client.ConnectionState);
    }

    [Fact]
    public async Task Event_EmptyString_Ignored()
    {
        await Client.ConnectAsync(ValidUri);

        var exception = Record.Exception(() => WebSocket.ReceiveMockMessage(""));
        Assert.Null(exception);
        Assert.False(ConnectionClosedEventRaised);
        Assert.Equal(WebSocketState.Open, Client.ConnectionState);
    }

    [Fact]
    public async Task Event_MalformedJson_Ignored()
    {
        await Client.ConnectAsync(ValidUri);

        var exception = Record.Exception(() => WebSocket.ReceiveMockMessage("{[\"hi\""));
        Assert.Null(exception);
        Assert.False(ConnectionClosedEventRaised);
        Assert.Equal(WebSocketState.Open, Client.ConnectionState);
    }
}