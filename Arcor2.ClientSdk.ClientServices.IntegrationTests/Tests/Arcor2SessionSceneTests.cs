﻿using System.Collections.Specialized;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Tests;

public class Arcor2SessionSceneTests(ITestOutputHelper output) : TestBase(output) {
    [Fact]
    public async Task Create_Valid_Creates() {
        await Setup();
        // Arrange
        var addAwaiter = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();
        var sceneName = RandomName();
        var sceneDesc = RandomName();

        try {
            // Act
            var record = await Record.ExceptionAsync(() => Session.CreateSceneAsync(sceneName, sceneDesc));

            // Assert
            Assert.Null(record);
            await addAwaiter;
            Assert.Single([Session.Scenes.Count]);
            var scene = Session.Scenes.First();
            Assert.Equal(sceneName, scene.Data.Name);
            Assert.Equal(sceneDesc, scene.Data.Description);
            Assert.NotEmpty(scene.Data.Id);
            Assert.Empty(scene.ActionObjects!);

            await navigationAwaiter;
            Assert.Equal(NavigationState.Scene, Session.NavigationState);
            Assert.Equal(scene.Id, Session.NavigationId);
        }
        finally {
            await Session.Scenes.First().CloseAsync(true);
            await Teardown();
        }
    }

    [Fact]
    public async Task Close_NotSavedNotForced_Throws() {
        await Setup();
        // Arrange
        await SceneOpen();

        try {
            // Act
            var record = await Record.ExceptionAsync(() => Session.Scenes.First().CloseAsync());

            // Assert
            Assert.NotNull(record);
            Assert.Single([Session.Scenes.Count]);
            Assert.Equal(NavigationState.Scene, Session.NavigationState);
            Assert.True(Session.Scenes.First().IsOpen);
        }
        finally {
            await Session.Scenes.First().CloseAsync(true);
            await Teardown();
        }
    }


