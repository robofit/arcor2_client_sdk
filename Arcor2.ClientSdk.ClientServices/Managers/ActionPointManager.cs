using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Action = Arcor2.ClientSdk.Communication.OpenApi.Models.Action;

namespace Arcor2.ClientSdk.ClientServices.Managers {
    /// <summary>
    /// Manages lifetime of an action point.
    /// </summary>
    public class ActionPointManager : LockableArcor2ObjectManager<BareActionPoint> {
        /// <summary>
        /// The parent project.
        /// </summary>
        internal ProjectManager Project { get; }

        /// <summary>
        /// A collection of actions.
        /// </summary>
        public ObservableCollection<ActionManager> Actions { get; private set; }

        /// <summary>
        /// A collection of orientations.
        /// </summary>
        public ObservableCollection<OrientationManager> Orientations { get; private set; }

        /// <summary>
        /// A collection of joints.
        /// </summary>
        public ObservableCollection<JointsManager> Joints { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ActionPointManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="project">The parent project.</param>
        /// <param name="actionPointMeta">The action point metadata.</param>
        public ActionPointManager(Arcor2Session session, ProjectManager project, BareActionPoint actionPointMeta) : base(
            session, actionPointMeta, actionPointMeta.Id) {
            Project = project;
            Actions = new ObservableCollection<ActionManager>();
            Orientations = new ObservableCollection<OrientationManager>();
            Joints = new ObservableCollection<JointsManager>();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ActionPointManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="project">The parent project.</param>
        /// <param name="actionPoint">The action point data.</param>
        public ActionPointManager(Arcor2Session session, ProjectManager project, ActionPoint actionPoint) : base(
            session, actionPoint.MapToBareActionPoint(), actionPoint.Id) {
            Project = project;
            Actions = new ObservableCollection<ActionManager>(actionPoint.Actions.Select(a => new ActionManager(Session, this, a)));
            Orientations = new ObservableCollection<OrientationManager>(actionPoint.Orientations.Select(o => new OrientationManager(Session, this, o)));
            Joints = new ObservableCollection<JointsManager>(actionPoint.RobotJoints.Select(j => new JointsManager(Session, this, j)));
        }

        /// <summary>
        /// Duplicates the action point.
        /// </summary>
        /// <param name="position">The position of duplicated action point.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task DuplicateAsync(Position position) {
            var response = await Session.client.DuplicateActionPointAsync(new CopyActionPointRequestArgs(Id, position));
            if(!response.Result) {
                throw new Arcor2Exception($"Duplicating action point {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Duplicates the action point.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task DuplicateAsync() {
            await DuplicateAsync(Data.Position);
        }

        /// <summary>
        /// Updates a parent object.
        /// </summary>
        /// <remarks>
        /// Empty string to clear.
        /// </remarks>
        /// <param name="newParentId">The ID of the new parent.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateParentAsync(string newParentId) {
            await LockAsync();
            var response = await Session.client.UpdateActionPointParentAsync(new UpdateActionPointParentRequestArgs(Id, newParentId));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating parent for action point {Id} failed.", response.Messages);
            }
            // Unlocked automatically by the server...
        }

        /// <summary>
        /// Updates a parent object.
        /// </summary>
        /// <param name="actionObject">The parent action object.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateParentAsync(ActionObjectManager actionObject) {
            await UpdateParentAsync(actionObject.Id);
        }

        /// <summary>
        /// Updates a parent object.
        /// </summary>
        /// <param name="actionPoint">The parent action point.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateParentAsync(ActionPointManager actionPoint) {
            await UpdateParentAsync(actionPoint.Id);
        }

