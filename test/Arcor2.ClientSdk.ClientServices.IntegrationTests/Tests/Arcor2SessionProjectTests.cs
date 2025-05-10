using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System.Collections.Specialized;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Tests;

public class Arcor2SessionProjectTests(Arcor2ServerFixture fixture, ITestOutputHelper output) : TestBase(fixture, output) {
    [Fact]
    public async Task Create_Valid_Creates() {
        await Setup();
        await SceneClosed();
        // Arrange
        var addAwaiter = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();
        var projectName = RandomName();
        var projectDesc = RandomName();

        try {
            // Act
            var record = await Record.ExceptionAsync(() =>
                Session.CreateProjectAsync(Session.Scenes.First(), projectName, projectDesc));

            // Assert
            Assert.Null(record);
            await addAwaiter;
            Assert.Single([Session.Projects.Count]);
            var project = Session.Projects.First();
            Assert.Equal(projectName, project.Data.Name);
            Assert.Equal(projectDesc, project.Data.Description);
            Assert.NotEmpty(project.Data.Id);
            Assert.Empty(project.ActionPoints!);
            Assert.Empty(project.LogicItems!);
            Assert.Empty(project.Overrides!);

            await navigationAwaiter;
            Assert.Equal(NavigationState.Project, Session.NavigationState);
            Assert.Equal(project.Id, Session.NavigationId);
        }
        finally {
            await Session.Projects.First().CloseAsync(true);
            await DisposeSceneOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task Close_NotSavedNotForced_Throws() {
        await Setup();
        // Arrange
        await ProjectOpen();

        try {
            // Act
            var record = await Record.ExceptionAsync(() => Session.Projects.First().CloseAsync());

            // Assert
            Assert.NotNull(record);
            Assert.Single([Session.Projects.Count]);
            Assert.Equal(NavigationState.Project, Session.NavigationState);
            Assert.True(Session.Projects.First().IsOpen);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task Close_NotSavedForced_ClosesNotPersists() {
        await Setup();
        // Arrange
        await ProjectOpen();

        var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();

        try {
            var removeAwaiter = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            // Act
            var record = await Record.ExceptionAsync(() => Session.Projects.First().CloseAsync(true));

            // Assert
            Assert.Null(record);
            await removeAwaiter;
            Assert.Empty(Session.Projects);
            await navigationAwaiter;
            Assert.Equal(NavigationState.MenuListOfProjects, Session.NavigationState);
        }
        finally {
            await DisposeSceneClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task Close_NotForcedAndSaved_Closes() {
        await Setup();
        // Arrange
        await ProjectOpen();
        var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();

        try {
            // Act
            await Session.Projects.First().SaveAsync();
            var record = await Record.ExceptionAsync(() => Session.Projects.First().CloseAsync());

            // Assert
            Assert.Null(record);
            Assert.Single(Session.Scenes);
            await navigationAwaiter;
            Assert.Equal(NavigationState.MenuListOfProjects, Session.NavigationState);
            Assert.NotNull(Session.NavigationId);
        }
        finally {
            await DisposeSceneClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task Close_ForcedAndSaved_Closes() {
        await Setup();
        // Arrange
        await ProjectOpen();
        var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();

        try {
            // Act
            await Session.Projects.First().SaveAsync();
            var record = await Record.ExceptionAsync(() => Session.Projects.First().CloseAsync(true));

            // Assert
            Assert.Null(record);
            Assert.Single(Session.Projects);
            await navigationAwaiter;
            Assert.Equal(NavigationState.MenuListOfProjects, Session.NavigationState);
            Assert.NotNull(Session.NavigationId);
        }
        finally {
            await DisposeProjectClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task Remove_Valid_Removed() {
        await Setup();
        // Arrange
        await ProjectClosed();

        try {
            var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();
            var removingAwaiter = Session.Projects.First().GetRemovingAwaiterAndWait();
            var remove = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            // Act
            var record = await Record.ExceptionAsync(() => Session.Projects.First().RemoveAsync());

            // Assert
            await removingAwaiter;
            await remove;
            Assert.Null(record);
            Assert.Empty(Session.Projects);
            await navigationAwaiter;
            Assert.Equal(NavigationState.MenuListOfProjects, Session.NavigationState);
        }
        finally {
            await DisposeSceneClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task Rename_Valid_Renames() {
        await Setup();
        // Arrange
        await ProjectClosed();
        var changedAwaiter = Session.Projects.First().GetUpdatedAwaiterAndWait();

        var newName = RandomName();
        try {
            // Act
            var record = await Record.ExceptionAsync(() => Session.Projects.First().RenameAsync(newName));

            // Assert
            Assert.Null(record);
            await changedAwaiter;
            Assert.Single(Session.Projects);
            Assert.Equal(newName, Session.Projects.First().Data.Name);
        }
        finally {
            await DisposeProjectClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateDescription_Valid_Updates() {
        await Setup();
        // Arrange
        await ProjectClosed();

        var changedAwaiter = Session.Projects.First().GetUpdatedAwaiterAndWait();

        var newDesc = RandomName();
        try {
            // Act
            var record = await Record.ExceptionAsync(() => Session.Projects.First().UpdateDescriptionAsync(newDesc));

            // Assert
            Assert.Null(record);
            await changedAwaiter;
            Assert.Single(Session.Projects);
            Assert.Equal(newDesc, Session.Projects.First().Data.Description);
        }
        finally {
            await DisposeProjectClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task Open_Valid_Opens() {
        await Setup();
        // Arrange
        await ProjectClosed();
        var navigationAwaiter = GetNavigationAwaiter(s => s.State == NavigationState.Project).WaitForEventAsync();

        try {
            // Act
            var record = await Record.ExceptionAsync(() => Session.Projects.First().OpenAsync());

            // Assert
            Assert.Null(record);
            await navigationAwaiter;
            Assert.True(Session.Projects.First().IsOpen);
            Assert.Equal(NavigationState.Project, Session.NavigationState);
            Assert.Equal(Session.Projects.First().Id, Session.NavigationId);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task SetHasLogic_Valid_Changes() {
        await Setup();
        // Arrange
        await ProjectClosed();
        var project = Session.Projects.First();
        try {
            // Act
            var changedAwaiterEvent = new EventAwaiter();
            project.PropertyChanged += changedAwaiterEvent.EventHandler;
            var changedAwaiter = changedAwaiterEvent.WaitForEventAsync();

            await project.SetHasLogicAsync(false);

            await changedAwaiter;

            // Assert
            Assert.False(project.Data.HasLogic);
        }
        finally {
            await DisposeProjectClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task Duplicate_Valid_Duplicates() {
        await Setup();
        // Arrange
        await ProjectClosed();
        var project = Session.Projects.First();
        try {
            // Act
            var addAwaiter = Session.Projects.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();

            await project.DuplicateAsync("CopyProject");

            await addAwaiter;

            // Assert
            Assert.Equal(2, Session.Projects.Count);
        }
        finally {
            await Session.Projects.Last().RemoveAsync();
            await DisposeProjectClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task AddOverride_Valid_Adds() {
        await Setup();
        // Arrange
        await ProjectOpenObject();
        var project = Session.Projects.First();
        var robot = project.Scene.ActionObjects!.First();
        var parameter = robot.Data.Meta.Parameters.First(p => p.Name == "url");
        try {
            // Act
            var addAwaiter = project.Overrides!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();

            await project.AddOverrideAsync(robot, new Parameter(parameter.Name, parameter.Type, "\"new_value\""));

            await addAwaiter;

            // Assert
            Assert.Contains(project.Overrides!,
                p => p.Data.Parameter is { Name: "url", Type: "string", Value: "\"new_value\"" });
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task RemoveOverride_Valid_Remove() {
        await Setup();
        // Arrange
        await ProjectOpenObject();
        var project = Session.Projects.First();
        var robot = project.Scene.ActionObjects!.First();
        var parameter = robot.Data.Meta.Parameters.First(p => p.Name == "url");
        try {
            var addAwaiter = project.Overrides!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            await project.AddOverrideAsync(robot, new Parameter(parameter.Name, parameter.Type, "\"new_value\""));
            await addAwaiter;
            var @override = project.Overrides!.FirstOrDefault(o => o.Data.Parameter.Name == parameter.Name);
            // Act
            var removeAwaiter = project.Overrides!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await @override!.RemoveAsync();
            await removeAwaiter;

            // Assert
            Assert.Empty(project.Overrides!);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateOverride_Valid_ChangesValue() {
        await Setup();
        // Arrange
        await ProjectOpenObject();
        var project = Session.Projects.First();
        var robot = project.Scene.ActionObjects!.First();
        var parameter = robot.Data.Meta.Parameters.First(p => p.Name == "url");
        try {
            var addAwaiter = project.Overrides!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            await project.AddOverrideAsync(robot, new Parameter(parameter.Name, parameter.Type, "\"new_value\""));
            await addAwaiter;
            var @override = project.Overrides!.FirstOrDefault(o => o.Data.Parameter.Name == parameter.Name);
            // Act
            var changedAwaiterEvent = new EventAwaiter();
            @override!.PropertyChanged += changedAwaiterEvent.EventHandler;
            var changedAwaiter = changedAwaiterEvent.WaitForEventAsync();
            await @override.UpdateAsync(new Parameter(parameter.Name, parameter.Type, "\"newer_value\""));
            await changedAwaiter;

            // Assert
            Assert.Equal("\"newer_value\"", @override.Data.Parameter.Value);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task AddParameter_Valid_Adds() {
        await Setup();
        // Arrange
        await ProjectOpen();
        var project = Session.Projects.First();
        try {
            // Act
            var addAwaiter = project.Parameters!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();

            await project.AddProjectParameterAsync("test_param", "string", "\"value\"");

            await addAwaiter;

            // Assert
            Assert.Contains(project.Parameters!,
                p => p.Data is { Name: "test_param", Type: "string", Value: "\"value\"" });
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task RemoveParameter_Valid_Removed() {
        await Setup();
        // Arrange
        await ProjectOpen();
        var project = Session.Projects.First();
        try {
            var addAwaiter = project.Parameters!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            await project.AddProjectParameterAsync("test_param", "string", "\"value\"");
            await addAwaiter;
            var parameter = project.Parameters!.FirstOrDefault(p => p.Data.Name == "test_param")!;

            // Act
            var removeAwaiter = project.Parameters!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await parameter.RemoveAsync();
            await removeAwaiter;

            // Assert
            Assert.DoesNotContain(project.Parameters!,
                p => p.Data is { Name: "test_param", Type: "string", Value: "\"value\"" });
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateParameter_Valid_ChangesNameAndValue() {
        await Setup();
        // Arrange
        await ProjectOpen();
        var project = Session.Projects.First();
        try {
            var addAwaiter = project.Parameters!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            await project.AddProjectParameterAsync("test_param", "string", "\"value\"");
            await addAwaiter;
            var parameter = project.Parameters!.FirstOrDefault(p => p.Data.Name == "test_param")!;

            // Act
            var changedAwaiterEvent = new EventAwaiter();
            parameter.PropertyChanged += changedAwaiterEvent.EventHandler;
            var changedAwaiter = changedAwaiterEvent.WaitForEventAsync();

            await parameter.UpdateNameAsync("new_name");

            await changedAwaiter;
            changedAwaiterEvent = new EventAwaiter();
            parameter.PropertyChanged += changedAwaiterEvent.EventHandler;
            var changedAwaiter2 = changedAwaiterEvent.WaitForEventAsync();
            await parameter.UpdateValueAsync("\"new_value\"");

            await changedAwaiter2;

            // Assert
            Assert.Contains(project.Parameters!,
                p => p.Data is { Name: "new_name", Type: "string", Value: "\"new_value\"" });
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task AddLogicItem_Valid_AddsLogicItem() {
        await Setup();
        // Arrange
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var action = project.ActionPoints!.First().Actions.First();
        try {
            // Act
            var addAwaiter1 = project.LogicItems!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            await project.AddLogicItem("START", action.Id);
            await addAwaiter1;
            var addAwaiter2 = project.LogicItems!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            await project.AddLogicItem(action.Id, "END");
            await addAwaiter2;

            // Assert
            Assert.Equal(2, project.LogicItems!.Count);

            var firstItem = project.LogicItems.First();
            var lastItem = project.LogicItems.Last();
            Assert.Equal("START", firstItem.Data.Start);
            Assert.Equal(action.Id, firstItem.Data.End);
            Assert.Equal(action.Id, lastItem.Data.Start);
            Assert.Equal("END", lastItem.Data.End);

            Assert.Null(firstItem.Data.Condition);
            Assert.Null(lastItem.Data.Condition);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateLogicItem_Valid_Updates() {
        await Setup();
        // Arrange
        await ProjectStartedObjectValidProgram();
        var project = Session.Projects.First();
        var logicItems = project.LogicItems!.ToList();
        var firstLogicItem = logicItems[0];
        var secondLogicItem = logicItems[1];
        var action = firstLogicItem.EndActionManager!;

        try {
            // Act
            var removeAwaiter = project.LogicItems!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await firstLogicItem.RemoveAsync();
            await removeAwaiter;

            var updatedAwaiter = new EventAwaiter();
            secondLogicItem.PropertyChanged += updatedAwaiter.EventHandler;
            var updateTask = updatedAwaiter.WaitForEventAsync();

            await secondLogicItem.UpdateAsync("START", action.Id); // Like the first logic item
            await updateTask;

            // Assert
            Assert.Single(project.LogicItems!);
            var updatedLogicItem = project.LogicItems!.First();
            Assert.Equal("START", updatedLogicItem.Data.Start);
            Assert.Equal(action.Id, updatedLogicItem.Data.End);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task RemoveLogicItem_Valid_Removes() {
        await Setup();
        // Arrange
        await ProjectStartedObjectValidProgram();
        var project = Session.Projects.First();
        var logicItems = project.LogicItems!.ToList();
        var firstLogicItem = logicItems[0];

        try {
            // Act
            var removeAwaiter = project.LogicItems!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await firstLogicItem.RemoveAsync();
            await removeAwaiter;

            // Assert
            Assert.Single(project.LogicItems!);
            var remainingLogicItem = project.LogicItems!.First();
            Assert.Equal(logicItems[1].Id, remainingLogicItem.Id);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task StartAndStop_Valid_StartsAndStops() {
        await Setup();
        // Arrange
        await ProjectOpen();
        await Session.Projects.First().SaveAsync();

        try {
            var startedAwaiterEvent =
                new EventAwaiter<SceneOnlineStateEventArgs>(e => e.State.State == OnlineState.Started);
            Session.Projects.First().Scene.OnlineStateChanged += startedAwaiterEvent.EventHandler;
            var startedAwaiter = startedAwaiterEvent.WaitForEventAsync();

            // Act
            var startRecord = await Record.ExceptionAsync(() => Session.Projects.First().Scene.StartAsync());

            // Assert
            await startedAwaiter;
            Assert.Null(startRecord);
            Assert.Equal(OnlineState.Started, Session.Projects.First().Scene.State.State);
            Assert.Null(Session.Projects.First().Scene.State.Message);

            // Arrange
            var stoppedAwaiterEvent =
                new EventAwaiter<SceneOnlineStateEventArgs>(e => e.State.State == OnlineState.Stopped);
            Session.Projects.First().Scene.OnlineStateChanged += stoppedAwaiterEvent.EventHandler;
            var stoppedAwaiter = stoppedAwaiterEvent.WaitForEventAsync();

            // Act
            var stopRecord = await Record.ExceptionAsync(() => Session.Projects.First().Scene.StopAsync());

            // Assert
            await stoppedAwaiter;
            Assert.Null(stopRecord);
            Assert.Equal(OnlineState.Stopped, Session.Projects.First().Scene.State.State);
            Assert.Null(Session.Projects.First().Scene.State.Message);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }
}