    [Fact]
    public async Task Close_NotSavedForced_ClosesNotPersists() {
        await Setup();
        // Arrange
        await SceneOpen();
        var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();

        try {
            var removeAwaiter = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove).WaitForEventAsync();
            // Act
            var record = await Record.ExceptionAsync(() => Session.Scenes.First().CloseAsync(true));

            // Assert
            Assert.Null(record);
            await removeAwaiter;
            Assert.Empty(Session.Scenes);
            await navigationAwaiter;
            Assert.Equal(NavigationState.MenuListOfScenes, Session.NavigationState);

        }
        finally {
            await Teardown();
        }
    }

    [Fact]
    public async Task Close_NotForcedAndSaved_Closes() {
        await Setup();
        // Arrange
        await SceneOpen();
        var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();

        try {
            // Act
            await Session.Scenes.First().SaveAsync();
            var record = await Record.ExceptionAsync(() => Session.Scenes.First().CloseAsync());

            // Assert
            Assert.Null(record);
            Assert.Single(Session.Scenes);
            await navigationAwaiter;
            Assert.Equal(NavigationState.MenuListOfScenes, Session.NavigationState);
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
        await SceneOpen();
        var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();

        try {
            // Act
            await Session.Scenes.First().SaveAsync();
            var record = await Record.ExceptionAsync(() => Session.Scenes.First().CloseAsync(true));

            // Assert
            Assert.Null(record);
            Assert.Single(Session.Scenes);
            await navigationAwaiter;
            Assert.Equal(NavigationState.MenuListOfScenes, Session.NavigationState);
            Assert.NotNull(Session.NavigationId);

        }
        finally {
            await DisposeSceneClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task Remove_Valid_Removed() {
        await Setup();
        // Arrange
        await SceneClosed();

        try {
            var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();
            var removingAwaiter = Session.Scenes.First().GetRemovingAwaiterAndWait();
            var remove = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove).WaitForEventAsync();
            // Act
            var record = await Record.ExceptionAsync(() => Session.Scenes.First().RemoveAsync());

            // Assert
            await removingAwaiter;
            await remove;
            Assert.Null(record);
            Assert.Empty(Session.Scenes);
            await navigationAwaiter;
            Assert.Equal(NavigationState.MenuListOfScenes, Session.NavigationState);
        }
        finally {
            await Teardown();
        }
    }



    [Fact]
    public async Task Rename_Valid_Renames() {
        await Setup();
        // Arrange
        await SceneClosed();
        var changedAwaiter = Session.Scenes.First().GetUpdatedAwaiterAndWait();

        var newName = RandomName();
        try {
            // Act
            var record = await Record.ExceptionAsync(() => Session.Scenes.First().RenameAsync(newName));

            // Assert
            Assert.Null(record);
            await changedAwaiter;
            Assert.Single(Session.Scenes);
            Assert.Equal(newName, Session.Scenes.First().Data.Name);

        }
        finally {
            await DisposeSceneClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateDescription_Valid_Updates() {
        await Setup();
        // Arrange
        await SceneClosed();

        var changedAwaiter = Session.Scenes.First().GetUpdatedAwaiterAndWait();

        var newDesc = RandomName();
        try {
            // Act
            var record = await Record.ExceptionAsync(() => Session.Scenes.First().UpdateDescriptionAsync(newDesc));

            // Assert
            Assert.Null(record);
            await changedAwaiter;
            Assert.Single(Session.Scenes);
            Assert.Equal(newDesc, Session.Scenes.First().Data.Description);

        }
        finally {
            await DisposeSceneClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task Open_Valid_Opens() {
        await Setup();
        // Arrange
        await SceneClosed();
        var navigationAwaiter = GetNavigationAwaiter(s => s.State == NavigationState.Scene).WaitForEventAsync();

        try {
            // Act
            var record = await Record.ExceptionAsync(() => Session.Scenes.First().OpenAsync());

            // Assert
            Assert.Null(record);
            await navigationAwaiter;
            Assert.True(Session.Scenes.First().IsOpen);
            Assert.Equal(NavigationState.Scene, Session.NavigationState);
            Assert.Equal(Session.Scenes.First().Id, Session.NavigationId);

        }
        finally {
            await DisposeSceneOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task StartAndStop_Valid_StartsAndStops() {
        await Setup();
        // Arrange
        await SceneOpen();
        await Session.Scenes.First().SaveAsync();

        try {

            var startedAwaiterEvent = new EventAwaiter<SceneOnlineStateEventArgs>(e => e.State.State == OnlineState.Started);
            Session.Scenes.First().OnlineStateChanged += startedAwaiterEvent.EventHandler;
            var startedAwaiter = startedAwaiterEvent.WaitForEventAsync();

            // Act
            var startRecord = await Record.ExceptionAsync(() => Session.Scenes.First().StartAsync());

            // Assert
            await startedAwaiter;
            Assert.Null(startRecord);
            Assert.Equal(OnlineState.Started, Session.Scenes.First().State.State);
            Assert.Null(Session.Scenes.First().State.Message);

            // Arrange
            var stoppedAwaiterEvent = new EventAwaiter<SceneOnlineStateEventArgs>(e => e.State.State == OnlineState.Stopped);
            Session.Scenes.First().OnlineStateChanged += stoppedAwaiterEvent.EventHandler;
            var stoppedAwaiter = stoppedAwaiterEvent.WaitForEventAsync();

            // Act
            var stopRecord = await Record.ExceptionAsync(() => Session.Scenes.First().StopAsync());

            // Assert
            await stoppedAwaiter;
            Assert.Null(stopRecord);
            Assert.Equal(OnlineState.Stopped, Session.Scenes.First().State.State);
            Assert.Null(Session.Scenes.First().State.Message);
        }
        finally {
            await DisposeSceneOpen();
            await Teardown();
        }
    }
}