        /// <summary>
        /// Clears the parent of the action point.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task ClearParentAsync() {
            await UpdateParentAsync(string.Empty);
        }

        /// <summary>
        /// Updates a position of the action point.
        /// </summary>
        /// <param name="newPosition">The new position.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdatePositionAsync(Position newPosition) {
            await LockAsync(true);
            var response = await Session.client.UpdateActionPointPositionAsync(new UpdateActionPointPositionRequestArgs(Id, newPosition));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating position for action point {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Updates action point using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="robotId">The ID of the robot action object.</param>
        /// <param name="endEffectorId">The ID of the end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateUsingRobotAsync(string robotId, string endEffectorId = "default", string? armId = null) {
            await LockAsync(true);
            var response = await Session.client.UpdateActionPointUsingRobotAsync(new UpdateActionPointUsingRobotRequestArgs(Id, new RobotArg(robotId, endEffectorId, armId!)));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating action point {Id} using robot failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Updates action point using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObject">The robot.</param>
        /// <param name="endEffectorId">The ID of the end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateUsingRobotAsync(ActionObjectManager actionObject, string endEffectorId = "default", string? armId = null) {
            await UpdateUsingRobotAsync(actionObject.Id, endEffectorId, armId);
        }

        /// <summary>
        /// Updates action point using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObject">The robot.</param>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateUsingRobotAsync(ActionObjectManager actionObject, EndEffector? endEffector = null, string? armId = null) {
            await UpdateUsingRobotAsync(actionObject.Id, endEffector?.Id ?? "default", armId);
        }

        /// <summary>
        /// Renames the action point.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RenameAsync(string newName) {
            await LockAsync();
            var response = await Session.client.RenameActionPointAsync(new RenameActionPointRequestArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming action point {Id} failed.", response.Messages);
            }
            // Unlocked automatically by the server
        }

        /// <summary>
        /// Removes the action point.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.client.RemoveActionPointAsync(new IdArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing action point {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new action with the default flow.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        /// <param name="actionObject">The action object.</param>
        /// <param name="action">The action itself (listed in <see cref="ObjectTypeManager"/>)</param>
        /// <param name="parameters">The optional list of parameters.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionAsync(string name, ActionObjectManager actionObject, ObjectAction action, List<ActionParameter>? parameters = null) {
            var type = $"{actionObject.Id}/{action.Name}";

            await AddActionAsync(name, type,
                new List<Flow> { new Flow(Flow.TypeEnum.Default, new List<string>()) },
                parameters ?? new List<ActionParameter>());
        }

        /// <summary>
        /// Adds a new action.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        /// <param name="actionObject">The action object.</param>
        /// <param name="action">The action itself (listed in <see cref="ObjectTypeManager"/>)</param>
        /// <param name="parameters">The optional list of parameters.</param>
        /// <param name="flows">The optional list of flows.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionAsync(string name, ActionObjectManager actionObject, ObjectAction action, List<Flow>? flows = null, List<ActionParameter>? parameters = null) {
            var type = $"{actionObject.Id}/{action.Name}";

            await AddActionAsync(name, type,
                flows ?? new List<Flow>(),
                parameters ?? new List<ActionParameter>());
        }

        /// <summary>
        /// Adds a new action.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        /// <param name="type">The action type in '{actionObjectId}/{actionName}' format.</param>
        /// <param name="parameters">The list of parameters.</param>
        /// <param name="flows">The list of flows.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddActionAsync(string name, string type, List<Flow> flows, List<ActionParameter> parameters) {
            var response = await Session.client.AddActionAsync(new AddActionRequestArgs(Id, name, type, parameters, flows));
            if(!response.Result) {
                throw new Arcor2Exception($"Creating new action for action point {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Adds a new orientation.
        /// </summary>
        /// <param name="name">The name of the orientation.</param>
        /// <param name="orientation">The orientation.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddOrientationAsync(Orientation orientation, string name = "default") {
            await LockAsync();
            var response = await Session.client.AddActionPointOrientationAsync(new AddActionPointOrientationRequestArgs(Id, orientation, name));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Creating new orientation for action point {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Adds a new orientation using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="robotId">The robot ID.</param>
        /// <param name="endEffectorId">The ID of the end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <param name="name">The name of the orientation.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddOrientationUsingRobotAsync(string robotId, string endEffectorId = "default", string? armId = null, string name = "default") {
            await LockAsync();
            var response = await Session.client.AddActionPointOrientationUsingRobotAsync(new AddActionPointOrientationUsingRobotRequestArgs(Id, new RobotArg(robotId, endEffectorId, armId!), name));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Creating new orientation using robot for action point {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Adds a new orientation using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObject">The robot.</param>
        /// <param name="endEffectorId">The ID of the end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <param name="name">The name of the orientation.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddOrientationUsingRobotAsync(ActionObjectManager actionObject, string endEffectorId = "default", string? armId = null, string name = "default") {
            await AddOrientationUsingRobotAsync(actionObject.Id, endEffectorId, armId, name);
        }

        /// <summary>
        /// Adds a new orientation using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObject">The robot.</param>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <param name="name">The name of the orientation.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddOrientationUsingRobotAsync(ActionObjectManager actionObject, EndEffector? endEffector = null, string? armId = null, string name = "default") {
            await AddOrientationUsingRobotAsync(actionObject.Id, endEffector?.Id ?? "default", armId, name);
        }

        /// <summary>
        /// Adds new joints using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="robotId">The robot ID.</param>
        /// <param name="endEffectorId">The ID of the end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <param name="name">The name of the joints.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddJointsUsingRobotAsync(string robotId, string endEffectorId = "default", string? armId = null, string name = "default") {
            await LockAsync();
            var response = await Session.client.AddActionPointJointsUsingRobotAsync(new AddActionPointJointsUsingRobotRequestArgs(Id, robotId, name, armId!, endEffectorId));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Creating new joints using robot for action point {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Adds new joints using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObject">The robot.</param>
        /// <param name="endEffectorId">The ID of the end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <param name="name">The name of the joints.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddJointsUsingRobotAsync(ActionObjectManager actionObject, string endEffectorId = "default", string? armId = null, string name = "default") {
            await AddJointsUsingRobotAsync(actionObject.Id, endEffectorId, armId, name);
        }

        /// <summary>
        /// Adds new joints using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="actionObject">The robot.</param>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <param name="name">The name of the joints.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddJointsUsingRobotAsync(ActionObjectManager actionObject, EndEffector? endEffector = null, string? armId = null, string name = "default") {
            await AddJointsUsingRobotAsync(actionObject.Id, endEffector?.Id ?? "default", armId, name);
        }

        /// <summary>
        /// Updates the action point according to the <paramref name="actionPoint"/> instance.
        /// </summary>
        /// <param name="actionPoint">Newer version of the action point.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(ActionPoint actionPoint) {
            if(Id != actionPoint.Id) {
                throw new InvalidOperationException($"Can't update an ActionPointManager ({Id}) using a action point data object ({actionPoint.Id}) with different ID.");
            }

            UpdateData(actionPoint.MapToBareActionPoint());
            Actions = new ObservableCollection<ActionManager>(Actions.UpdateListOfLockableArcor2Objects<ActionManager, Action, Action>(actionPoint.Actions,
                a => a.Id,
                (m, a) => m.UpdateAccordingToNewObject(a),
                a => new ActionManager(Session, this, a)));
            Orientations = new ObservableCollection<OrientationManager>(Orientations.UpdateListOfLockableArcor2Objects<OrientationManager, NamedOrientation, NamedOrientation>(actionPoint.Orientations,
                o => o.Id,
                (m, o) => m.UpdateAccordingToNewObject(o),
                o => new OrientationManager(Session, this, o)));
            Joints = new ObservableCollection<JointsManager>(Joints.UpdateListOfLockableArcor2Objects<JointsManager, ProjectRobotJoints, ProjectRobotJoints>(actionPoint.RobotJoints,
                j => j.Id,
                (m, j) => m.UpdateAccordingToNewObject(j),
                j => new JointsManager(Session, this, j)));
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            foreach (var action in Actions) {
                action.Dispose();
            }
            foreach(var orientation in Orientations) {
                orientation.Dispose();
            }
            foreach (var joint in Joints) {
                joint.Dispose();
            }
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.client.ActionPointUpdated += OnActionPointUpdated;
            Session.client.ActionPointBaseUpdated += OnActionPointBaseUpdated;
            Session.client.ActionPointRemoved += OnActionPointRemoved;
            Session.client.ActionAdded += OnActionAdded;
            Session.client.OrientationAdded += OnOrientationAdded;
            Session.client.JointsAdded += OnJointsAdded;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.ActionPointUpdated -= OnActionPointUpdated;
            Session.client.ActionPointBaseUpdated -= OnActionPointBaseUpdated;
            Session.client.ActionPointRemoved -= OnActionPointRemoved;
            Session.client.ActionAdded -= OnActionAdded;
            Session.client.OrientationAdded -= OnOrientationAdded;
            Session.client.JointsAdded -= OnJointsAdded;
        }

        private void OnJointsAdded(object sender, JointsEventArgs e) {
            if(Project.IsOpen) {
                if(e.ParentId == Id) {
                    Joints.Add(new JointsManager(Session, this, e.Data));
                }
            }
        }

        private void OnOrientationAdded(object sender, OrientationEventArgs e) {
            if (Project.IsOpen) {
                if (e.ParentId == Id) {
                    Orientations.Add(new OrientationManager(Session, this, e.Data));
                }
            }
        }

        private void OnActionPointRemoved(object sender, BareActionPointEventArgs e) {
            if (Project.IsOpen) {
                if (e.ActionPoint.Id == Id) {
                    RemoveData();
                    Project.ActionPoints!.Remove(this);
                    Dispose();
                }
            }
        }

        private void OnActionPointBaseUpdated(object sender, BareActionPointEventArgs e) {
            if (Project.IsOpen) {
                if (e.ActionPoint.Id == Id) {
                    UpdateData(e.ActionPoint);
                }
            }
        }

        private void OnActionPointUpdated(object sender, BareActionPointEventArgs e) {
            if (Project.IsOpen) {
                if (e.ActionPoint.Id == Id) {
                    UpdateData(e.ActionPoint);
                }
            }
        }

        private void OnActionAdded(object sender, ActionEventArgs e) {
            if (Project.IsOpen) {
                if(e.ParentId == Id) {
                    Actions.Add(new ActionManager(Session, this, e.Action));
                }
            }
        }
    }
}
