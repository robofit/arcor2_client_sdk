﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.ClientServices.Models.Extras;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Joint = Arcor2.ClientSdk.ClientServices.Models.Extras.Joint;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// Manages a lifetime of a scene's action object.
    /// </summary>
    public class ActionObjectManager : LockableArcor2ObjectManager {
        /// <summary>
        /// The parent scene.
        /// </summary>
        internal SceneManager Scene { get; }

        /// <summary>
        /// Information about the object type.
        /// </summary>
        public SceneObject Data { get; private set; }

        /// <summary>
        /// The list of joints and its values.
        /// </summary>
        /// <value>
        /// <c>null</c> if not applicable (e.g. the object is not a robot).
        /// </value>
        public IList<Joint>? Joints { get; private set; }

        /// <summary>
        /// The list of end effectors and its poses.
        /// </summary>
        /// <value>
        /// <c>null</c> if not applicable (e.g. the object is not a robot).
        /// </value>
        public IList<EndEffector>? EefPose { get; private set; }

        /// <summary>
        /// The list of joints and its values.
        /// </summary>
        /// <value>
        /// <c>null</c> if not applicable (e.g. the object is not a robot or is single-armed).
        /// </value>
        public IList<string>? Arms { get; private set; }

        /// <summary>
        /// The type of action object.
        /// </summary>
        // TODO: Cache this
        public ObjectTypeManager ObjectType => Session.ObjectTypes.First(o => o.Meta.Type == Data.Type);

        /// <summary>
        /// Initializes a new instance of <see cref="ActionObjectManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="scene">The parent scene.</param>
        /// <param name="data">The action object data.</param>
        internal ActionObjectManager(Arcor2Session session, SceneManager scene, SceneObject data) : base(session, data.Id) {
            Scene = scene;
            Data = data;
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
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating pose of action object {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Steps the robot position along an axis.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="axis">The axis to step.</param>
        /// <param name="step">The step site.</param>
        /// <param name="endEffectorId">The end effector ID. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <param name="mode">The mode. By default, <c>Robot</c></param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StepPositionAsync(StepRobotEefRequestArgs.AxisEnum axis, decimal step, string endEffectorId = "default", string armId = "", bool safe = true, bool linear = false, decimal speed = 1, StepRobotEefRequestArgs.ModeEnum mode = StepRobotEefRequestArgs.ModeEnum.Robot) {
            // TODO: Normal enum
            await LockAsync();
            var response = await Session.client.StepRobotEndEffectorAsync(new StepRobotEefRequestArgs(Id, endEffectorId, axis, StepRobotEefRequestArgs.WhatEnum.Position, mode, step, safe, null!, speed, linear, armId ));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Stepping robot {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Steps the robot orientation along an axis.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="axis">The axis to step.</param>
        /// <param name="step">The step site.</param>
        /// <param name="endEffectorId">The end effector ID. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <param name="mode">The mode. By default, <c>Robot</c></param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StepOrientationAsync(StepRobotEefRequestArgs.AxisEnum axis, decimal step, string endEffectorId = "default", string armId = "", bool safe = true, bool linear = false, decimal speed = 1, StepRobotEefRequestArgs.ModeEnum mode = StepRobotEefRequestArgs.ModeEnum.Robot) {
            // TODO: Normal enum
            await LockAsync();
            var response = await Session.client.StepRobotEndEffectorAsync(new StepRobotEefRequestArgs(Id, endEffectorId, axis, StepRobotEefRequestArgs.WhatEnum.Orientation, mode, step, safe, null!, speed, linear, armId));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Stepping robot {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Moves the robot into a pose.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="pose">The target pose.</param>
        /// <param name="endEffectorId">The end effector ID. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToPoseAsync(Pose pose, string endEffectorId = "default", string armId = "", bool safe = true, bool linear = false, decimal speed = 1) {
            var response = await Session.client.MoveToPoseAsync(new MoveToPoseRequestArgs(Id, endEffectorId, speed, pose.Position, pose.Orientation, safe, linear, armId));
            if(!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to a pose failed.", response.Messages);
            }
        }

        /// <summary>
        /// Moves the robot into orientation of action point.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="orientationId">The orientation ID.</param>
        /// <param name="endEffectorId">The end effector ID. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointOrientationAsync(string orientationId, string endEffectorId = "default", string armId = "", bool safe = true, bool linear = false, decimal speed = 1) {
            var response = await Session.client.MoveToActionPointAsync(new MoveToActionPointRequestArgs(Id, speed, endEffectorId, orientationId, null!, safe, linear, armId));
            if(!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to action point orientation failed.", response.Messages);
            }
        }

        /// <summary>
        /// Moves the robot into orientation of action point.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="orientation">The orientation.</param>
        /// <param name="endEffectorId">The end effector ID. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointOrientationAsync(OrientationManager orientation, string endEffectorId = "default", string armId = "", bool safe = true, bool linear = false, decimal speed = 1) {
            await MoveToActionPointOrientationAsync(orientation.Id, endEffectorId, armId, safe, linear, speed);
        }

        /// <summary>
        /// Moves the robot into joints of action point.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="jointsId">The joints ID.</param>
        /// <param name="endEffectorId">The end effector ID. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(string jointsId, string endEffectorId = "default", string armId = "", bool safe = true, bool linear = false, decimal speed = 1) {
            var response = await Session.client.MoveToActionPointAsync(new MoveToActionPointRequestArgs(Id, speed, endEffectorId, null!, jointsId, safe, linear, armId));
            if (!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to action point joints failed.", response.Messages);
            }
        }

        /// <summary>
        /// Moves the robot into joints of action point.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="joints">The joints.</param>
        /// <param name="endEffectorId">The end effector ID. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(JointsManager joints, string endEffectorId = "default", string armId = "", bool safe = true, bool linear = false, decimal speed = 1) {
            await MoveToActionPointJointsAsync(joints.Id, endEffectorId, armId, safe, linear, speed);
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
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating parameters of action object {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Reloads the arms and their end effector of the robot.
        /// </summary>
        internal async Task ReloadRobotArmsAndEefPose() {
            if (ObjectType.RobotMeta?.MultiArm ?? false) {
                var armsResponse = await Session.client.GetRobotArmsAsync(new GetRobotArmsRequestArgs(Id));

                if(armsResponse.Result) {
                    Arms = armsResponse.Data;
                }
            }

            var eefResponse = await Session.client.GetRobotEndEffectorsAsync(new GetEndEffectorsRequestArgs(Id));

            if (eefResponse.Result) {
                EefPose = eefResponse.Data.Select(id => new EndEffector(id)).ToList();
            }
        }

        /// <summary>
        /// Reloads the joints and their values of the robot.
        /// </summary>
        internal async Task ReloadRobotJoints() {
            var jointsResponse = await Session.client.GetRobotJointsAsync(new GetRobotJointsRequestArgs(Id));
            if(jointsResponse.Result) {
                Joints = jointsResponse.Data.Select(j => j.ToCustomJointObject()).ToList();
            }
        }

        /// <summary>
        /// Register the robot for updates of eef pose/joints.
        /// </summary>
        /// <remarks>
        /// Must be called in an online scene/project. User must be registered.
        /// </remarks>
        /// <param name="type">The type of registration.</param>
        /// <param name="enabled">Toggle on/off.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        internal async Task RegisterForUpdatesAsync(RobotUpdateType type, bool enabled = true) {
            var response = await Session.client.RegisterForRobotEventAsync(new RegisterForRobotEventRequestArgs(Id, send: true, what: type switch {
                RobotUpdateType.Pose => RegisterForRobotEventRequestArgs.WhatEnum.EefPose,
                RobotUpdateType.Joints => RegisterForRobotEventRequestArgs.WhatEnum.Joints,
                _ => throw new InvalidOperationException("Bad RobotUpdateType enum value.")
            } ));
            if(!response.Result) {
                throw new Arcor2Exception($"Registering for robot updates for action object {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        /// Updates the action object according to the <paramref name="actionObject"/> instance.
        /// </summary>
        /// <param name="actionObject">Newer version of the action object.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(SceneObject actionObject) {
            if(Id != actionObject.Id) {
                throw new InvalidOperationException($"Can't update an ActionObjectManager ({Id}) using a action object data object ({actionObject.Id}) with different ID.");
            }

            Data = actionObject;
        }

        protected override void RegisterHandlers() {
            Session.client.OnSceneActionObjectUpdated += OnSceneActionObjectUpdated;
            Session.client.OnSceneActionObjectRemoved += OnSceneActionObjectRemoved;
            Session.client.OnRobotJointsUpdated += OnRobotJointsUpdated;
            Session.client.OnRobotEndEffectorUpdated += OnRobotEndEffectorUpdated;
        }
        protected override void UnregisterHandlers() {
            Session.client.OnSceneActionObjectUpdated -= OnSceneActionObjectUpdated;
            Session.client.OnSceneActionObjectRemoved -= OnSceneActionObjectRemoved;
            Session.client.OnRobotJointsUpdated -= OnRobotJointsUpdated;
            Session.client.OnRobotEndEffectorUpdated -= OnRobotEndEffectorUpdated;
        }

        private void OnSceneActionObjectUpdated(object sender, SceneActionObjectEventArgs e) {
            if(Id == e.SceneObject.Id) {
                Data = e.SceneObject;
            }
        }

        private void OnSceneActionObjectRemoved(object sender, SceneActionObjectEventArgs e) {
            if(Id == e.SceneObject.Id) {
                Scene.ActionObjects?.Remove(this);
                Dispose();
            }
        }
        private void OnRobotEndEffectorUpdated(object sender, RobotEndEffectorUpdatedEventArgs e) {
            if (e.Data.RobotId == Id) {
                EefPose = e.Data.EndEffectors.Select(e => e.ToEndEffector()).ToList();
            }
        }

        private void OnRobotJointsUpdated(object sender, RobotJointsUpdatedEventArgs e) {
            if (Id == e.Data.RobotId) {
                Joints = e.Data.Joints.Select(j => j.ToCustomJointObject()).ToList();
            }
        }

    }
}

