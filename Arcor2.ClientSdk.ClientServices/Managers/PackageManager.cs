using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using PackageState = Arcor2.ClientSdk.ClientServices.Enums.PackageState;
using PackageExceptionEventArgs = Arcor2.ClientSdk.ClientServices.EventArguments.PackageExceptionEventArgs;
using PackageStateEventArgs = Arcor2.ClientSdk.ClientServices.EventArguments.PackageStateEventArgs;

namespace Arcor2.ClientSdk.ClientServices.Managers {
    /// <summary>
    /// Manages lifetime of a package.
    /// </summary>
    public class PackageManager : LockableArcor2ObjectManager<PackageMeta> {
        /// <summary>
        /// The parent project ID.
        /// </summary>
        public string ProjectId { get; }

        /// <summary>
        /// The state of the package.
        /// </summary>
        // Realistically will never be undefined if user can use it (unless returned by the server).
        public PackageState State { get; private set; } = PackageState.Undefined;

        private ProjectManager? cachedProject;

        /// <summary>
        /// The parent project.
        /// </summary>
        public ProjectManager Project => cachedProject ??= Session.Projects.First(p => p.Id == ProjectId);

        /// <summary>
        /// Gets if the package is open.
        /// </summary>
        /// <returns> <c>true</c> if this package is open, <c>false</c> otherwise.</returns>
        public bool IsOpen => Session.NavigationState == NavigationState.Package && Session.NavigationId == Id;

        /// <summary>
        /// Raised when the state of the package changes.
        /// </summary>
        public event EventHandler<PackageStateEventArgs>? StateChanged;

        /// <summary>
        /// Raised when exception occurs (e.g., invalid breakpoint name).
        /// </summary>
        public event EventHandler<PackageExceptionEventArgs>? ExceptionOccured;

        /// <summary>
        /// Initializes a new instance of <see cref="PackageManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="package">Package object.</param>
        internal PackageManager(Arcor2Session session, PackageSummary package) : base(session, package.PackageMeta,
            package.Id) {
            ProjectId = package.ProjectMeta.Id;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PackageManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="package">Package object.</param>
        /// <param name="state">The package state.</param>
        internal PackageManager(Arcor2Session session, PackageInfoData package, PackageStateData? state = null) : base(session, package.MapToPackageMeta(),
            package.PackageId) {
            ProjectId = package.Project.Id;
            State = state?.State?.MapToCustomPackageModeEnum() ?? PackageState.Undefined;
        }

        /// <summary>
        /// Removes the package.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.Client.RemovePackageAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing joints {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Renames the package.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RenameAsync(string newName) {
            var response = await Session.Client.RenamePackageAsync(new RenamePackageRequestArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming package {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Runs the package.
        /// </summary>
        /// <remarks>
        /// The session must be in the menu. Invalid breakpoint IDs will not throw an exception, but rather lead to <see cref="ExceptionOccured"/> event and closing of the package.
        /// </remarks>
        /// <param name="startPaused">Should the package start paused? By default, "true".</param>
        /// <param name="breakPoints">A list of breakpoints (action point IDs). The package will pause before executing them.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RunAsync(List<string> breakPoints, bool startPaused) {
            var response = await Session.Client.RunPackageAsync(new RunPackageRequestArgs(Id, startPaused, breakPoints));
            if(!response.Result) {
                throw new Arcor2Exception($"Running package {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Runs the package.
        /// </summary>
        /// <remarks>
        /// The session must be in the menu.
        /// </remarks>
        /// <param name="startPaused">Should the package start paused? By default, "true".</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RunAsync(bool startPaused = true) {
            await RunAsync(new List<string>(), startPaused);
        }

        /// <summary>
        /// Runs the package.
        /// </summary>
        /// <remarks>
        /// The session must be in the menu. Invalid breakpoint IDs will not throw an exception, but rather lead to <see cref="ExceptionOccured"/> event and closing of the package.
        /// </remarks>
        /// <param name="startPaused">Should the package start paused? By default, "true".</param>
        /// <param name="breakPoints">A list of breakpoints (action points). The package will pause before executing them. Invalid IDs will lead to <see cref="ExceptionOccured"/> event.</param>>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RunAsync(List<ActionPointManager> breakPoints, bool startPaused = true) {
            var breakPointIds = breakPoints.Select(a => a.Id).ToList();
            await RunAsync(breakPointIds, startPaused);
        }

        /// <summary>
        /// Stops and closes the package.
        /// </summary>
        /// <remarks>
        /// The package must be opened and in the <see cref="PackageState.Running"/> state.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StopAsync() {
            var response = await Session.Client.StopPackageAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Stopping package {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Pauses the package.
        /// </summary>
        /// <remarks>
        /// The package must be opened and in the <see cref="PackageState.Running"/> state.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task PauseAsync() {
            var response = await Session.Client.PausePackageAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Pausing package {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Resumes the package.
        /// </summary>
        /// <remarks>
        /// The package must be opened and in the <see cref="PackageState.Paused"/> state.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task ResumeAsync() {
            var response = await Session.Client.ResumePackageAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Resuming package {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Resumes the execution, executes the next action, and pauses it again.
        /// </summary>
        /// <remarks>
        /// The package must be opened and in the <see cref="PackageState.Paused"/> state.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StepAsync() {
            var response = await Session.Client.StepActionAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Stepping an action for package {Id} failed.", response.Messages);
            }
        }


        internal void UpdateAccordingToNewObject(PackageSummary package) {
            if(Id != package.Id) {
                throw new InvalidOperationException(
                    $"Can't update an PackageManager ({Id}) using a package data object ({package.Id}) with different ID.");
            }

            UpdateData(package.PackageMeta);
        }

        internal void UpdateAccordingToNewObject(PackageInfoData package) {
            if(Id != package.PackageId) {
                throw new InvalidOperationException(
                    $"Can't update an PackageManager ({Id}) using a package data object ({package.PackageId}) with different ID.");
            }

            if (Data.Name != package.PackageName) {
                Data.Name = package.PackageName;
                OnUpdated();
            }
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.Client.PackageUpdated += OnPackageUpdated;
            Session.Client.PackageRemoved += OnPackageRemoved;
            Session.Client.PackageState += OnPackageState;
            Session.Client.PackageException += OnPackageException;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.Client.PackageUpdated -= OnPackageUpdated;
            Session.Client.PackageRemoved -= OnPackageRemoved;
            Session.Client.PackageState -= OnPackageState;
            Session.Client.PackageException -= OnPackageException;
        }

        private void OnPackageRemoved(object sender, PackageChangedEventArgs e) {
            if (e.Data.Id == Id) {
                RemoveData();
                Session.Packages.Remove(this);
                Dispose();
            }
        }

        private void OnPackageUpdated(object sender, PackageChangedEventArgs e) {
            if (e.Data.Id == Id) {
                UpdateData(e.Data.PackageMeta);
            }
        }

        private void OnPackageState(object sender, Communication.PackageStateEventArgs e) {
            if(e.Data.PackageId == Id) {
                State = e.Data.State?.MapToCustomPackageModeEnum() ?? PackageState.Undefined;
                StateChanged?.Invoke(this, new PackageStateEventArgs(State));
            }
        }
        private void OnPackageException(object sender, Communication.PackageExceptionEventArgs e) {
            ExceptionOccured?.Invoke(this, new PackageExceptionEventArgs(e.Data.Type, e.Data.Message, e.Data.Handled));
        }
    }
}
