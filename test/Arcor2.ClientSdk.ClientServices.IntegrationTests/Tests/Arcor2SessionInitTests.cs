using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System.Collections.Specialized;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Tests;

public class Arcor2SessionInitTests(Arcor2ServerFixture fixture, ITestOutputHelper output) : TestBase(fixture, output) {
    [Fact]
    public async Task ConnectionSequence_Valid_CorrectState() {
        var openedAwaiterEvent = new EventAwaiter();
        var closedAwaiterEvent = new EventAwaiter();
        Session.ConnectionOpened += openedAwaiterEvent.EventHandler;
        Session.ConnectionClosed += closedAwaiterEvent.EventHandler;
        var openedAwaiter = openedAwaiterEvent.WaitForEventAsync();
        var closedAwaiter = closedAwaiterEvent.WaitForEventAsync();
        var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();

        Assert.Equal(Arcor2SessionState.None, Session.ConnectionState);

        await Session.ConnectAsync(Uri);
        await openedAwaiter;
        Assert.Equal(Arcor2SessionState.Open, Session.ConnectionState);
        await navigationAwaiter;
        Assert.Equal(NavigationState.MenuListOfScenes, Session.NavigationState);

        await Session.CloseAsync();
        await closedAwaiter;
        Assert.Equal(Arcor2SessionState.Closed, Session.ConnectionState);
    }

    [Fact]
    public async Task ConnectionSequence_TwoConnects_Throws() {
        await Session.ConnectAsync(Uri);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Session.ConnectAsync(Uri));
        await Session.CloseAsync();
    }

    [Fact]
    public async Task Close_BeforeOpened_Throws() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => Session.CloseAsync());

    [Fact]
    public async Task Close_AfterClose_Throws() {
        await Session.ConnectAsync(Uri);
        await Session.CloseAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => Session.CloseAsync());
    }

    [Fact]
    public async Task Rpc_BeforeOpened_Throws() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => Session.CreateSceneAsync("exception"));

    [Fact]
    public async Task ConnectionSequence_TwoCloses_Throws() {
        await Session.ConnectAsync(Uri);
        await Session.CloseAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => Session.CloseAsync());
    }

    [Fact]
    public async Task ConnectionSequence_TwoDisposes_Idempotent() {
        await Session.ConnectAsync(Uri);
        Session.Dispose();
        var ex = Record.Exception(Session.Dispose);
        Assert.Null(ex);
    }

    [Fact]
    public async Task ConnectionSequence_CloseAndDispose_Idempotent() {
        await Session.ConnectAsync(Uri);
        await Session.CloseAsync();
        var ex = Record.Exception(Session.Dispose);
        Assert.Null(ex);
    }

    [Fact]
    public async Task InitializationSequence_Valid_CorrectState() {
        await Session.ConnectAsync(Uri);
        Assert.Equal(Arcor2SessionState.Open, Session.ConnectionState);
        await Session.InitializeAsync();
        Assert.Equal(Arcor2SessionState.Initialized, Session.ConnectionState);
        Assert.NotEmpty(Session.ObjectTypes);

        await Session.RegisterAndSubscribeAsync("user");
        Assert.Equal(Arcor2SessionState.Registered, Session.ConnectionState);
        Assert.Equal("user", Session.Username);
        Assert.Equal(Arcor2SessionState.Registered, Session.ConnectionState);
        Assert.Equal(NavigationState.MenuListOfScenes, Session.NavigationState);
        Assert.NotEmpty(Session.ObjectTypes);
        Assert.Empty(Session.Scenes);
        Assert.Empty(Session.Projects);
        Assert.Empty(Session.Packages);

        await Session.CloseAsync();
        Assert.Equal(Arcor2SessionState.Closed, Session.ConnectionState);
    }

    [Fact]
    public async Task InitializationSequenceTwoInstances_Valid_CorrectState() {
        var session2 = new Arcor2Session();
        await Session.ConnectAsync(Uri);
        await session2.ConnectAsync(Uri);
        await Session.InitializeAsync();
        await session2.InitializeAsync();

        Assert.NotEmpty(Session.ObjectTypes);
        Assert.NotEmpty(session2.ObjectTypes);

        await Session.RegisterAndSubscribeAsync("user");
        await session2.RegisterAndSubscribeAsync("user2");
        Assert.Equal("user", Session.Username);
        Assert.Equal(Arcor2SessionState.Registered, Session.ConnectionState);
        Assert.Equal(NavigationState.MenuListOfScenes, Session.NavigationState);
        Assert.NotEmpty(Session.ObjectTypes);
        Assert.Empty(Session.Scenes);
        Assert.Empty(Session.Projects);
        Assert.Empty(Session.Packages);

        await Session.CloseAsync();
        await session2.CloseAsync();
    }

    [Fact]
    public async Task InitializationSequenceTwoInstances_SameName_Throws() {
        var session2 = new Arcor2Session();
        await Session.ConnectAsync(Uri);
        await session2.ConnectAsync(Uri);
        await Session.InitializeAsync();
        await session2.InitializeAsync();

        Assert.NotEmpty(Session.ObjectTypes);
        Assert.NotEmpty(session2.ObjectTypes);

        await Session.RegisterAndSubscribeAsync("user");
        var ex = await Record.ExceptionAsync(async () => await session2.RegisterAndSubscribeAsync("user"));

        await Session.CloseAsync();
        await session2.CloseAsync();
    }

    [Fact]
    public async Task UnderlyingClient_ValidRpc_Success() {
        await Session.ConnectAsync(Uri);
        var client = Session.GetUnderlyingArcor2Client();
        var scenes = await client.ListScenesAsync();
        Assert.NotNull(scenes);
        await Session.CloseAsync();
    }

    [Fact]
    public async Task UnderlyingClient_ValidRpcWithEvent_StateChanges() {
        await Session.ConnectAsync(Uri);
        var client = Session.GetUnderlyingArcor2Client();
        try {
            var openAwaiterEvent = new EventAwaiter<OpenSceneEventArgs>();
            client.SceneOpened += openAwaiterEvent.EventHandler;
            var openAwaiter = openAwaiterEvent.WaitForEventAsync();

            await client.AddNewSceneAsync(new NewSceneRequestArgs(RandomName()));

            await openAwaiter;
            Assert.Equal(NavigationState.Scene, Session.NavigationState);
            Assert.Single(Session.Scenes);
        }
        finally {
            var remove = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await Session.Scenes.First().RemoveAsync();
            await remove;
            await Session.CloseAsync();
        }
    }
}