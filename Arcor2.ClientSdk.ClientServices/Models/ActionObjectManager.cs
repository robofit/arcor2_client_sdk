using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// Manages a lifetime of a scene's action object.
    /// </summary>
    public class ActionObjectManager : Arcor2ObjectManager {
        /// <summary>
        /// The parent scene.
        /// </summary>
        internal SceneManager Scene { get; }

        /// <summary>
        /// Information about the object type.
        /// </summary>
        public SceneObject Data { get; internal set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ActionObjectManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="scene">The parent scene.</param>
        /// <param name="data">The action object data.</param>
        public ActionObjectManager(Arcor2Session session, SceneManager scene, SceneObject data) : base(session, data.Id) {
            Scene = scene;
            Data = data;
        }

        /// <summary>
        /// Gets the corresponding object type.
        /// </summary>
        public ObjectTypeManager? GetObjectType() {
            return Session.ObjectTypes.FirstOrDefault(o => o.Meta.Type == Data.Type);
        }

        /// <summary>
        /// Removes an action object from the scene.
        /// </summary>
        /// <param name="force">If <c>true</c>, the operation will ignore any warnings.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync(bool force = false) {
            var response = await Session.client.RemoveActionObjectFromSceneAsync(new RemoveFromSceneRequestArgs(Id, force));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing action object {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Updates a pose of an action object.
        /// </summary>
        /// <param name="pose">The pose.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdatePoseAsync(Pose pose) {
            await LockAsync();
            var response = await Session.client.UpdateActionObjectPoseAsync(new UpdateObjectPoseRequestArgs(Id, pose));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating pose of action object {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Renames an action object.
        /// </summary>
        /// <param name="newName">New name.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RenameAsync(string newName) {
            await LockAsync();
            var response = await Session.client.RenameActionObjectAsync(new RenameArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming action object {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Updates parameters of an action object.
        /// </summary>
        /// <param name="parameters">The new list of parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateParametersAsync(ICollection<Parameter> parameters) {
            // TODO: Maybe make this better? Seems kind of awkward to update by passing new list, maybe small CRUD methods?
            await LockAsync();
            var response = await Session.client.UpdateActionObjectParametersAsync(new UpdateObjectParametersRequestArgs(Id, parameters.ToList()));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating parameters of action object {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        internal async Task RegisterForUpdatesAsync(RobotUpdateType type, bool enabled = true) {
            var response = await Session.client.RegisterForRobotEventAsync(new RegisterForRobotEventRequestArgs(Id, send: true, what: type switch {
                RobotUpdateType.Pose => RegisterForRobotEventRequestArgs.WhatEnum.EefPose,
                RobotUpdateType.Joints => RegisterForRobotEventRequestArgs.WhatEnum.Joints
            } ));
            if(!response.Result) {
                throw new Arcor2Exception($"Registering for robot updates for action object {Id} failed.", response.Messages);
            }
        }

        protected override void RegisterHandlers() {
            Session.client.OnSceneActionObjectUpdated += OnSceneActionObjectUpdated;
            Session.client.OnSceneActionObjectRemoved += OnSceneActionObjectRemoved;
        }

        private void OnSceneActionObjectUpdated(object sender, SceneActionObjectEventArgs e) {
            if (Id == e.SceneObject.Id) {
                Data = e.SceneObject;
            }
        }

        private void OnSceneActionObjectRemoved(object sender, SceneActionObjectEventArgs e) {
            if(Id == e.SceneObject.Id) {
                Scene.ActionObjects?.Remove(this);
                Dispose();
            }
        }

        protected override void UnregisterHandlers() {
            Session.client.OnSceneActionObjectUpdated -= OnSceneActionObjectUpdated;
            Session.client.OnSceneActionObjectRemoved -= OnSceneActionObjectRemoved;
        }
    }
}

