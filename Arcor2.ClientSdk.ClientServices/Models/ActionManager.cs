using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Action = Arcor2.ClientSdk.Communication.OpenApi.Models.Action;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// Manages lifetime of an action.
    /// </summary>
    public class ActionManager : LockableArcor2ObjectManager {
        /// <summary>
        /// The parent action point.
        /// </summary>
        internal ActionPointManager ActionPoint { get; }

        /// <summary>
        /// The metadata of the action.
        /// </summary>
        public BareAction Meta { get; private set; }

        /// <summary>
        /// The parameters of the action.
        /// </summary>
        public IList<ActionParameter> Parameters { get; private set; }

        /// <summary>
        /// The logic flows from the action.
        /// </summary>
        public IList<Flow> Flows { get; private set; }

        /// <summary>
        /// Gets the action definition from object type.
        /// </summary>
        public ObjectAction ActionType => ActionPoint.Project.Scene.ActionObjects!
            .FirstOrDefault(a => a.Id == Meta.Type.Split('/').First())!.ObjectType.Actions
            .FirstOrDefault(a => a.Name == Meta.Type.Split('/').Last())!;

        /// <summary>
        /// Initializes a new instance of <see cref="ActionManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="actionPoint">The parent action point.</param>
        /// <param name="actionMeta">The action metadata.</param>
        public ActionManager(Arcor2Session session, ActionPointManager actionPoint, BareAction actionMeta) : base(
            session, actionMeta.Id) {
            ActionPoint = actionPoint;
            Meta = actionMeta;
            // We shouldn't get bare version if the action already has some of these.
            Parameters = new List<ActionParameter>();
            Flows = new List<Flow>();
        }


        /// <summary>
        /// Initializes a new instance of <see cref="ActionManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="actionPoint">The parent action point.</param>
        /// <param name="action">The action data.</param>
        public ActionManager(Arcor2Session session, ActionPointManager actionPoint, Action action) : base(
            session, action.Id) {
            ActionPoint = actionPoint;
            Meta = action.MapToBareAction();
            Parameters = action.Parameters;
            Flows = action.Flows;
        }

        /// <summary>
        /// Updates the action.
        /// </summary>
        /// <param name="parameters">Updated list of parameters.</param>
        /// <param name="flows">Updated list of flows.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateAsync(List<Flow> flows, List<ActionParameter> parameters) {
            await LockAsync();
            var response = await Session.client.UpdateActionAsync(new UpdateActionRequestArgs(Id, parameters, flows));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating action {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Updates the action flows.
        /// </summary>
        /// <param name="flows">Updated list of flows.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateFlowsAsync(List<Flow> flows) {
            await UpdateAsync(flows, (Parameters as List<ActionParameter>)!);
        }

        /// <summary>
        /// Updates the action parameters.
        /// </summary>
        /// <param name="parameters">Updated list of parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateParametersAsync(List<ActionParameter> parameters) {
            await UpdateAsync((Flows as List<Flow>)!, parameters);
        }

        /// <summary>
        /// Removes the action.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.client.RemoveActionAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing action {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Renames the action.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RenameAsync(string newName) {
            await LockAsync();
            var response = await Session.client.RenameActionAsync(new RenameActionRequestArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming action point {Id} failed.", response.Messages);
            }
            // Unlocked automatically by the server
        }

        /// <summary>
        /// Updates the action according to the <paramref name="action"/> instance.
        /// </summary>
        /// <param name="action">Newer version of the action.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(Action action) {
            if (Id != action.Id) {
                throw new InvalidOperationException(
                    $"Can't update an ActionManager ({Id}) using a action data object ({action.Id}) with different ID.");
            }

            Meta = action.MapToBareAction();
            Flows = action.Flows;
            Parameters = action.Parameters;
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.client.OnActionUpdated += OnActionUpdated;
            Session.client.OnActionBaseUpdated += OnActionBaseUpdated;
            Session.client.OnActionRemoved += OnActionRemoved;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.OnActionUpdated -= OnActionUpdated;
            Session.client.OnActionBaseUpdated -= OnActionBaseUpdated;
            Session.client.OnActionRemoved -= OnActionRemoved;
        }

        private void OnActionRemoved(object sender, BareActionEventArgs e) {
            if (ActionPoint.Project.IsOpen) {
                if(e.Action.Id == Id) {
                    ActionPoint.Actions.Remove(this);
                    Dispose();
                }
            }
        }

        private void OnActionBaseUpdated(object sender, ActionEventArgs e) {
            if (ActionPoint.Project.IsOpen) {
                if (e.Action.Id == Id) {
                    Meta = e.Action.MapToBareAction();
                    Flows = e.Action.Flows ?? Flows;
                    Parameters = e.Action.Parameters ?? Parameters;
                }
            }
        }

        private void OnActionUpdated(object sender, ActionEventArgs e) {
            if (ActionPoint.Project.IsOpen) {
                if (e.Action.Id == Id) {
                    Meta = e.Action.MapToBareAction();
                    Flows = e.Action.Flows ?? Flows;
                    Parameters = e.Action.Parameters ?? Parameters;
                }
            }
        }
    }
}