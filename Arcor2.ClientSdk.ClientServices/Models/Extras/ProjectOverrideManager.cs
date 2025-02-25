using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models.Extras {
    /// <summary>
    /// Represents a group of overrides of action object parameters for a project.
    /// </summary>
    /// <remarks>
    /// The manager deletes himself when count of overriden parameters in <see cref="Parameter"/> collection is zero.
    /// </remarks>
    public class ProjectOverrideManager : Arcor2ObjectManager {

        /// <summary>
        /// The parent project.
        /// </summary>
        internal ProjectManager Project;
        
        /// <summary>
        /// The overriden action object ID.
        /// </summary>
        public string ActionObjectId { get; }

        /// <summary>
        /// An overriden parameters.
        /// </summary>
        public Parameter Parameter { get; private set; }

        /// <summary>
        /// The action object being overriden.
        /// </summary>
        public ActionObjectManager ActionObject => Project.Scene.ActionObjects!.First(a => a.Id == ActionObjectId);

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectOverrideManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="project">The parent project.</param>
        /// <param name="actionObjectId">The ID of overriden action object.</param>
        /// <param name="parameter">An overriden parameters.</param>
        internal ProjectOverrideManager(Arcor2Session session, ProjectManager project, string actionObjectId, Parameter parameter) : base(session) {
            Project = project;
            ActionObjectId = actionObjectId;
            Parameter = parameter;
        }

        /// <summary>
        /// Updates a project override for an action object.
        /// </summary>
        /// <remarks>
        /// It is recommended to copy the parameter object from <see cref="Parameter"/> list, or by converting the action object parameter meta.
        /// </remarks>
        /// <param name="parameter">The updated parameter override of an action object.</param>
        /// <remarks>
        /// The project must be opened.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateAsync(Parameter parameter) {
            await LockAsync(ActionObjectId);
            var response = await Session.client.UpdateOverrideAsync(new UpdateOverrideRequestArgs(ActionObjectId, parameter));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating project override {parameter.Name} for action object {ActionObjectId} in project {Project.Id} failed.", response.Messages);
            }
            await UnlockAsync(ActionObjectId);
        }

        /// <summary>
        /// Deletes a project override for an action object.
        /// </summary>
        /// <remarks>
        /// It is recommended to copy the parameter object from <see cref="Parameter"/> list, or by converting the action object parameter meta.
        /// </remarks>
        /// <remarks>
        /// The project must be opened.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            await LockAsync(ActionObjectId);
            var response = await Session.client.RemoveOverrideAsync(new DeleteOverrideRequestArgs(ActionObjectId, Parameter));
            if(!response.Result) {
                throw new Arcor2Exception($"Deleting project override {Parameter.Name} for action object {ActionObjectId} in project {Project.Id} failed.", response.Messages);
            }
            await UnlockAsync(ActionObjectId);
        }

        internal void UpdateAccordingToNewObject(Parameter parameter) {
            Parameter = parameter;
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.client.OnOverrideUpdated += OnOverrideUpdated;
            Session.client.OnOverrideRemoved += OnOverrideRemoved;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.OnOverrideUpdated -= OnOverrideUpdated;
            Session.client.OnOverrideRemoved -= OnOverrideRemoved;
        }

        private void OnOverrideUpdated(object sender, ParameterEventArgs e) {
            if(Project.IsOpen) {
                if (e.ParentId == ActionObjectId) {
                    Parameter = e.Parameter;
                }
            }
        }

        private void OnOverrideRemoved(object sender, ParameterEventArgs e) {
            if(Project.IsOpen) {
                if(e.ParentId == ActionObjectId) {
                    Project.Overrides!.Remove(this);
                    Dispose();
                }
            }
        }
    }
}
