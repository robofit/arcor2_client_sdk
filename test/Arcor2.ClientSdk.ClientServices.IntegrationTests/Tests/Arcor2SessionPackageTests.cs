using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Fixtures;
using Arcor2.ClientSdk.ClientServices.IntegrationTests.Helpers;
using System.Collections.Specialized;
using Xunit.Abstractions;

namespace Arcor2.ClientSdk.ClientServices.IntegrationTests.Tests;

public class Arcor2SessionPackageTests(Arcor2ServerFixture fixture, ITestOutputHelper output) : TestBase(fixture, output) {
    [Fact]
    public async Task BuildIntoPackage_Valid_BuildsPackage() {
        await Setup();
        await ProjectClosedObjectValidProgram();
        var project = Session.Projects.First();

        try {
            var addAwaiter = Session.Packages.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            await project.BuildIntoPackageAsync("TestPackage");
            await addAwaiter;

            // Assert
            Assert.Single(Session.Packages);
            var package = Session.Packages.First();
            Assert.Equal(project, package.Project);
            Assert.Equal("TestPackage", package.Data.Name);
            Assert.NotEqual(NavigationState.Package, Session.NavigationState); // Should not open it
        }
        finally {
            await Session.Packages.First().RemoveAsync();
            await DisposeProjectClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task BuildIntoTemporaryPackageAndRunAndGoBack_BreakPointPaused_BuildsAndRuns() {
        await Setup();
        // Arrange
        await ProjectStartedObjectValidProgram();
        var project = Session.Projects.First();
        var breakpoints = Session.Projects.First().ActionPoints!.Select(a => a.Id).ToList();
        await project.Scene.StopAsync();
        await project.Scene.GetStoppedAwaiter().WaitForEventAsync();
        await project.SaveAsync();

        try {
            // Act
            var navAwaiter = GetNavigationAwaiter(n => n.State == NavigationState.Package).WaitForEventAsync();
            var addAwaiter = Session.Packages.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Add)
                .WaitForEventAsync();
            await project.BuildIntoTemporaryPackageAndRunAsync(breakpoints);

            // Does not matter which comes first.
            await Task.WhenAll(navAwaiter, addAwaiter);

            // Assert
            Assert.Single(Session.Packages);
            var package = Session.Packages.First();
            Assert.Equal(project, package.Project);
            Assert.Equal(NavigationState.Package, Session.NavigationState);
            Assert.Equal(package.Id, Session.NavigationId);

            // Act
            var backNavAwaiter = GetNavigationAwaiter(n => n.State == NavigationState.Project).WaitForEventAsync();
            var removeAwaiter = Session.Packages.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await package.StopAsync();

            await Task.WhenAll(backNavAwaiter, removeAwaiter);

            // Assert
            Assert.Empty(Session.Packages);
            Assert.Equal(NavigationState.Project, Session.NavigationState);
            Assert.Equal(project.Id, Session.NavigationId);
        }
        finally {
            await DisposeProjectOpen();
            await Teardown();
        }
    }

    [Fact]
    public async Task RenamePackage_Valid_UpdatesName() {
        await Setup();
        await PackageClosedObjectValidProgram();
        // Arrange
        var package = Session.Packages.First();
        var newName = "RenamedPackage";

        try {
            // Act
            var updatedAwaiter = new EventAwaiter();
            package.PropertyChanged += updatedAwaiter.EventHandler;
            var updateTask = updatedAwaiter.WaitForEventAsync();

            await package.RenameAsync(newName);

            await updateTask;

            // Assert
            Assert.Equal(newName, package.Data.Name);
        }
        finally {
            await DisposePackageClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task RemovePackage_Valid_Removes() {
        await Setup();
        await PackageClosedObjectValidProgram();
        // Arrange
        var package = Session.Packages.First();

        try {
            // Act
            var removeAwaiter = Session.Packages.CreateCollectionChangedAwaiter(NotifyCollectionChangedAction.Remove)
                .WaitForEventAsync();
            await package.RemoveAsync();

            await removeAwaiter;

            // Assert
            Assert.Empty(Session.Packages);
        }
        finally {
            await DisposePackageClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task RunPackage_Valid_Runs() {
        await Setup();
        await PackageClosedObjectValidProgram();
        // Arrange
        var package = Session.Packages.First();

        try {
            // Act
            var navAwaiter = GetNavigationAwaiter(n => n.State == NavigationState.Package).WaitForEventAsync();
            await package.RunAsync();
            await package.GetPackageStateAwaiter(PackageState.Running).WaitForEventAsync();

            await navAwaiter;

            // Assert
            Assert.Equal(NavigationState.Package, Session.NavigationState);
            Assert.Equal(package.Id, Session.NavigationId);
            Assert.True(package.IsOpen);
        }
        finally {
            await package.StopAsync();
            await package.GetPackageStateAwaiter(PackageState.Stopped).WaitForEventAsync();
            await package.RemoveAsync();
            await DisposePackageClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task StopPackage_Valid_Stops() {
        await Setup();
        await PackageClosedObjectValidProgram();
        // Arrange
        var package = Session.Packages.First();
        await package.RunAsync();
        await package.GetPackageStateAwaiter(PackageState.Running).WaitForEventAsync();

        try {
            // Act
            var backNavAwaiter = GetNavigationAwaiter(n => n.State == NavigationState.MenuListOfPackages)
                .WaitForEventAsync();
            await package.StopAsync();
            await package.GetPackageStateAwaiter(PackageState.Stopped).WaitForEventAsync();

            await backNavAwaiter;

            // Assert
            Assert.Single(Session.Packages);
            Assert.Equal(NavigationState.MenuListOfPackages, Session.NavigationState);
            Assert.False(package.IsOpen);
        }
        finally {
            await DisposePackageClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task PauseAndResumePackage_Valid_TransitionsState() {
        await Setup();
        await PackageClosedObjectValidProgram();
        var package = Session.Packages.First();

        try {
            // Do not test transitional states, not really guaranteed to happen?
            var runAwaiter = package.GetPackageStateAwaiter(PackageState.Running).WaitForEventAsync();
            await package.RunAsync(false);
            await runAwaiter;
            Assert.Equal(PackageState.Running, package.State);

            var pauseAwaiter = package.GetPackageStateAwaiter(PackageState.Paused).WaitForEventAsync();
            await package.PauseAsync();
            await pauseAwaiter;
            Assert.Equal(PackageState.Paused, package.State);

            var runAwaiter2 = package.GetPackageStateAwaiter(PackageState.Running).WaitForEventAsync();
            await package.ResumeAsync();
            await runAwaiter2;
            Assert.Equal(PackageState.Running, package.State);

            var stopAwaiter = package.GetPackageStateAwaiter(PackageState.Stopped).WaitForEventAsync();
            await package.StopAsync();
            await stopAwaiter;
            Assert.Equal(PackageState.Stopped, package.State);
        }
        finally {
            await package.RemoveAsync();
            await DisposePackageClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task PausePackage_InvalidBreakpoint_RaisesExceptionOccurred() {
        await Setup();
        await PackageClosedObjectValidProgram();
        // Arrange
        var package = Session.Packages.First();
        var invalidBreakpointId = "invalidIDaaa";
        var exceptionAwaiter = new EventAwaiter();
        package.ExceptionOccured += exceptionAwaiter.EventHandler;
        var exceptionTask = exceptionAwaiter.WaitForEventAsync();
        var stopTask = package.GetPackageStateAwaiter(PackageState.Stopped).WaitForEventAsync();

        try {
            // Act
            await package.RunAsync([invalidBreakpointId], false);

            // Assert
            await Task.WhenAll(exceptionTask, stopTask);
        }
        finally {
            await package.RemoveAsync();
            await DisposePackageClosed();
            await Teardown();
        }
    }

    [Fact]
    public async Task StepPackage_Valid_TransitionsStateAndRaisesEvents() {
        await Setup();
        await PackageClosedObjectValidProgram();
        var package = Session.Packages.First();
        var action = package.Project.ActionPoints!.First().Actions.First();
        var actionStartingAwaiter = new EventAwaiter();
        var actionFinishedAwaiter = new EventAwaiter();
        action.Starting += actionStartingAwaiter.EventHandler;
        action.Finished += actionFinishedAwaiter.EventHandler;

        try {
            var stateBeforeTask = actionStartingAwaiter.WaitForEventAsync();
            var startTask = package.GetPackageStateAwaiter(PackageState.Paused).WaitForEventAsync();
            await package.RunAsync();
            await startTask;
            await stateBeforeTask;

            var runTask = package.GetPackageStateAwaiter(PackageState.Running).WaitForEventAsync();
            var pauseTask = package.GetPackageStateAwaiter(PackageState.Paused).WaitForEventAsync();
            var stateAfterTask = actionFinishedAwaiter.WaitForEventAsync();
            await package.StepAsync();
            await Task.WhenAll(stateAfterTask, runTask, pauseTask);
        }
        finally {
            await package.StopAsync();
            await package.GetPackageStateAwaiter(PackageState.Stopped).WaitForEventAsync();
            await package.RemoveAsync();
            await DisposePackageClosed();
            await Teardown();
        }
    }
}