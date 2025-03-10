using System.Collections.Specialized;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Tests;

public class Arcor2SessionObjectTypeTests(ITestOutputHelper output) : TestBase(output) {
    [Fact]
    public async Task InitializedObjectTypes_ExpectedTypes_AreThere() {
        await Setup();

        Assert.NotEmpty(Session.ObjectTypes);
        Assert.Contains(Session.ObjectTypes, m => m.Data.Meta.Type == "Generic");
        Assert.Contains(Session.ObjectTypes, m => m.Data.Meta.Type == "CollisionObject");
        Assert.Contains(Session.ObjectTypes, m => m.Data.Meta.Type == "GenericWithPose");
        Assert.Contains(Session.ObjectTypes, m => m.Data.Meta.Type == "VirtualCollisionObject");
        Assert.Contains(Session.ObjectTypes, m => m.Data.Meta.Type == "MultiArmRobot");
        Assert.Contains(Session.ObjectTypes, m => m.Data.Meta.Type == "Camera");
        Assert.Contains(Session.ObjectTypes, m => m.Data.Meta.Type == "DobotM1");
        Assert.Contains(Session.ObjectTypes, m => m.Data.Meta.Type == "DobotMagician");

        await Teardown();
    }

    [Fact]
    public async Task Create_MinimalData_Success() {
        await Setup();

        // Arrange
        var name = "OT" + RandomName()[..4];
        try {
            var addAwaiter = Session.ObjectTypes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();

            // Act
            var record = await Record.ExceptionAsync(() => Session.CreateObjectTypeAsync(name));

            // Assert
            await addAwaiter;
            Assert.Null(record);
            var type = Session.ObjectTypes.FirstOrDefault(o => o.Id == name);
            Assert.NotNull(type);
            Assert.Null(type.Data.RobotMeta);
            Assert.Empty(type.Data.Actions);
        }
        finally {
            await Session.ObjectTypes.First(o => o.Id == name).DeleteAsync();
            await Teardown();
        }
    }

    [Fact]
    public async Task Create_MaximalData_Success() {
        await Setup();

        // Arrange
        var name = "OT" + RandomName()[..4];
        try {
            var addAwaiter = Session.ObjectTypes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();

            // Act
            var record = await Record.ExceptionAsync(() => Session.CreateObjectTypeAsync(name,
                "CollisionObject",
                "desc",
                new BoxCollisionModel(1, 1, 1), 
                null!, 
                false, 
                true, 
                false, 
                [new ParameterMeta("Param", "string", false, null!, "Description", "\"value\"")]));

            // Assert
            await addAwaiter;
            Assert.Null(record);
            var type = Session.ObjectTypes.FirstOrDefault(o => o.Id == name);
            Assert.NotNull(type);
            Assert.Null(type.Data.RobotMeta);
            Assert.Empty(type.Data.Actions);
            Assert.NotEmpty(type.Data.Meta.Description);
            Assert.NotEmpty(type.Data.Meta.Base);
            Assert.NotNull(type.Data.Meta.ObjectModel);
            Assert.NotEmpty(type.Data.Meta.Settings);
        }
        finally {
            await Session.ObjectTypes.First(o => o.Id == name).DeleteAsync();
            await Teardown();
        }
    }

    [Fact]
    public async Task Delete_Valid_Deletes() {
        await Setup();

        // Arrange
        var name = "OT" + RandomName()[..4];
        var addAwaiter = Session.ObjectTypes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await Session.CreateObjectTypeAsync(name);
        await addAwaiter;

        try {
            var type = Session.ObjectTypes.First(o => o.Id == name);
            var removeAwaiter = Session.ObjectTypes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();

            // Act
            await type.DeleteAsync();

            // Assert
            await removeAwaiter;
            var typeTest = Session.ObjectTypes.FirstOrDefault(o => o.Id == name);
            Assert.Null(typeTest);
        }
        finally {
            await Teardown();
        }
    }



    [Fact]
    public async Task UpdateObjectModel_Model_Updates() {
        await Setup();

        // Arrange
        var name = "OT" + RandomName()[0..4];
        var addAwaiter = Session.ObjectTypes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await Session.CreateObjectTypeAsync(name, "CollisionObject", model: new BoxCollisionModel(2, 2, 2));
        await addAwaiter;

        // Only when opened
        await SceneOpen();

        try {
            var type = Session.ObjectTypes.First(o => o.Id == name);
            var updatedAwaiter = type.GetRemovingAwaiterAndWait();
            // Act
            await type.UpdateObjectModel(new BoxCollisionModel(6, 6, 6));

            // Assert
            await updatedAwaiter;
            var typeTest = Session.ObjectTypes.FirstOrDefault(o => o.Id == name);
            Assert.NotNull(typeTest!.Data.Meta.ObjectModel.Box);
            Assert.Equal(6, typeTest!.Data.Meta.ObjectModel.Box.SizeX);
            Assert.Equal(6, typeTest!.Data.Meta.ObjectModel.Box.SizeY);
            Assert.Equal(6, typeTest!.Data.Meta.ObjectModel.Box.SizeZ);
        }
        finally {
            await Session.ObjectTypes.First(o => o.Id == name).DeleteAsync();
            await DisposeSceneOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task ReloadActions_NoActions_StillEmpty() {
        await Setup();

        // Arrange
        var name = "OT" + RandomName()[0..4];
        var addAwaiter = Session.ObjectTypes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await Session.CreateObjectTypeAsync(name);
        await addAwaiter;

        try {
            var type = Session.ObjectTypes.First(o => o.Id == name);
            var updatedAwaiter = type.GetRemovingAwaiterAndWait();
            // Act
            await type.ReloadActionsAsync();

            // Assert
            await updatedAwaiter;
            var typeTest = Session.ObjectTypes.FirstOrDefault(o => o.Id == name);
            Assert.Empty(typeTest!.Data.Actions);
        }
        finally {
            await Session.ObjectTypes.First(o => o.Id == name).DeleteAsync();
            await Teardown();
        }
    }

    [Fact]
    public async Task GetUsingScenes_NoScenes_Empty() {
        await Setup();

        // Arrange
        var name = "OT" + RandomName()[0..4];
        var addAwaiter = Session.ObjectTypes.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
            .WaitForEventAsync();
        await Session.CreateObjectTypeAsync(name);
        await addAwaiter;

        try {
            var type = Session.ObjectTypes.First(o => o.Id == name);

            // Act
            var scenes = await type.GetUsingScenesAsync();

            // Assert
            Assert.Empty(scenes);
        }
        finally {
            await Session.ObjectTypes.First(o => o.Id == name).DeleteAsync();
            await Teardown();
        }
    }

    [Fact]
    public async Task GetUsingScenes_OneScene_One() {
        await Setup();
        await SceneOpenObject();

        try {
            var type = Session.ObjectTypes.First(o => o.Id == "DobotM1");
            var scenes = await type.GetUsingScenesAsync();
            Assert.Single(scenes);
        }
        finally {
            await DisposeSceneOpen();
            await Teardown();
        }
    }
}