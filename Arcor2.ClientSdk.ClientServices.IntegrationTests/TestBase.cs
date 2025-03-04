using System.Collections.Specialized;
using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests;

public class TestBase(ITestOutputHelper output) : IAsyncLifetime {
    private Arcor2ServerFixture server;
    protected Arcor2Session Session { get; private set; } = null!;
    protected IArcor2Logger Logger { get; private set; }
    protected Uri Uri => server.Uri;
    protected string Host => server.Host;
    protected ushort Port => server.Port;

    protected string RandomName() => Guid.NewGuid().ToString();
    public async Task InitializeAsync() {
        Logger = new TestLogger(output);
        Session = new Arcor2Session(logger: Logger);
        server = new Arcor2ServerFixture(); 
        await server.InitializeAsync();
        await Task.CompletedTask;
    }

    protected virtual async Task Setup() {
        await Session.ConnectAsync(Uri);
        await Session.InitializeAsync();
        await Session.RegisterAndSubscribeAsync("user" + Guid.NewGuid().ToString()[..4]);
    }

    protected virtual async Task Teardown() {
        await Session.CloseAsync();
    }

    public async Task DisposeAsync() {
        await server.DisposeAsync();
    }

    // Helpers

    public EventAwaiter<NavigationStateEventArgs> GetNavigationAwaiter() {
        var navigationState = new EventAwaiter<NavigationStateEventArgs>();
        Session.NavigationStateChanged += navigationState.EventHandler;
        return navigationState;
    }

    public EventAwaiter<NavigationStateEventArgs> GetNavigationAwaiter(Func<NavigationStateEventArgs, bool> predicate) {
        var navigationState = new EventAwaiter<NavigationStateEventArgs>(predicate);
        Session.NavigationStateChanged += navigationState.EventHandler;
        return navigationState;
    }

    public async Task<string> SceneOpen() {
        var addAwaiter = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        var name = RandomName();
        await Session.CreateSceneAsync(name);
        await addAwaiter;
        return name;
    }

    public async Task SceneOpenObject(string type = "DobotM1") {
        await SceneOpen();
        var addAwaiter = Session.Scenes.First().ActionObjects!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        await Session.Scenes.First().AddActionObjectAsync(type, "TestObject", new Pose(new Position(), new Orientation()));
        await addAwaiter;
        await Session.Scenes.First().SaveAsync();
    }

    public async Task ProjectOpenObject(string type = "DobotM1") {
        await SceneOpenObject();
        await Session.Scenes.First().CloseAsync();
        var addAwaiter = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        await Session.CreateProjectAsync(Session.Scenes.First(), "ProjectName", "ProjectDesc");
        await addAwaiter;
    }

    public async Task ProjectClosedObject(string type = "DobotM1") {
        await SceneOpenObject();
        await Session.Scenes.First().CloseAsync();
        var addAwaiter = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        await Session.CreateProjectAsync(Session.Scenes.First(), "ProjectName", "ProjectDesc");
        await addAwaiter;
        await Session.Projects.First().SaveAsync();
        await Session.Projects.First().CloseAsync();
    }


    public async Task<string> SceneClosed() {
        var addAwaiter = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        var name = RandomName();
        await Session.CreateSceneAsync(name);
        await addAwaiter;
        await Session.Scenes.First().SaveAsync();
        await Session.Scenes.First().CloseAsync();
        return name;
    }

    public async Task DisposeSceneOpen() {
        try {
            await Session.Scenes.First().CloseAsync(true);
            var remove = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await Session.Scenes.First().RemoveAsync();
            await remove;
        }
        catch { /**/ }
    }

    public async Task DisposeSceneClosed() {
        try {
            var remove = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await Session.Scenes.First().RemoveAsync();
            await remove;
        }
        catch { /**/ }
    }

    public async Task DisposeProjectOpen() {
        try {
            await Session.Projects.First().CloseAsync(true);
            var removeS = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            var removeP = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await Session.Projects.First().RemoveAsync();
            await Session.Scenes.First().RemoveAsync();
            await removeP;
            await removeS;

        }
        catch { /**/ }
    }

    public async Task DisposeProjectClosed() {
        try {
            var removeS = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            var removeP = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await Session.Projects.First().RemoveAsync();
            await Session.Scenes.First().RemoveAsync();
            await removeP;
            await removeS;
        }
        catch { /**/ }
    }
}