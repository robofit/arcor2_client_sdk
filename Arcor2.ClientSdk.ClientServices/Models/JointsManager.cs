using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Action = Arcor2.ClientSdk.Communication.OpenApi.Models.Action;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// Manages lifetime of joints.
    /// </summary>
    public class JointsManager : LockableArcor2ObjectManager {
        /// <summary>
        /// The parent action point.
        /// </summary>
        internal ActionPointManager ActionPoint { get; }

        /// <summary>
        /// The data of the joints.
        /// </summary>
        public ProjectRobotJoints Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Action"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="actionPoint">The parent action point.</param>
        /// <param name="joints">The joints data.</param>
        public JointsManager(Arcor2Session session, ActionPointManager actionPoint, ProjectRobotJoints joints) : base(
            session, joints.Id) {
            ActionPoint = actionPoint;
            Data = joints;
        }

        /// <summary>
        /// Updates the joints.
        /// </summary>
        /// <param name="joints">A new list of joint values.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateAsync(List<Joint> joints) {
            await LockAsync(ActionPoint.Id);
            var response = await Session.client.UpdateActionPointJointsAsync(new UpdateActionPointJointsRequestArgs(Id, joints));
            if(!response.Result) {
                await TryUnlockAsync(ActionPoint.Id);
                throw new Arcor2Exception($"Updating joints {Id} for action point {Id} failed.", response.Messages);
            }

            await UnlockAsync(ActionPoint.Id);
        }

        /// <summary>
        /// Updates the joints using a corresponding robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateUsingRobotAsync() {
            await LockAsync(ActionPoint.Id);
            var response = await Session.client.UpdateActionPointJointsUsingRobotAsync(new UpdateActionPointJointsUsingRobotRequestArgs(Id));
            if(!response.Result) {
                await TryUnlockAsync(ActionPoint.Id);
                throw new Arcor2Exception($"Updating joints {Id} for action point {Id} using robot failed.", response.Messages);
            }

            await UnlockAsync(ActionPoint.Id);
        }

        /// <summary>
        /// Removes the joints.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync() {
            await LockAsync(ActionPoint.Id);
            var response = await Session.client.RemoveActionPointJointsAsync(new RemoveActionPointJointsRequestArgs(Id));
            if(!response.Result) {
                await TryUnlockAsync(ActionPoint.Id);
                throw new Arcor2Exception($"Removing joints {Id} failed.", response.Messages);
            }
            await UnlockAsync(ActionPoint.Id);
        }

        /// <summary>
        /// Renames the joints.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RenameAsync(string newName) {
            await LockAsync(ActionPoint.Id);
            var response = await Session.client.RenameActionPointJointsAsync(new RenameActionPointJointsRequestArgs(Id, newName));
            if(!response.Result) {
                await TryUnlockAsync(ActionPoint.Id);
                throw new Arcor2Exception($"Renaming joints {Id} failed.", response.Messages);
            }

            await UnlockAsync(ActionPoint.Id);
        }

        /// <summary>
        /// Updates the joints according to the <paramref name="joints"/> instance.
        /// </summary>
        /// <param name="joints">Newer version of the joints.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(ProjectRobotJoints joints) {
            if (Id != joints.Id) {
                throw new InvalidOperationException(
                    $"Can't update an JointsManager ({Id}) using an joints data object ({joints.Id}) with different ID.");
            }

            Data = joints;
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.client.OnJointsUpdated += OnJointsUpdated;
            Session.client.OnJointsBaseUpdated += OnJointsBaseUpdated;
            Session.client.OnJointsRemoved += OnJointsRemoved;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.OnJointsUpdated -= OnJointsUpdated;
            Session.client.OnJointsBaseUpdated -= OnJointsBaseUpdated;
            Session.client.OnJointsRemoved -= OnJointsRemoved;
        }

        private void OnJointsRemoved(object sender, JointsEventArgs e) {
            if(ActionPoint.Project.IsOpen) {
                if(e.Data.Id == Id) {
                    ActionPoint.Joints.Remove(this);
                    Dispose();
                }
            }
        }

        private void OnJointsBaseUpdated(object sender, JointsEventArgs e) {
            if (ActionPoint.Project.IsOpen) {
                if (e.Data.Id == Id) {
                    Data = e.Data;
                }
            }
        }

        private void OnJointsUpdated(object sender, JointsEventArgs e) {
            if(ActionPoint.Project.IsOpen) {
                if(e.Data.Id == Id) {
                    Data = e.Data;
                }
            }
        }
    }
}