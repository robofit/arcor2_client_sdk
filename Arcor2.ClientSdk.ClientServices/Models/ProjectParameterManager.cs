using System;
using System.Threading.Tasks;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    public class ProjectParameterManager : LockableArcor2ObjectManager<ProjectParameter> {
        /// <summary>
        /// The parent project.
        /// </summary>
        internal ProjectManager Project { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectParameterManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="project">The parent project.</param>
        /// <param name="parameter">The project parameter.</param>
        internal ProjectParameterManager(Arcor2Session session, ProjectManager project, ProjectParameter parameter) : base(session, parameter, parameter.Id) {
            Project = project;
        }

        /// <summary>
        /// Updates the project parameter value.
        /// </summary>
        /// <param name="value">The new value</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateValueAsync(string value) {
            await LockAsync();
            var response = await Session.client.UpdateProjectParameterAsync(new UpdateProjectParameterRequestArgs(Id, Data.Name, value));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing project parameter {Id} failed.", response.Messages);
            }
            // Unlocked automatically by server
        }


        /// <summary>
        /// Updates the project parameter name.
        /// </summary>
        /// <param name="name">The new name.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateNameAsync(string name) {
            await LockAsync();
            var response = await Session.client.UpdateProjectParameterAsync(new UpdateProjectParameterRequestArgs(Id, name, Data.Value));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing project parameter {Id} failed.", response.Messages);
            }
            // Unlocked automatically by server
        }

        /// <summary>
        /// Removes the project parameter.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.client.RemoveProjectParameterAsync(new RemoveProjectParameterRequestArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing project parameter {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Updates the parameter according to the <paramref name="parameter"/> instance.
        /// </summary>
        /// <param name="parameter">Newer version of the parameter.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(ProjectParameter parameter) {
            if(Id != parameter.Id) {
                throw new InvalidOperationException($"Can't update a ProjectParameter ({Id}) using a project parameter data object ({parameter.Id}) with different ID.");
            }
            UpdateData(parameter);
        }


        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.client.OnProjectParameterUpdated += OnProjectParameterUpdated;
            Session.client.OnProjectParameterRemoved += OnProjectParameterRemoved;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.OnProjectParameterUpdated -= OnProjectParameterUpdated;
            Session.client.OnProjectParameterRemoved -= OnProjectParameterRemoved;
        }

        private void OnProjectParameterRemoved(object sender, ProjectParameterEventArgs e) {
            if (e.ProjectParameter.Id == Id) {
                RemoveData();
                Project.Parameters!.Remove(this);
                Dispose();
            }
        }

        private void OnProjectParameterUpdated(object sender, ProjectParameterEventArgs e) {
            if (e.ProjectParameter.Id == Id) {
                UpdateData(e.ProjectParameter);
            }
        }
    }
}
