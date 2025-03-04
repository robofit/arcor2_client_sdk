using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Action = Arcor2.ClientSdk.Communication.OpenApi.Models.Action;

namespace Arcor2.ClientSdk.ClientServices.Managers
{
    /// <summary>
    /// Manages lifetime of an action.
    /// </summary>
    public class ActionManager : LockableArcor2ObjectManager<Action> {
        /// <summary>
        /// The parent action point.
        /// </summary>
        internal ActionPointManager ActionPoint { get; }

        /// <summary>
        /// Is the action currently executing?
        /// </summary>
        /// <remarks>
        /// This flag is set only for project execution (see <see cref="Executing"/>, <see cref="Executed"/>, and <see cref="Cancelled"/>).
        /// </remarks>
        public bool IsExecuting { get; private set; }

        /// <summary>
        /// Raised when action starts executing in a project.
        /// </summary>
        public event EventHandler? Executing;
        /// <summary>
        /// Raised when action finished executing in a project.
        /// </summary>
        public event EventHandler<ActionExecutedEventArgs>? Executed;
        /// <summary>
        /// Raised when action execution was cancelled in a project.
        /// </summary>
        public event EventHandler? Cancelled;

        /// <summary>
        /// Raised before action is execute in a package.
        /// </summary>
        public event EventHandler<ActionStartingEventArgs>? Starting;
        /// <summary>
        /// Raised after action finished executed in a package.
        /// </summary>
        public event EventHandler<ActionFinishedEventArgs>? Finished; 

        /// <summary>
        /// Gets the action definition from object type.
        /// </summary>
        // Do not cache, the instance can change.
        public ObjectAction ActionType => ActionPoint.Project.Scene.ActionObjects!
            .FirstOrDefault(a => a.Id == Data.Type.Split('/').First())!.ObjectType.Data.Actions
            .FirstOrDefault(a => a.Name == Data.Type.Split('/').Last())!;

        /// <summary>
        /// Initializes a new instance of <see cref="ActionManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="actionPoint">The parent action point.</param>
        /// <param name="actionData">The action metadata.</param>
        public ActionManager(Arcor2Session session, ActionPointManager actionPoint, BareAction actionData) : base(
            session, actionData.MapToAction(), actionData.Id) {
            ActionPoint = actionPoint;
        }


        /// <summary>
        /// Initializes a new instance of <see cref="ActionManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="actionPoint">The parent action point.</param>
        /// <param name="action">The action data.</param>
        public ActionManager(Arcor2Session session, ActionPointManager actionPoint, Action action) : base(
            session, action, action.Id) {
            ActionPoint = actionPoint;
        }

        /// <summary>
        /// Updates the action.
        /// </summary>
        /// <param name="parameters">Updated list of parameters.</param>
        /// <param name="flows">Updated list of flows.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateAsync(List<Flow> flows, List<ActionParameter> parameters) {
            await LibraryLockAsync();
            var response = await Session.Client.UpdateActionAsync(new UpdateActionRequestArgs(Id, parameters, flows));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Updating action {Id} failed.", response.Messages);
            }

            await LibraryUnlockAsync();
        }

        /// <summary>
        /// Updates the action flows.
        /// </summary>
        /// <param name="flows">Updated list of flows.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateFlowsAsync(List<Flow> flows) {
            await UpdateAsync(flows, Data.Parameters!);
        }

        /// <summary>
        /// Updates the action parameters.
        /// </summary>
        /// <param name="parameters">Updated list of parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateParametersAsync(List<ActionParameter> parameters) {
            await UpdateAsync(Data.Flows!, parameters);
        }

        /// <summary>
        /// Removes the action.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.Client.RemoveActionAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing action {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task ExecuteAsync() {
            var response = await Session.Client.ExecuteActionAsync(new ExecuteActionRequestArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Executing action {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Cancels the action.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// Note that this method will cancel any currently running action.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CancelAsync() {
            var response = await Session.Client.CancelActionAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Cancelling action failed.", response.Messages);
            }
        }

        /// <summary>
        /// Renames the action.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RenameAsync(string newName) {
            await LibraryLockAsync();
            var response = await Session.Client.RenameActionAsync(new RenameActionRequestArgs(Id, newName));
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
            if(Id != action.Id) {
                throw new InvalidOperationException(
                    $"Can't update an ActionManager ({Id}) using a action data object ({action.Id}) with different ID.");
            }
            UpdateData(action);
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.Client.ActionUpdated += OnActionUpdated;
            Session.Client.ActionBaseUpdated += OnActionBaseUpdated;
            Session.Client.ActionRemoved += OnActionRemoved;
            Session.Client.ActionExecution += OnActionExecution;
            Session.Client.ActionCancelled += OnActionCancelled;
            Session.Client.ActionResult += OnActionResult;
            Session.Client.ActionStateBefore += OnActionStateBefore;
            Session.Client.ActionStateAfter += OnActionStateAfter;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.Client.ActionUpdated -= OnActionUpdated;
            Session.Client.ActionBaseUpdated -= OnActionBaseUpdated;
            Session.Client.ActionRemoved -= OnActionRemoved;
            Session.Client.ActionExecution -= OnActionExecution;
            Session.Client.ActionCancelled -= OnActionCancelled;
            Session.Client.ActionResult -= OnActionResult;
            Session.Client.ActionStateBefore -= OnActionStateBefore;
            Session.Client.ActionStateAfter -= OnActionStateAfter;
        }

        private void OnActionRemoved(object sender, BareActionEventArgs e) {
            if(ActionPoint.Project.IsOpen) {
                if(e.Data.Id == Id) {
                    RemoveData();
                    ActionPoint.Actions.Remove(this);
                    Dispose();
                }
            }
        }

        private void OnActionBaseUpdated(object sender, ActionEventArgs e) {
            if(ActionPoint.Project.IsOpen) {
                if(e.Data.Id == Id) {
                    UpdateData(e.Data);
                }
            }
        }

        private void OnActionUpdated(object sender, ActionEventArgs e) {
            if(ActionPoint.Project.IsOpen) {
                if(e.Data.Id == Id) {
                    UpdateData(e.Data);
                }
            }
        }

        private void OnActionResult(object sender, ActionResultEventArgs e) {
            if(e.Data.ActionId == Id) {
                if(!IsExecuting) {
                    Session.Logger?.LogWarning($"Action {Id} received ActionResult event when its {nameof(IsExecuting)} property was false.");
                }
                IsExecuting = false;
                Executed?.Invoke(this, new ActionExecutedEventArgs(e.Data.Results, e.Data.Error));
            }
        }

        private void OnActionCancelled(object sender, EventArgs e) {
            // No better way to tell.
            if(IsExecuting) {
                IsExecuting = false;
                Cancelled?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnActionExecution(object sender, ActionExecutionEventArgs e) {
            if(e.Data.ActionId == Id) {
                if(IsExecuting) {
                    Session.Logger?.LogWarning($"Action {Id} received ActionExecution event when its {nameof(IsExecuting)} property was true.");
                }
                IsExecuting = true;
                Executing?.Invoke(this, EventArgs.Empty);
            }
        }
        private void OnActionStateAfter(object sender, ActionStateAfterEventArgs e) {
            if(e.Data.ActionId == Id) {
                IsExecuting = false;
                Finished?.Invoke(this, new ActionFinishedEventArgs(e.Data.Results));
            }
        }

        private void OnActionStateBefore(object sender, ActionStateBeforeEventArgs e) {
            if(e.Data.ActionId == Id) {
                IsExecuting = true;
                Starting?.Invoke(this, new ActionStartingEventArgs(e.Data.Parameters));
            }
        }
    }
}