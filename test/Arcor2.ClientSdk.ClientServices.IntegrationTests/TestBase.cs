using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System.Collections.Specialized;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests;

public class TestBase(Arcor2ServerFixture fixture, ITestOutputHelper output) : IAsyncLifetime, IClassFixture<Arcor2ServerFixture> {
    private Arcor2ServerFixture server = fixture!;
    protected Arcor2Session Session { get; private set; } = null!;
    protected IArcor2Logger Logger { get; private set; } = null!;
    protected Uri Uri => server.Uri;
    protected string Host => server.Host;
    protected ushort Port => server.Port;

    public async Task InitializeAsync() {
        Logger = new TestLogger(output);
        Session = new Arcor2Session(logger: Logger);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync() {

    }

    protected string RandomName() => "rname_" + Guid.NewGuid().ToString()[..4];

    protected virtual async Task Setup() {
        await Session.ConnectAsync(Uri);
        await Session.InitializeAsync();
        await Session.RegisterAndSubscribeAsync("user" + Guid.NewGuid().ToString()[..4]);
    }

    protected virtual async Task Teardown() => await Session.CloseAsync();

    // --------
    // Helpers
    // -------

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

    // Setup sequence methods

    /// <summary>
    ///     Prepares the session with a blank open scene.
    /// </summary>
    public async Task<string> SceneOpen() {
        var addAwaiter = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        var name = RandomName();
        await Session.CreateSceneAsync(name);
        await addAwaiter;
        return name;
    }

    /// <summary>
    ///     Prepares the session with an open scene with action object.
    /// </summary>
    public async Task SceneOpenObject(string type = "DobotM1") {
        await SceneOpen();
        var addAwaiter = Session.Scenes.First().ActionObjects!
            .CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        await Session.Scenes.First()
            .AddActionObjectWithDefaultParametersAsync(type, "TestObject", new Pose(new Position(), new Orientation()));
        await addAwaiter;
        await Session.Scenes.First().SaveAsync();
    }

    /// <summary>
    ///     Deletes session with open scene.
    /// </summary>
    public async Task DisposeSceneOpen() {
        try {
            await Session.Scenes.First().CloseAsync(true);
            var remove = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await Session.Scenes.First().RemoveAsync();
            await remove;
        }
        catch {
            /**/
        }
    }

    /// <summary>
    ///     Prepares the session with a blank closed scene.
    /// </summary>
    public async Task<string> SceneClosed() {
        var addAwaiter = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        var name = RandomName();
        await Session.CreateSceneAsync(name);
        await addAwaiter;
        await Session.Scenes.First().SaveAsync();
        await Session.Scenes.First().CloseAsync();
        return name;
    }

    /// <summary>
    ///     Deletes session with a closed scene.
    /// </summary>
    public async Task DisposeSceneClosed() {
        try {
            var remove = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await Session.Scenes.First().RemoveAsync();
            await remove;
        }
        catch {
            /**/
        }
    }

    /// <summary>
    ///     Prepares the session with a blank open project and its scene.
    /// </summary>
    public async Task ProjectOpen() {
        await SceneClosed();
        var addAwaiter = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await Session.CreateProjectAsync(Session.Scenes.First(), "ProjectName", "ProjectDesc");
        await addAwaiter;
    }

    /// <summary>
    ///     Prepares the session with a blank closed project and its scene.
    /// </summary>
    public async Task ProjectClosed() {
        await SceneClosed();
        var addAwaiter = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await Session.CreateProjectAsync(Session.Scenes.First(), "ProjectName", "ProjectDesc");
        await addAwaiter;
        await Session.Projects.First().SaveAsync();
        await Session.Projects.First().CloseAsync();
    }

    /// <summary>
    ///     Prepares the session with an open project and its scene with action object.
    /// </summary>
    public async Task ProjectOpenObject(string type = "DobotM1") {
        await SceneOpenObject(type);
        await Session.Scenes.First().CloseAsync();
        var addAwaiter = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await Session.CreateProjectAsync(Session.Scenes.First(), "ProjectName", "ProjectDesc");
        await addAwaiter;
    }

    /// <summary>
    ///     Prepares the session with a closed project and its scene with action object.
    /// </summary>
    public async Task ProjectClosedObject() {
        await SceneOpenObject();
        await Session.Scenes.First().CloseAsync();
        var addAwaiter = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await Session.CreateProjectAsync(Session.Scenes.First(), "ProjectName", "ProjectDesc");
        await addAwaiter;
        await Session.Projects.First().SaveAsync();
        await Session.Projects.First().CloseAsync();
    }

    /// <summary>
    ///     Prepares the session with an open project with three action points and its scene.
    /// </summary>
    public async Task ProjectOpenThreeActionPoints() {
        await ProjectOpen();
        var project = Session.Projects.First();
        await project.AddActionPointAsync("TestAP1", new Position());
        await project.AddActionPointAsync("TestAP2", new Position(10, 10, 10));
        var addAwaiter = project.ActionPoints!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await project.AddActionPointAsync("TestAP3", new Position(0, 10, 200));
        await addAwaiter;
    }

    /// <summary>
    ///     Prepares the session with an open project with three action points and its scene with action object.
    /// </summary>
    public async Task ProjectOpenObjectThreeActionPoints(string type = "DobotM1") {
        await ProjectOpenObject(type);
        var project = Session.Projects.First();
        await project.AddActionPointAsync("TestAP1", new Position());
        await project.AddActionPointAsync("TestAP2", new Position(10, 10, 10));
        var addAwaiter = project.ActionPoints!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await project.AddActionPointAsync("TestAP3", new Position(0, 10, 200));
        await addAwaiter;
    }

    /// <summary>
    ///     Prepares the session with a started project with action point with an orientation, joints, and an action; and its
    ///     scene.
    /// </summary>
    public async Task ProjectStartedObjectActionPointWithEntities(string type = "DobotM1") {
        await ProjectOpenObject(type);
        var project = Session.Projects.First();
        var robot = project.Scene.ActionObjects!.First();
        var addAwaiter = project.ActionPoints!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        var startAwaiter = project.Scene.GetStartedAwaiter().WaitForEventAsync();
        await project.Scene.StartAsync();
        await startAwaiter;
        // This will create joints and orientation
        await project.AddActionPointUsingRobotAsync("TestAP1", robot);
        await addAwaiter;
        var ap = project.ActionPoints!.First();
        var actionAddAwaiter = ap.Actions.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await ap.AddActionAsync(
            "home",
            robot,
            robot.ObjectType.Data.Actions.First(a => a.Name == "home"),
            new List<ActionParameter>());
        await actionAddAwaiter;
    }

    /// <summary>
    ///     Prepares the session with started project with a simple program using one action point and the home action.
    /// </summary>
    public async Task ProjectStartedObjectValidProgram() {
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var action = project.ActionPoints!.First().Actions.First();
        var addAwaiter1 = project.LogicItems!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await project.AddLogicItem("START", action.Id);
        await addAwaiter1;
        var addAwaiter2 = project.LogicItems!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await project.AddLogicItem(action.Id, "END");
        await addAwaiter2;
    }

    /// <summary>
    ///     Prepares the session with started project with a simple program using one action point and the home action.
    /// </summary>
    public async Task ProjectClosedObjectValidProgram() {
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var action = project.ActionPoints!.First().Actions.First();
        var addAwaiter1 = project.LogicItems!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await project.AddLogicItem("START", action.Id);
        await addAwaiter1;
        var addAwaiter2 = project.LogicItems!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await project.AddLogicItem(action.Id, "END");
        await project.Scene.StopAsync();
        await project.Scene.GetStoppedAwaiter().WaitForEventAsync();
        await project.SaveAsync();
        await project.CloseAsync();
        await addAwaiter2;
    }

    /// <summary>
    ///     Prepares the session with a closed package using project with a simple program using one action point and the home
    ///     action.
    /// </summary>
    public async Task PackageClosedObjectValidProgram() {
        await ProjectClosedObjectValidProgram();
        var addAwaiter1 = Session.Packages.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await Session.Projects.First().BuildIntoPackageAsync("Package");
        await addAwaiter1;
    }

    /// <summary>
    ///     Deletes session with an open project.
    /// </summary>
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
        catch {
            /**/
        }
    }

    /// <summary>
    ///     Deletes session with started project.
    /// </summary>
    public async Task DisposeProjectStarted() {
        try {
            await Session.Projects.First().Scene.StopAsync();
            await Session.Projects.First().Scene.GetStoppedAwaiter().WaitForEventAsync();
            await DisposeProjectOpen();
        }
        catch {
            /**/
        }
    }

    /// <summary>
    ///     Deletes session with a closed project.
    /// </summary>
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
        catch {
            /**/
        }
    }

    /// <summary>
    ///     Deletes session with a closed package.
    /// </summary>
    public async Task DisposePackageClosed() {
        try {
            var removeP = Session.Packages.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await Session.Projects.First().RemoveAsync();
            await removeP;
            await DisposeProjectClosed();
        }
        catch {
            /**/
        }
    }
}