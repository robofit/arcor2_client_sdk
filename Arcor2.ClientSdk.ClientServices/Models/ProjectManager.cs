using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// Manages lifetime of a project.
    /// </summary>
    public class ProjectManager : Arcor2ObjectManager {

        /// <summary>
        /// The metadata of the project.
        /// </summary>
        public BareProject Meta { get; set; }

        /// <summary>
        /// The project parameters.
        /// </summary>
        public IList<ProjectParameterManager>? Parameters { get; set; }

        /// <summary>
        /// Gets if the project is open.
        /// </summary>
        /// <returns> <c>true</c> if this project is open, <c>false</c> otherwise.</returns>
        public bool IsOpen => Session.NavigationState == NavigationState.Project && Session.NavigationId == Id;

        /// <summary>
        /// Gets the parent scene.
        /// </summary>
        public SceneManager ParentScene => Session.Scenes.First(s => s.Id == Meta.SceneId);

        /// <summary>
        /// Raised when project is saved by the server.
        /// </summary>
        public EventHandler? OnSaved;

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="meta">Project meta object.</param>
        internal ProjectManager(Arcor2Session session, BareProject meta) : base(session, meta.Id) {
            Meta = meta;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="project">Project object.</param>
        internal ProjectManager(Arcor2Session session, Project project) : base(session, project.Id) {
            Meta = project.MapToBareProject();
            Parameters = project.Parameters
                .Select(p => new ProjectParameterManager(Session, this, p)).ToList();
            // TODO: Rest
        }

        /// <summary>
        /// Renames the project.
        /// </summary>
        /// <param name="newName">New name for the project.</param>
        /// <exception cref="Arcor2Exception" />
        public async Task RenameAsync(string newName) {
            await LockAsync();
            var response = await Session.client.RenameProjectAsync(new RenameProjectRequestArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming project {Id} failed.", response.Messages);
            }
            // Unlocked by the server.
        }

        /// <summary>
        /// Opens the project.
        /// </summary>
        /// <remarks>
        /// The session must be in a menu.
        ///
        /// For extra caution, make sure that the project is actually opened by invoking <see cref="IsOpen"/>
        /// or checking if the <see cref="Arcor2Session.NavigationState"/> is in the <see cref="NavigationState.Project"/> state with corresponding project ID.
        /// 
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task OpenAsync() {
            var response = await Session.client.OpenProjectAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Opening project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Closes the project.
        /// </summary>
        /// <remarks>
        /// Project must be open on invocation. Will fail with unsaved changes and if unforced.
        /// </remarks>
        /// <param name="force">If true, the project will be closed even with unsaved changes, etc... </param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CloseAsync(bool force = false) {
            var response = await Session.client.CloseProjectAsync(new CloseProjectRequestArgs(force));
            if(!response.Result) {
                throw new Arcor2Exception($"Closing project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Saves the project.
        /// </summary>
        /// <remarks>
        /// Project must be open on invocation.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SaveAsync() {
            var response = await Session.client.SaveProjectAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Saving project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Deletes the project.
        /// </summary>
        /// <remarks>
        /// The project must be closed.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.client.RemoveProjectAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Updates the description of the project.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateDescriptionAsync(string newDescription) {
            var response = await Session.client.UpdateProjectDescriptionAsync(new UpdateProjectDescriptionRequestArgs(Id, newDescription));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating description of project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Starts the project.
        /// </summary>
        /// <remarks>
        /// The project must be opened, in the stopped state, and all locks freed.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StartAsync() {
            var response = await Session.client.StartSceneAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Starting project {Id} (scene {Meta.SceneId}) failed.", response.Messages);
            }
        }

        /// <summary>
        /// Stops the project.
        /// </summary>
        /// <remarks>
        /// The project must be opened, in the started state, and without running actions.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StopAsync() {
            var response = await Session.client.StopSceneAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Stopping of project {Id} (scene {Meta.SceneId}) failed.", response.Messages);
            }
        }

        /// <summary>
        /// Loads the project fully without opening it.
        /// </summary>
        public async Task LoadAsync() {
            var response = await Session.client.GetProjectAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Loading project {Id} failed.", response.Messages);
            }
            UpdateAccordingToNewObject(response.Data);
        }

        /// <summary>
        /// Duplicates the project.
        /// </summary>
        public async Task DuplicateAsync(string newName) {
            var response = await Session.client.DuplicateProjectAsync(new CopyProjectRequestArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Duplicating project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Sets if the project should contain logic.
        /// </summary>
        public async Task SetHasLogicAsync(bool hasLogic) {
            var response = await Session.client.UpdateProjectHasLogicAsync(new UpdateProjectHasLogicRequestArgs(Id, hasLogic));
            if(!response.Result) {
                throw new Arcor2Exception($"Setting HasLogic for project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Builds the project into package.
        /// </summary>
        public async Task BuildIntoPackageAsync(string packageName) {
            var response = await Session.client.BuildProjectAsync(new BuildProjectRequestArgs(Id, packageName));
            if(!response.Result) {
                throw new Arcor2Exception($"Building project {Id} into package failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new project parameter.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddProjectParameter(string name, string type, string value) {
            var response = await Session.client.AddProjectParameterAsync(new AddProjectParameterRequestArgs(name, type, value));
            if(!response.Result) {
                throw new Arcor2Exception($"Adding project parameter for project {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Updates the project according to the <paramref name="project"/> instance.
        /// </summary>
        /// <param name="project">Newer version of the project.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(Project project) {
            if(Id != project.Id) {
                throw new InvalidOperationException($"Can't update a ProjectManager ({Id}) using a project data object ({project.Id}) with different ID.");
            }

            Meta = project.MapToBareProject();
            Parameters = Parameters.UpdateListOfArcor2Objects(project.Parameters,
                p => p.Id,
                (m, p) => m.UpdateAccordingToNewObject(p),
                p => new ProjectParameterManager(Session, this, p));
        }

        /// <summary>
        /// Updates the project according to the <paramref name="project"/> instance.
        /// </summary>
        /// <param name="project">Newer version of the project.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(BareProject project) {
            if(Id != project.Id) {
                throw new InvalidOperationException($"Can't update a ProjectManager ({Id}) using a project data object ({project.Id}) with different ID.");
            }

            Meta = project;
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.client.OnProjectSaved += OnSaved;
            Session.client.OnProjectRemoved += OnProjectRemoved;
            Session.client.OnProjectBaseUpdated += OnProjectBaseUpdated;
            Session.client.OnProjectParameterAdded += OnProjectParameterAdded;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.OnProjectSaved -= OnSaved;
            Session.client.OnProjectRemoved -= OnProjectRemoved;
            Session.client.OnProjectBaseUpdated -= OnProjectBaseUpdated;
            Session.client.OnProjectParameterAdded -= OnProjectParameterAdded;
        }

        private void OnProjectBaseUpdated(object sender, BareProjectEventArgs e) {
            if(e.Project.Id == Id) {
                Meta = e.Project;
            }
        }

        private void OnProjectRemoved(object sender, BareProjectEventArgs e) {
            if(e.Project.Id == Id) {
                Session.Projects.Remove(this);
                Dispose();
            }
        }

        private void OnProjectParameterAdded(object sender, ProjectParameterEventArgs e) {
            if (Parameters == null) {
                Session.logger?.LogError($"When adding a new project parameter, the parameters collection for project {Id} was null.");
            }

            Parameters?.Add(new ProjectParameterManager(Session, this, e.ProjectParameter));
        }
    }
}
