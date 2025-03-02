using System.Collections.Specialized;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Tests;

public class Arcor2SessionSceneTests(Arcor2ServerFixture serverFixture, ITestOutputHelper output) : TestBase(serverFixture, output) {
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
        var addAwaiter = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        await Session.CreateSceneAsync(RandomName());
        await addAwaiter;

        try {
            // Act
            var record = await Record.ExceptionAsync(() => Session.Scenes.First().CloseAsync());

            // Assert
            Assert.NotNull(record);
            Assert.Single([Session.Scenes.Count]);
            Assert.Equal(NavigationState.Scene, Session.NavigationState);
        }
        finally {
            await Session.Scenes.First().CloseAsync(true);
            await Teardown();
        }
    }


    [Fact]
    public async Task Close_NotSavedForced_Closes() {
        await Setup();
        // Arrange
        var addAwaiter = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        await Session.CreateSceneAsync(RandomName());
        await addAwaiter;
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
        var addAwaiter = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        await Session.CreateSceneAsync(RandomName());
        await addAwaiter;
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
            await Session.Scenes.First().RemoveAsync();
            await Teardown();
        }
    }

    [Fact]
    public async Task Close_ForcedAndSaved_Closes() {
        await Setup();
        // Arrange
        var addAwaiter = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        await Session.CreateSceneAsync(RandomName());
        await addAwaiter;
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
            await Session.Scenes.First().RemoveAsync();
            await Teardown();
        }
    }

    [Fact]
    public async Task Remove_Valid_Removed() {
        await Setup();
        // Arrange
        var add = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
        await Session.CreateSceneAsync(RandomName());
        await add;
        await Session.Scenes.First().SaveAsync();
        await Session.Scenes.First().CloseAsync();

        try {
            var navigationAwaiter = GetNavigationAwaiter().WaitForEventAsync();
            var remove = Session.Scenes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove).WaitForEventAsync();
            // Act
            var record = await Record.ExceptionAsync(() => Session.Scenes.First().RemoveAsync());

            // Assert
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
}