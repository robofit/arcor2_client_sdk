using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System.Collections.Specialized;
using Xunit.Abstractions;
using Joint = Arcor2.ClientSdk.ClientServices.Models.Joint;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Tests;

public class Arcor2SessionActionObjectTests(Arcor2ServerFixture fixture, ITestOutputHelper output) : TestBase(fixture, output) {
    [Fact]
    public async Task Create_Dobot_Success() {
        await Setup();
        await SceneOpen();

        // Arrange
        try {
            var addAwaiter = Session.Scenes.First().ActionObjects!
                .CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();

            // Act
            await Session.Scenes.First()
                .AddActionObjectAsync("DobotM1", "Dobot", new Pose(new Position(), new Orientation()));

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

    [Fact]
    public async Task GetKinematics_Dobot_NotEmpty() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var actionObject = Session.Scenes.First().ActionObjects!.First();

        try {
            // Act
            var fk = await actionObject.GetForwardKinematicsAsync();
            var ik = await actionObject.GetInverseKinematicsAsync();
            // Assert
            Assert.NotNull(fk);
            Assert.NotNull(ik);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task SetEndEffectorPerpendicularToWorld_Dobot_Moves() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var actionObject = Session.Scenes.First().ActionObjects!.First();

        try {
            // Arrange
            var robotMoveToPoseAwaiter = new EventAwaiter();
            actionObject.MovingToPose += robotMoveToPoseAwaiter.EventHandler;
            var robotMoveToPoseTask = robotMoveToPoseAwaiter.WaitForEventAsync();
            var updatedAwaiter = new EventAwaiter();
            actionObject.PropertyChanged += updatedAwaiter.EventHandler;
            var updatedTask = updatedAwaiter.WaitForEventAsync();

            // Act
            await actionObject.SetEndEffectorPerpendicularToWorldAsync();

            // Assert
            await Task.WhenAll(robotMoveToPoseTask, updatedTask);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task MoveToPose_DobotImpossible_FailsMove() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var actionObject = Session.Scenes.First().ActionObjects!.First();

        try {
            // Arrange
            var robotMoveToPoseAwaiter =
                new EventAwaiter<RobotMovingToPoseEventArgs>(a => a.MoveEventType == RobotMoveType.Failed);
            actionObject.MovingToPose += (sender, args) => robotMoveToPoseAwaiter.EventHandler(sender, args);
            var robotMoveToPoseTask = robotMoveToPoseAwaiter.WaitForEventAsync();
            var updatedAwaiter = new EventAwaiter();
            actionObject.PropertyChanged += updatedAwaiter.EventHandler;
            var updatedTask = updatedAwaiter.WaitForEventAsync();

            // Act
            await actionObject.MoveToPoseAsync(new Pose(new Position(5, 5, 5), new Orientation()));

            // Assert
            await Task.WhenAll(robotMoveToPoseTask, updatedTask);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task MoveToJoints_DobotSingleArm_Fails() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var actionObject = Session.Scenes.First().ActionObjects!.First();

        try {
            // Arrange
            var robotMoveToPoseAwaiter =
                new EventAwaiter<RobotMovingToJointsEventArgs>(a => a.MoveEventType == RobotMoveType.Failed);
            actionObject.MovingToJoints += robotMoveToPoseAwaiter.EventHandler;
            var robotMoveToPoseTask = robotMoveToPoseAwaiter.WaitForEventAsync();
            var updatedAwaiter = new EventAwaiter();
            actionObject.PropertyChanged += updatedAwaiter.EventHandler;
            var updatedTask = updatedAwaiter.WaitForEventAsync();

            var modifiedJoint = actionObject.Data.Joints!.First();
            var newJoints = actionObject.Data.Joints!.Skip(1)
                .Append(new Joint(modifiedJoint.Id, modifiedJoint.Value - 0.1m));

            // Act
            await actionObject.MoveToJointsAsync(newJoints.ToList());

            // Assert
            await Task.WhenAll(robotMoveToPoseTask, updatedTask);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task MoveToActionPointJoints_Dobot_Moves() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var actionObject = Session.Scenes.First().ActionObjects!.First();
        var actionPoint = Session.Projects.First().ActionPoints!.First();

        try {
            // Arrange
            // TODO: Why does this not catch the raised event?
            //var robotMoveToPoseAwaiter = new EventAwaiter<RobotMovingToActionPointJointsEventArgs>(a => a.MoveEventType == RobotMoveType.Finished);
            //actionObject.MovingToActionPointJoints += robotMoveToPoseAwaiter.EventHandler;
            //var robotMoveToPoseTask = robotMoveToPoseAwaiter.WaitForEventAsync();
            var updatedAwaiter = new EventAwaiter();
            actionObject.PropertyChanged += updatedAwaiter.EventHandler;
            var updatedTask = updatedAwaiter.WaitForEventAsync();

            // Act
            await actionObject.MoveToActionPointJointsAsync(actionPoint.Joints.First());

            // Assert
            await Task.WhenAll( /*robotMoveToPoseTask,*/ updatedTask);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task MoveToActionPointOrientation_Dobot_Moves() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var actionObject = Session.Scenes.First().ActionObjects!.First();
        var actionPoint = Session.Projects.First().ActionPoints!.First();

        try {
            // Arrange
            var robotMoveToPoseAwaiter =
                new EventAwaiter<RobotMovingToActionPointOrientationEventArgs>(a =>
                    a.MoveEventType == RobotMoveType.Finished);
            actionObject.MovingToActionPointOrientation += robotMoveToPoseAwaiter.EventHandler;
            var robotMoveToPoseTask = robotMoveToPoseAwaiter.WaitForEventAsync();
            var updatedAwaiter = new EventAwaiter();
            actionObject.PropertyChanged += updatedAwaiter.EventHandler;
            var updatedTask = updatedAwaiter.WaitForEventAsync();

            // Act
            await actionObject.MoveToActionPointOrientationAsync(actionPoint.Orientations.First());

            // Assert
            await Task.WhenAll(robotMoveToPoseTask, updatedTask);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task StepPosition_Dobot_Move() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var actionObject = Session.Scenes.First().ActionObjects!.First();

        try {
            // Arrange
            var robotMoveToPoseAwaiter =
                new EventAwaiter<RobotMovingToPoseEventArgs>(a => a.MoveEventType == RobotMoveType.Finished);
            actionObject.MovingToPose += (sender, args) => robotMoveToPoseAwaiter.EventHandler(sender, args);
            var robotMoveToPoseTask = robotMoveToPoseAwaiter.WaitForEventAsync();
            var updatedAwaiter = new EventAwaiter();
            actionObject.PropertyChanged += updatedAwaiter.EventHandler;
            var updatedTask = updatedAwaiter.WaitForEventAsync();
            var oldPose = actionObject.Data.Meta.Pose.Position;

            // Act
            await actionObject.StepPositionAsync(Axis.X, 0.1m);

            // Assert
            await Task.WhenAll(updatedTask, robotMoveToPoseTask);
            Assert.True(oldPose.EqualTo(actionObject.Data.Meta.Pose.Position, 0.01m));
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task SetHandTeachingMode_Dobot_FailsUnavailable() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var actionObject = Session.Scenes.First().ActionObjects!.First();

        try {
            // Act
            await Assert.ThrowsAsync<Arcor2Exception>(() => actionObject.SetHandTeachingModeAsync());
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }
}