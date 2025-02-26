using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Models.Extras;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models
{
    /// <summary>
    /// Represents a group of overrides of action object parameters for a project.
    /// </summary>
    public class ProjectOverrideManager : Arcor2ObjectManager<ProjectOverride>
    {

        /// <summary>
        /// The parent project.
        /// </summary>
        internal ProjectManager Project;

        /// <summary>
        /// The action object being overriden.
        /// </summary>
        public ActionObjectManager ActionObject => Project.Scene.ActionObjects!.First(a => a.Id == Data.ActionObjectId);

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectOverrideManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="project">The parent project.</param>
        /// <param name="actionObjectId">The ID of overriden action object.</param>
        /// <param name="parameter">An overriden parameters.</param>
        internal ProjectOverrideManager(Arcor2Session session, ProjectManager project, string actionObjectId, Parameter parameter) : base(session, new ProjectOverride(actionObjectId, parameter))
        {
            Project = project;
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
        public async Task UpdateAsync(Parameter parameter)
        {
            await LockAsync(Data.ActionObjectId);
            var response = await Session.client.UpdateOverrideAsync(new UpdateOverrideRequestArgs(Data.ActionObjectId, parameter));
            if (!response.Result)
            {
                throw new Arcor2Exception($"Updating project override {parameter.Name} for action object {Data.ActionObjectId} in project {Project.Id} failed.", response.Messages);
            }
            await UnlockAsync(Data.ActionObjectId);
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
        public async Task RemoveAsync()
        {
            await LockAsync(Data.ActionObjectId);
            var response = await Session.client.RemoveOverrideAsync(new DeleteOverrideRequestArgs(Data.ActionObjectId, Data.Parameter));
            if (!response.Result)
            {
                throw new Arcor2Exception($"Deleting project override {Data.Parameter.Name} for action object {Data.ActionObjectId} in project {Project.Id} failed.", response.Messages);
            }
            await UnlockAsync(Data.ActionObjectId);
        }

        internal void UpdateAccordingToNewObject(Parameter parameter)
        {
            Data.Parameter = parameter;
            OnUpdated();
        }

        protected override void RegisterHandlers()
        {
            base.RegisterHandlers();
            Session.client.OnOverrideUpdated += OnOverrideUpdated;
            Session.client.OnOverrideRemoved += OnOverrideRemoved;
        }

        protected override void UnregisterHandlers()
        {
            base.UnregisterHandlers();
            Session.client.OnOverrideUpdated -= OnOverrideUpdated;
            Session.client.OnOverrideRemoved -= OnOverrideRemoved;
        }

        private void OnOverrideUpdated(object sender, ParameterEventArgs e)
        {
            if (Project.IsOpen)
            {
                if (e.ParentId == Data.ActionObjectId)
                {
                    Data.Parameter = e.Parameter;
                    OnUpdated();
                }
            }
        }

        private void OnOverrideRemoved(object sender, ParameterEventArgs e)
        {
            if (Project.IsOpen)
            {
                if (e.ParentId == Data.ActionObjectId)
                {
                    RemoveData();
                    Project.Overrides!.Remove(this);
                    Dispose();
                }
            }
        }
    }
}
