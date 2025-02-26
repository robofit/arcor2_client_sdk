using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// Manages lifetime of a package.
    /// </summary>
    public class PackageManager : LockableArcor2ObjectManager<PackageMeta> {
        /// <summary>
        /// The parent project ID.
        /// </summary>
        public string ProjectId { get; }

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
        /// Initializes a new instance of <see cref="PackageManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="package">Package object.</param>
        internal PackageManager(Arcor2Session session, PackageSummary package) : base(session, package.PackageMeta,
            package.Id) {
            ProjectId = package.ProjectMeta.Id;
        }

        /// <summary>
        /// Removes the package.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.client.RemovePackageAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing joints {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Renames the package.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RenameAsync(string newName) {
            var response = await Session.client.RenamePackageAsync(new RenamePackageRequestArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming package {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Runs the package.
        /// </summary>
        /// <param name="startPaused">Should the package start paused?</param>
        /// <param name="breakPoints">A list of breakpoints</param>
        /// TODO: Are thy just action IDs? Confirm
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RunAsync(bool startPaused, List<string>? breakPoints = null) {
            var response = await Session.client.RunPackageAsync(new RunPackageRequestArgs(Id, startPaused, breakPoints ?? new List<string>()));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming package {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Stops the package.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StopAsync() {
            var response = await Session.client.StopPackageAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Stopping package {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Pauses the package.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task PauseAsync() {
            var response = await Session.client.PausePackageAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Pausing package {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Resumes the package.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task ResumeAsync() {
            var response = await Session.client.ResumePackageAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Resuming package {Id} failed.", response.Messages);
            }
        }

        internal void UpdateAccordingToNewObject(PackageSummary package) {
            if(Id != package.Id) {
                throw new InvalidOperationException(
                    $"Can't update an PackageManager ({Id}) using a package data object ({package.Id}) with different ID.");
            }

            UpdateData(package.PackageMeta);
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.client.OnPackageUpdated += OnPackageUpdated;
            Session.client.OnPackageRemoved += OnPackageRemoved;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.OnPackageUpdated -= OnPackageUpdated;
            Session.client.OnPackageRemoved -= OnPackageRemoved;
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
    }
}
