using System.Collections.Specialized;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Tests;

public class Arcor2SessionActionObjectTests(ITestOutputHelper output) : TestBase(output) {
    [Fact]
    public async Task Create_Dobot_Success() {
        await Setup();
        await SceneOpen();

        // Arrange
        try {
            var addAwaiter = Session.Scenes.First().ActionObjects!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();

            // Act
            await Session.Scenes.First().AddActionObjectAsync("DobotM1", "Dobot", new Pose(new Position(0,0,0), new Orientation(0,0,0,1)));

            // Assert
            await addAwaiter;
            Assert.NotNull(Session.Scenes.First().ActionObjects);
            Assert.NotEmpty(Session.Scenes.First().ActionObjects!);
            Assert.Equal("Dobot", Session.Scenes.First().ActionObjects!.First().Data.Meta.Name);
        }
        finally {
            await DisposeSceneOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task Rename_Dobot_Success() {
        await Setup();
        await SceneOpenObject();
        var actionObject = Session.Scenes.First().ActionObjects!.First();

        // Arrange
        try {
            var updatedAwaiter = actionObject.GetUpdatedAwaiterAndWait();

            // Act
            await actionObject.RenameAsync("newTestName");

            // Assert
            await updatedAwaiter;
            Assert.Equal("newTestName", actionObject.Data.Meta.Name);
        }
        finally {
            await DisposeSceneOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdatePose_Dobot_Success() {
        await Setup();
        await SceneOpenObject();
        var actionObject = Session.Scenes.First().ActionObjects!.First();

        // Arrange
        try {
            var updatedAwaiter = actionObject.GetUpdatedAwaiterAndWait();

            // Act
            await actionObject.UpdatePoseAsync(new Pose(new Position(50, 50, 50), new Orientation()));

            // Assert
            await updatedAwaiter;
            Assert.Equal(50, actionObject.Data.Meta.Pose.Position.X);
            Assert.Equal(50, actionObject.Data.Meta.Pose.Position.Y);
            Assert.Equal(50, actionObject.Data.Meta.Pose.Position.Z);
            Assert.Equal(0, actionObject.Data.Meta.Pose.Orientation.X);
            Assert.Equal(0, actionObject.Data.Meta.Pose.Orientation.Y);
            Assert.Equal(0, actionObject.Data.Meta.Pose.Orientation.Z);
            Assert.Equal(1, actionObject.Data.Meta.Pose.Orientation.W);
        }
        finally {
            await DisposeSceneOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateParameters_Dobot_Success() {
        await Setup();
        await SceneOpenObject();
        var actionObject = Session.Scenes.First().ActionObjects!.First();

        // Arrange
        try {
            var updatedAwaiter = actionObject.GetUpdatedAwaiterAndWait();

            var parameter = actionObject.ObjectType.Data.Meta.Settings.First().ToParameter();
            parameter.Value = "\"https://192.168.0.0/this/is/a/test\"";
            // Act
            await actionObject.UpdateParametersAsync([parameter]);

            // Assert
            await updatedAwaiter;
            Assert.Equal("\"https://192.168.0.0/this/is/a/test\"", actionObject.Data.Meta.Parameters.First().Value);
        }
        finally {
            await DisposeSceneOpen();
            await Teardown();
        }
    }
}