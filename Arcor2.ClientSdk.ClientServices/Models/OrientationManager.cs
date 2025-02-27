using System;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Models.Extras;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Action = Arcor2.ClientSdk.Communication.OpenApi.Models.Action;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// Manages lifetime of an orientation.
    /// </summary>
    public class OrientationManager : LockableArcor2ObjectManager<NamedOrientation> {
        /// <summary>
        /// The parent action point.
        /// </summary>
        internal ActionPointManager ActionPoint { get; }


        /// <summary>
        /// Initializes a new instance of <see cref="Action"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="actionPoint">The parent action point.</param>
        /// <param name="orientation">The orientation data.</param>
        internal OrientationManager(Arcor2Session session, ActionPointManager actionPoint, NamedOrientation orientation) : base(
            session, orientation, orientation.Id) {
            ActionPoint = actionPoint;
        }

        /// <summary>
        /// Updates the orientation.
        /// </summary>
        /// <param name="newOrientation">The new orientation.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateAsync(Orientation newOrientation) {
            await LockAsync();
            var response = await Session.client.UpdateActionPointOrientationAsync(new UpdateActionPointOrientationRequestArgs(Id, newOrientation));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating orientation {Id} for action point {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Updates the orientation using a robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <param name="robotId">The ID of the robot action object.</param>
        /// <param name="endEffectorId">The ID of the end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The ID of the arm. By default, <c>null</c>.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateUsingRobotAsync(string robotId, string endEffectorId = "default", string? armId = null) {
            await LockAsync();
            var response = await Session.client.UpdateActionPointOrientationUsingRobotAsync(new UpdateActionPointOrientationUsingRobotRequestArgs(Id, new RobotArg(robotId, endEffectorId, armId!)));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating orientation {Id} for action point {Id} using robot failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Updates the orientation using a robot.
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
        /// Updates the orientation using a robot.
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
        /// Removes the orientation.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            var response = await Session.client.RemoveActionPointOrientationAsync(new RemoveActionPointOrientationRequestArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing orientation {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Renames the orientation.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RenameAsync(string newName) {
            await LockAsync();
            var response = await Session.client.RenameActionPointOrientationAsync(new RenameActionPointOrientationRequestArgs(Id, newName));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Renaming orientation {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Updates the orientation according to the <paramref name="orientation"/> instance.
        /// </summary>
        /// <param name="orientation">Newer version of the orientation.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(NamedOrientation orientation) {
            if(Id != orientation.Id) {
                throw new InvalidOperationException(
                    $"Can't update an OrientationManager ({Id}) using an orientation data object ({orientation.Id}) with different ID.");
            }

            UpdateData(orientation);
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.client.OrientationUpdated += OnOrientationUpdated;
            Session.client.OrientationBaseUpdated += OnOrientationBaseUpdated;
            Session.client.OrientationRemoved += OnOrientationRemoved;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.OrientationUpdated -= OnOrientationUpdated;
            Session.client.OrientationBaseUpdated -= OnOrientationBaseUpdated;
            Session.client.OrientationRemoved -= OnOrientationRemoved;
        }

        private void OnOrientationRemoved(object sender, OrientationEventArgs e) {
            if(ActionPoint.Project.IsOpen) {
                if(e.Data.Id == Id) {
                    RemoveData();
                    ActionPoint.Orientations.Remove(this);
                    Dispose();
                }
            }
        }

        private void OnOrientationBaseUpdated(object sender, OrientationEventArgs e) {
            if(ActionPoint.Project.IsOpen) {
                if(e.Data.Id == Id) {
                    UpdateData(e.Data);
                }
            }
        }

        private void OnOrientationUpdated(object sender, OrientationEventArgs e) {
            if(ActionPoint.Project.IsOpen) {
                if(e.Data.Id == Id) {
                    UpdateData(e.Data);
                }
            }
        }
    }
}