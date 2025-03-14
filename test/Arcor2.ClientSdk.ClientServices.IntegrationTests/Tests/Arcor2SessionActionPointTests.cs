using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System.Collections.Specialized;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Tests;

public class Arcor2SessionProjectActionPointTests(ITestOutputHelper output) : TestBase(output) {
    [Fact]
    public async Task CreateActionPoint_Valid_Creates() {
        await Setup();
        await ProjectOpen();

        try {
            // Arrange
            var addAwaiter = Session.Projects.First().ActionPoints!
                .CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
            var actionPointName = RandomName();

            // Act
            var record = await Record.ExceptionAsync(() =>
                Session.Projects.First().AddActionPointAsync(actionPointName, new Position()));

            // Assert
            Assert.Null(record);
            await addAwaiter;
            Assert.Single(Session.Projects.First().ActionPoints!);
            var actionPoint = Session.Projects.First().ActionPoints!.First();
            Assert.Equal(actionPointName, actionPoint.Data.Name);
            Assert.NotEmpty(actionPoint.Data.Id);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task CreateActionPointUsingRobot_Valid_Creates() {
        await Setup();
        await ProjectOpenObject("DobotMagician");
        var project = Session.Projects.First();

        try {
            // Arrange
            var addAwaiter = project.ActionPoints!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            var actionPointName = RandomName();
            var robot = project.Scene.ActionObjects!.First();
            await project.Scene.StartAsync();
            await project.Scene.GetStartedAwaiter().WaitForEventAsync();

            // Act
            var record =
                await Record.ExceptionAsync(() => project.AddActionPointUsingRobotAsync(actionPointName, robot));

            // Assert
            Assert.Null(record);
            await addAwaiter;
            Assert.Single(project.ActionPoints!);
        }
        finally {
            await project.Scene.StopAsync();
            await project.Scene.GetStoppedAwaiter().WaitForEventAsync();
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateActionPointUsingRobot_Valid_ChangesPosition() {
        await Setup();
        await ProjectOpenObjectThreeActionPoints("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();

        var originalPosition = actionPoint.Data.Position;

        try {
            // Arrange
            await project.Scene.StartAsync();
            await project.Scene.GetStartedAwaiter().WaitForEventAsync();
            // Act
            var changedAwaiterEvent = new EventAwaiter();
            actionPoint.Updated += changedAwaiterEvent.EventHandler;
            var changedAwaiter = changedAwaiterEvent.WaitForEventAsync();

            await actionPoint.UpdateUsingRobotAsync(project.Scene.ActionObjects!.First());

            // Assert
            await changedAwaiter;
            Assert.False(originalPosition.EqualTo(actionPoint.Data.Position));
        }
        finally {
            await project.Scene.StopAsync();
            await project.Scene.GetStoppedAwaiter().WaitForEventAsync();
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateActionPointPose_Valid_Updates() {
        await Setup();
        await ProjectOpenThreeActionPoints();

        try {
            // Arrange
            var actionPoint = Session.Projects.First().ActionPoints!.First();
            var newPosition = new Position(11, 123, 321);

            // Act
            var changedAwaiterEvent = new EventAwaiter();
            actionPoint.Updated += changedAwaiterEvent.EventHandler;
            var changedAwaiter = changedAwaiterEvent.WaitForEventAsync();

            await actionPoint.UpdatePositionAsync(newPosition);

            await changedAwaiter;

            // Assert
            Assert.Equal(newPosition, actionPoint.Data.Position);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task RemoveActionPoint_Valid_Removes() {
        await Setup();
        await ProjectOpenThreeActionPoints();

        try {
            var actionPoint = Session.Projects.First().ActionPoints!.First();

            // Act
            var removeAwaiter = Session.Projects.First().ActionPoints!
                .CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove).WaitForEventAsync();
            await actionPoint.RemoveAsync();
            await removeAwaiter;

            // Assert
            Assert.Equal(2, Session.Projects.First().ActionPoints!.Count);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateActionPointName_Valid_Updates() {
        await Setup();
        await ProjectOpenThreeActionPoints();

        try {
            // Arrange
            var actionPoint = Session.Projects.First().ActionPoints!.First();
            var newName = RandomName();

            // Act
            var changedAwaiterEvent = new EventAwaiter();
            actionPoint.Updated += changedAwaiterEvent.EventHandler;
            var changedAwaiter = changedAwaiterEvent.WaitForEventAsync();

            await actionPoint.RenameAsync(newName);

            await changedAwaiter;

            // Assert
            Assert.Equal(newName, actionPoint.Data.Name);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateParent_Valid_Updates() {
        await Setup();
        await ProjectOpenThreeActionPoints();

        try {
            // Arrange
            var actionPoint = Session.Projects.First().ActionPoints!.First();
            var parentActionPoint = Session.Projects.First().ActionPoints!.Last();

            // Act
            var changedAwaiterEvent = new EventAwaiter();
            actionPoint.Updated += changedAwaiterEvent.EventHandler;
            var changedAwaiter = changedAwaiterEvent.WaitForEventAsync();

            await actionPoint.UpdateParentAsync(parentActionPoint);

            await changedAwaiter;

            // Assert
            Assert.Equal(parentActionPoint.Id, actionPoint.Data.Parent);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task ClearParent_Valid_Clears() {
        await Setup();
        await ProjectOpenThreeActionPoints();

        try {
            // Arrange
            var actionPoint = Session.Projects.First().ActionPoints!.First();
            var parentActionPoint = Session.Projects.First().ActionPoints!.Last();
            await actionPoint.UpdateParentAsync(parentActionPoint);

            // Act
            var changedAwaiterEvent = new EventAwaiter();
            actionPoint.Updated += changedAwaiterEvent.EventHandler;
            var changedAwaiter = changedAwaiterEvent.WaitForEventAsync();

            await actionPoint.ClearParentAsync();

            await changedAwaiter;

            // Assert
            Assert.True(string.IsNullOrEmpty(actionPoint.Data.Parent));
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task Duplicate_Valid_Duplicates() {
        await Setup();
        await ProjectOpenThreeActionPoints();

        try {
            // Arrange
            var actionPoint = Session.Projects.First().ActionPoints!.First();

            // Act
            var addAwaiter = Session.Projects.First().ActionPoints!
                .CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add).WaitForEventAsync();
            await actionPoint.DuplicateAsync(new Position(999, 999, 999));
            await addAwaiter;

            // Assert
            Assert.Equal(4, Session.Projects.First().ActionPoints!.Count);
            var duplicatedActionPoint = Session.Projects.First().ActionPoints!.Last();
            Assert.Equal(999, duplicatedActionPoint.Data.Position.X);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task AddJointsUsingRobot_Valid_AddsJoints() {
        await Setup();
        await ProjectOpenObjectThreeActionPoints("DobotMagician");
        var project = Session.Projects.First();

        try {
            // Arrange
            var actionPoint = project.ActionPoints!.First();
            var robot = project.Scene.ActionObjects!.First();
            await project.Scene.StartAsync();
            await project.Scene.GetStartedAwaiter().WaitForEventAsync();

            var addAwaiter = actionPoint.Joints.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            // Act
            var record = await Record.ExceptionAsync(() => actionPoint.AddJointsUsingRobotAsync(robot));
            await addAwaiter;

            // Assert
            Assert.Null(record);
            Assert.NotNull(actionPoint.Joints);
            Assert.Single(actionPoint.Joints);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateJoints_Valid_Updates() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var joints = actionPoint.Joints.First();

        try {
            // Arrange
            var newJoints = new List<Joint>(joints.Data.Joints); // Example new joint values
            newJoints[0].Value = 0.125m;

            // Act
            var updatedAwaiter = new EventAwaiter();
            joints.Updated += updatedAwaiter.EventHandler;
            var updateTask = updatedAwaiter.WaitForEventAsync();

            await joints.UpdateAsync(newJoints);

            await updateTask;

            // Assert
            Assert.Equal(0.125m, joints.Data.Joints[0].Value);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateJointsUsingRobot_Valid_Updates() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var joints = actionPoint.Joints.First();

        try {
            // Change
            var updatedAwaiterC = new EventAwaiter();
            joints.Updated += updatedAwaiterC.EventHandler;
            var updateTaskC = updatedAwaiterC.WaitForEventAsync();

            var newJoints = new List<Joint>(joints.Data.Joints);
            newJoints[0].Value = 0.125m;
            await joints.UpdateAsync(newJoints);

            await updateTaskC;

            // Act
            var updatedAwaiter = new EventAwaiter();
            joints.Updated += updatedAwaiter.EventHandler;
            var updateTask = updatedAwaiter.WaitForEventAsync();

            await joints.UpdateUsingRobotAsync();

            await updateTask;

            // Assert
            Assert.NotEqual(0.125m, joints.Data.Joints[0].Value);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task RenameJoints_Valid_UpdatesName() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var joints = actionPoint.Joints.First();
        var newName = "NewJointName";

        try {
            // Arrange
            var updatedAwaiter = new EventAwaiter();
            joints.Updated += updatedAwaiter.EventHandler;
            var updateTask = updatedAwaiter.WaitForEventAsync();

            // Act
            await joints.RenameAsync(newName);

            await updateTask;

            // Assert
            Assert.Equal(newName, joints.Data.Name);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task RemoveJoints_Valid_Removes() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var joints = actionPoint.Joints.First();

        try {
            // Arrange
            var removeAwaiter = actionPoint.Joints.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();

            // Act
            await joints.RemoveAsync();

            await removeAwaiter;

            // Assert
            Assert.Empty(actionPoint.Joints);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task AddOrientation_Valid_AddsOrientation() {
        await Setup();
        await ProjectOpenThreeActionPoints();

        try {
            // Arrange
            var actionPoint = Session.Projects.First().ActionPoints!.First();
            var orientation = new Orientation(0.7071067811865475m, 0, 0, 0.7071067811865475m);

            // Act
            var addAwaiter = actionPoint.Orientations.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            var record = await Record.ExceptionAsync(() => actionPoint.AddOrientationAsync(orientation, "Orient"));
            await addAwaiter;

            // Assert
            Assert.Null(record);
            Assert.Single(actionPoint.Orientations);
            Assert.True(orientation.EqualTo(actionPoint.Orientations.First().Data.Orientation, 0.001m));
            Assert.Equal("Orient", actionPoint.Orientations.First().Data.Name);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task AddOrientationUsingRobot_Valid_AddsOrientation() {
        await Setup();
        await ProjectOpenObjectThreeActionPoints("DobotMagician");
        var project = Session.Projects.First();

        try {
            // Arrange
            var actionPoint = project.ActionPoints!.First();
            var robot = project.Scene.ActionObjects!.First();
            await project.Scene.StartAsync();
            await project.Scene.GetStartedAwaiter().WaitForEventAsync();

            // Act
            var addAwaiter = actionPoint.Orientations.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            // Act
            var record = await Record.ExceptionAsync(() => actionPoint.AddOrientationUsingRobotAsync(robot));
            await addAwaiter;

            // Assert
            Assert.Null(record);
            Assert.NotNull(actionPoint.Orientations);
            Assert.Single(actionPoint.Orientations);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateOrientation_Valid_Updates() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var orientation = actionPoint.Orientations.First();

        try {
            // Arrange
            var newOrientation = new Orientation(0.7071067811865475m, 0, 0, 0.7071067811865475m);

            // Act
            var updatedAwaiter = new EventAwaiter();
            orientation.Updated += updatedAwaiter.EventHandler;
            var updateTask = updatedAwaiter.WaitForEventAsync();

            await orientation.UpdateAsync(newOrientation);

            await updateTask;

            // Assert
            Assert.True(newOrientation.EqualTo(orientation.Data.Orientation, 0.001m));
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task UpdateOrientationUsingRobot_Valid_Updates() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var orientation = actionPoint.Orientations.First();
        var robot = project.Scene.ActionObjects!.First();

        try {
            // Change
            var updatedAwaiterC = new EventAwaiter();
            orientation.Updated += updatedAwaiterC.EventHandler;
            var updateTaskC = updatedAwaiterC.WaitForEventAsync();

            var newOrientation = new Orientation(0.7071067811865475m, 0, 0, 0.7071067811865475m);
            await orientation.UpdateAsync(newOrientation);

            await updateTaskC;

            // Act
            var updatedAwaiter = new EventAwaiter();
            orientation.Updated += updatedAwaiter.EventHandler;
            var updateTask = updatedAwaiter.WaitForEventAsync();

            await orientation.UpdateUsingRobotAsync(robot, "default");

            await updateTask;

            // Assert
            Assert.NotEqual(newOrientation, orientation.Data.Orientation);
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task RenameOrientation_Valid_UpdatesName() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var orientation = actionPoint.Orientations.First();
        var newName = "NewOrientationName";

        try {
            // Arrange
            var updatedAwaiter = new EventAwaiter();
            orientation.Updated += updatedAwaiter.EventHandler;
            var updateTask = updatedAwaiter.WaitForEventAsync();

            // Act
            await orientation.RenameAsync(newName);

            await updateTask;

            // Assert
            Assert.Equal(newName, orientation.Data.Name);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task RemoveOrientation_Valid_Removes() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var orientation = actionPoint.Orientations.First();

        try {
            // Arrange
            var removeAwaiter = actionPoint.Orientations
                .CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();

            // Act
            await orientation.RemoveAsync();

            await removeAwaiter;

            // Assert
            Assert.Empty(actionPoint.Orientations);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task AddAction_Valid_AddsAction() {
        await Setup();
        await ProjectOpenObjectThreeActionPoints();

        try {
            // Arrange
            var actionPoint = Session.Projects.First().ActionPoints!.First();
            var robot = Session.Projects.First().Scene.ActionObjects!.First();

            // Act
            var addAwaiter = actionPoint.Actions!.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            var record = await Record.ExceptionAsync(() => actionPoint.AddActionAsync(
                "home",
                robot,
                robot.ObjectType.Data.Actions.First(a => a.Name == "home"),
                new List<ActionParameter>()));

            // Assert
            Assert.Null(record);
            await addAwaiter;
            Assert.Single(actionPoint.Actions!);
            Assert.Equal("home", actionPoint.Actions!.First().Data.Name);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task RenameAction_Valid_UpdatesName() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var action = actionPoint.Actions.First();
        var newName = "NewActionName";

        try {
            // Arrange
            var updatedAwaiter = new EventAwaiter();
            action.Updated += updatedAwaiter.EventHandler;
            var updateTask = updatedAwaiter.WaitForEventAsync();

            // Act
            await action.RenameAsync(newName);

            await updateTask;

            // Assert
            Assert.Equal(newName, action.Data.Name);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task RemoveAction_Valid_Removes() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var action = actionPoint.Actions.First();

        try {
            // Arrange
            var removeAwaiter = actionPoint.Actions.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();

            // Act
            await action.RemoveAsync();

            await removeAwaiter;

            // Assert
            Assert.Empty(actionPoint.Actions);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task ExecuteAction_Valid_RaisesEvents() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var action = actionPoint.Actions.First();

        try {
            // Arrange
            var executingAwaiter = new EventAwaiter();
            var executedAwaiter = new EventAwaiter();
            action.Executing += executingAwaiter.EventHandler;
            action.Executed += executedAwaiter.EventHandler;

            // Act
            await action.ExecuteAsync();

            // Assert
            await executingAwaiter.WaitForEventAsync(); // Ensure Executing event is raised
            await executedAwaiter.WaitForEventAsync(); // Ensure Executed event is raised
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }

    [Fact]
    public async Task ExecuteActionAndCancel_NotCancellable_RaisesEventAndFails() {
        await Setup();
        await ProjectStartedObjectActionPointWithEntities("DobotMagician");
        var project = Session.Projects.First();
        var actionPoint = project.ActionPoints!.First();
        var action = actionPoint.Actions.First();

        try {
            // Arrange
            var executingAwaiter = new EventAwaiter();
            var executedAwaiter = new EventAwaiter();
            action.Executing += executingAwaiter.EventHandler;
            action.Executed += executedAwaiter.EventHandler;

            // Act
            await action.ExecuteAsync();

            await Assert.ThrowsAsync<Arcor2Exception>(action.CancelAsync);

            // Assert
            await executingAwaiter.WaitForEventAsync();
            await executedAwaiter.WaitForEventAsync();
        }
        finally {
            await DisposeProjectStarted();
            await Teardown();
        }
    }
}