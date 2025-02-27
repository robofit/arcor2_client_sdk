using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class ActionObjectManager : LockableArcor2ObjectManager<ActionObject> {
        // TODO: Stop, maybe Grippers and suctions?

        /// <summary>
        /// The parent scene.
        /// </summary>
        internal SceneManager Scene { get; }

        private ObjectTypeManager? cachedObjectType;

        /// <summary>
        /// The type of action object.
        /// </summary>
        public ObjectTypeManager ObjectType {
            get {
                return cachedObjectType ??= Session.ObjectTypes.First(o => o.Data.Meta.Type == Data.Meta.Type);
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ActionObjectManager"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="scene">The parent scene.</param>
        /// <param name="meta">The action object data.</param>
        internal ActionObjectManager(Arcor2Session session, SceneManager scene, SceneObject meta) : base(session, new ActionObject(meta), meta.Id) {
            Scene = scene;
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
        public async Task StepPositionAsync(Axis axis, decimal step, string endEffectorId = "default", string armId = "", bool safe = true, bool linear = false, decimal speed = 1, StepMode mode = StepMode.Robot) {
            await LockAsync();
            var response = await Session.client.StepRobotEndEffectorAsync(new StepRobotEefRequestArgs(Id, endEffectorId, axis.ToOpenApiAxisEnum(), StepRobotEefRequestArgs.WhatEnum.Position, mode.ToOpenApiModeEnum(), step, safe, null!, speed, linear, armId ));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Stepping robot {Id} failed.", response.Messages);
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
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <param name="mode">The mode. By default, <c>Robot</c></param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StepPositionAsync(Axis axis, decimal step, EndEffector? endEffector = null, string armId = "", bool safe = true, bool linear = false, decimal speed = 1, StepMode mode = StepMode.Robot) {
            await LockAsync();
            var response = await Session.client.StepRobotEndEffectorAsync(new StepRobotEefRequestArgs(Id, endEffector?.Id ?? "default", axis.ToOpenApiAxisEnum(), StepRobotEefRequestArgs.WhatEnum.Position, mode.ToOpenApiModeEnum(), step, safe, null!, speed, linear, armId));
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
        public async Task StepOrientationAsync(Axis axis, decimal step, string endEffectorId = "default", string armId = "", bool safe = true, bool linear = false, decimal speed = 1, StepMode mode = StepMode.Robot) {
            await LockAsync();
            var response = await Session.client.StepRobotEndEffectorAsync(new StepRobotEefRequestArgs(Id, endEffectorId, axis.ToOpenApiAxisEnum(), StepRobotEefRequestArgs.WhatEnum.Orientation, mode.ToOpenApiModeEnum(), step, safe, null!, speed, linear, armId));
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
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <param name="mode">The mode. By default, <c>Robot</c></param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StepOrientationAsync(Axis axis, decimal step, EndEffector? endEffector = null, string? armId = null, bool safe = true, bool linear = false, decimal speed = 1, StepMode mode = StepMode.Robot) {
            await LockAsync();
            var response = await Session.client.StepRobotEndEffectorAsync(new StepRobotEefRequestArgs(Id, endEffector?.Id ?? "default", axis.ToOpenApiAxisEnum(), StepRobotEefRequestArgs.WhatEnum.Orientation, mode.ToOpenApiModeEnum(), step, safe, null!, speed, linear, armId!));
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
        /// The scene must be online and the action object a robot with corresponding feature. See <c>MoveToPose</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="pose">The target pose.</param>
        /// <param name="endEffectorId">The end effector ID. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToPoseAsync(Pose pose, string endEffectorId = "default", string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            var response = await Session.client.MoveToPoseAsync(new MoveToPoseRequestArgs(Id, endEffectorId, speed, pose.Position, pose.Orientation, safe, linear, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to a pose failed.", response.Messages);
            }
        }


        /// <summary>
        /// Moves the robot into a pose.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>MoveToPose</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="pose">The target pose.</param>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToPoseAsync(Pose pose, EndEffector? endEffector = null, string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            var response = await Session.client.MoveToPoseAsync(new MoveToPoseRequestArgs(Id, endEffector?.Id ??  "default", speed, pose.Position, pose.Orientation, safe, linear, armId!));
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
        public async Task MoveToActionPointOrientationAsync(string orientationId, string endEffectorId = "default", string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            var response = await Session.client.MoveToActionPointAsync(new MoveToActionPointRequestArgs(Id, speed, endEffectorId, orientationId, null!, safe, linear, armId!));
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
        public async Task MoveToActionPointOrientationAsync(OrientationManager orientation, string endEffectorId = "default", string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            await MoveToActionPointOrientationAsync(orientation.Id, endEffectorId, armId, safe, linear, speed);
        }

        /// <summary>
        /// Moves the robot into orientation of action point.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="orientationId">The orientation ID.</param>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointOrientationAsync(string orientationId, EndEffector? endEffector = null, string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            await MoveToActionPointOrientationAsync(orientationId, endEffector?.Id ?? "default", armId, safe, linear, speed);
        }

        /// <summary>
        /// Moves the robot into orientation of action point.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="orientation">The orientation.</param>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointOrientationAsync(OrientationManager orientation, EndEffector? endEffector = null, string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            await MoveToActionPointOrientationAsync(orientation.Id, endEffector?.Id ?? "default", armId, safe, linear, speed);
        }

        /// <summary>
        /// Moves the robot into joints of action point.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>MoveToJoints</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="jointsId">The joints ID.</param>
        /// <param name="endEffectorId">The end effector ID. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(string jointsId, string endEffectorId = "default", string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            var response = await Session.client.MoveToActionPointAsync(new MoveToActionPointRequestArgs(Id, speed, endEffectorId, null!, jointsId, safe, linear, armId!));
            if (!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to action point joints failed.", response.Messages);
            }
        }

        /// <summary>
        /// Moves the robot into joints of action point.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>MoveToJoints</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The joints.</param>
        /// <param name="endEffectorId">The end effector ID. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(JointsManager joints, string endEffectorId = "default", string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            await MoveToActionPointJointsAsync(joints.Id, endEffectorId, armId, safe, linear, speed);
        }

        /// <summary>
        /// Moves the robot into joints of action point.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>MoveToJoints</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="jointsId">The joints ID.</param>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(string jointsId, EndEffector? endEffector = null, string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            await MoveToActionPointJointsAsync(jointsId, endEffector?.Id ?? "default", armId, safe, linear, speed);
        }

        /// <summary>
        /// Moves the robot into joints of action point.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>MoveToJoints</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The joints.</param>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(JointsManager joints, EndEffector? endEffector = null, string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            await MoveToActionPointJointsAsync(joints.Id, endEffector?.Id ?? "default", armId, safe, linear, speed);
        }

        /// <summary>
        /// Sets the robot's end effector perpendicular to world.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SetEndEffectorPerpendicularToWorldAsync(string endEffectorId, string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            await LockAsync();
            var response = await Session.client.SetEndEffectorPerpendicularToWorldAsync(new SetEefPerpendicularToWorldRequestArgs(Id, endEffectorId, safe, speed, linear, armId!));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Setting robot {Id} eef perpendicular to world failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Sets the robot's end effector perpendicular to world.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SetEndEffectorPerpendicularToWorldAsync(EndEffector endEffector, string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            await SetEndEffectorPerpendicularToWorldAsync(endEffector.Id, armId, safe, linear, speed);
        }

        /// <summary>
        /// Sets the robot's end effector perpendicular to world using the default end effector.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SetEndEffectorPerpendicularToWorldAsync(string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            await SetEndEffectorPerpendicularToWorldAsync("default", armId, safe, linear, speed);
        }


        /// <summary>
        /// Enables or disables hand teaching mode for the robot and optionally its arm.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>HandTeaching</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="enable">Should hand teaching mode be enabled?</param>
        /// <param name="armId">The optional arm ID.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SetHandTeachingModeAsync(bool enable = true, string? armId = null) {
            await LockAsync();
            var response = await Session.client.SetHandTeachingModeAsync(new HandTeachingModeRequestArgs(Id, enable, armId!));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Setting hand teaching mode for robot {Id} failed.", response.Messages);
            }
            await UnlockAsync();
        }

        /// <summary>
        /// Calculates the forwards kinematics for the robot and optionally its arm. Uses default end effector.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The joints and their values.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync(IList<Joint> joints, string? armId = null) {
            return await GetForwardKinematicsAsync("default", joints, armId);
        }

        /// <summary>
        /// Calculates the forwards kinematics for the robot and optionally its arm.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The joints and their values.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync(string endEffectorId, IList<Joint> joints, string? armId = null) {
            var response = await Session.client.ForwardKinematicsAsync(new ForwardKinematicsRequestArgs(Id, endEffectorId, joints.Select(j => j.ToOpenApiJointObject()).ToList(), armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Getting forward kinematics for robot {Id} failed.", response.Messages);
            }
            return response.Data;
        }

        /// <summary>
        /// Calculates the forwards kinematics for the robot and optionally its arm.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The joints and their values.</param>
        /// <param name="endEffector">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync(EndEffector endEffector, IList<Joint> joints, string? armId = null) {
            return await GetForwardKinematicsAsync(endEffector.Id, joints, armId);
        }

        /// <summary>
        /// Calculates the forwards kinematics using the current joint values.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync(string endEffectorId, string? armId = null) {
            var joints = Data.Joints!.Select(j => j.ToOpenApiJointObject()).ToList();
            var response = await Session.client.ForwardKinematicsAsync(new ForwardKinematicsRequestArgs(Id, endEffectorId, joints, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Getting forward kinematics for robot {Id} failed.", response.Messages);
            }

            return response.Data;
        }

        /// <summary>
        /// Calculates the forwards kinematics using the current joint values. Uses the default end effector.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync() {
            return await GetForwardKinematicsAsync("default");
        }

        /// <summary>
        /// Calculates the forwards kinematics using the current joint values. Uses the default end effector.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync(EndEffector endEffector, string? armId = null) {
            return await GetForwardKinematicsAsync(endEffector.Id, armId);
        }

        /// <summary>
        /// Calculates the inverse kinematics using the current pose and joints.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>> GetInverseKinematicsAsync(string endEffectorId, bool avoidCollisions = true, string? armId = null) {
            var pose = Data.EefPoses!.First(e => e.Id == endEffectorId).Pose!;
            var joints = Data.Joints!.Select(j => j.ToOpenApiJointObject()).ToList();
            var response = await Session.client.InverseKinematicsAsync(new InverseKinematicsRequestArgs(Id, endEffectorId, pose, joints, avoidCollisions, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Getting inverse kinematics for robot {Id} failed.", response.Messages);
            }

            return response.Data.Select(j => j.ToCustomJointObject()).ToList();
        }

        /// <summary>
        /// Calculates the inverse kinematics using the current pose and joints. Uses the default end effector.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>> GetInverseKinematicsAsync(bool avoidCollisions = true, string? armId = null) {
            return await GetInverseKinematicsAsync("default", avoidCollisions, armId);
        }

        /// <summary>
        /// Calculates the inverse kinematics using the current pose and joints.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>> GetInverseKinematicsAsync(EndEffector endEffector, bool avoidCollisions = true, string? armId = null) {
            return await GetInverseKinematicsAsync(endEffector.Id, avoidCollisions, armId);
        }

        /// <summary>
        /// Calculates the inverse kinematics.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="pose">THe pose for which to calculate inverse kinematics.</param>
        /// <param name="startingJoints">The starting joint values to help IK solver to converge to close solution. By default, uses the current joint values.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>> GetInverseKinematicsAsync(string endEffectorId, Pose pose, IList<Joint>? startingJoints = null, bool avoidCollisions = true, string? armId = null) {
            var joints = (startingJoints ?? Data.Joints)?.Select(j => j.ToOpenApiJointObject())?.ToList();
            var response = await Session.client.InverseKinematicsAsync(new InverseKinematicsRequestArgs(Id, endEffectorId, pose, joints!, avoidCollisions, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Getting inverse kinematics for robot {Id} failed.", response.Messages);
            }

            return response.Data.Select(j => j.ToCustomJointObject()).ToList();
        }

        /// <summary>
        /// Calculates the inverse kinematics.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="pose">THe pose for which to calculate inverse kinematics.</param>
        /// <param name="startingJoints">The starting joint values to help IK solver to converge to close solution. By default, uses the current joint values.</param>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>> GetInverseKinematicsAsync(EndEffector endEffector, Pose pose, IList<Joint>? startingJoints = null, bool avoidCollisions = true, string? armId = null) {
            return await GetInverseKinematicsAsync(endEffector.Id, pose, startingJoints, avoidCollisions, armId);
        }

        /// <summary>
        /// Calculates the inverse kinematics. Uses the default end effector.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c> feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="pose">THe pose for which to calculate inverse kinematics.</param>
        /// <param name="startingJoints">The starting joint values to help IK solver to converge to close solution. By default, uses the current joint values.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>> GetInverseKinematicsAsync(Pose pose, IList<Joint>? startingJoints = null, bool avoidCollisions = true, string? armId = null) {
            return await GetInverseKinematicsAsync("default", pose, startingJoints, avoidCollisions, armId);
        }

        /// <summary>
        /// Calibrates the robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object must be a robot.
        /// </remarks>
        /// <param name="cameraActionObjectId">The ID of the camera action object.</param>
        /// <param name="moveToCalibrationPose">Should the robot move to the calibration pose? By default, <c>true</c>.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CalibrateRobotAsync(string cameraActionObjectId, bool moveToCalibrationPose = true) {
            await LockAsync();
            var response = await Session.client.CalibrateRobotAsync(new CalibrateRobotRequestArgs(Id, cameraActionObjectId, moveToCalibrationPose));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Calibrating robot {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Calibrates the robot.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object must be a robot.
        /// </remarks>
        /// <param name="cameraActionObject">The camera action object.</param>
        /// <param name="moveToCalibrationPose">Should the robot move to the calibration pose? By default, <c>true</c>.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CalibrateRobotAsync(ActionObjectManager cameraActionObject, bool moveToCalibrationPose = true) {
            await CalibrateRobotAsync(cameraActionObject.Id, moveToCalibrationPose);
        }

        /// <summary>
        /// Calibrates the camera.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object must be of the Camera type.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CalibrateCameraAsync() {
            await LockAsync();
            var response = await Session.client.CalibrateCameraAsync(new CalibrateCameraRequestArgs(Id));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Calibrating camera {Id} failed.", response.Messages);
            }
            await UnlockAsync();
        }

        /// <summary>
        /// Retrieves camera color image.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object must be of the Camera type.
        /// </remarks>
        /// <returns>
        /// Raw byte array representing the image in JPEG format.
        /// </returns>
        /// <exception cref="Arcor2Exception"></exception>
        [Obsolete("Not implemented as of ARServer 1.5.0.")]
        public async Task<byte[]> GetCameraColorImageAsync() {
            await LockAsync();
#pragma warning disable CS0618 // Type or member is obsolete
            var response = await Session.client.GetCameraColorImageAsync(new CameraColorImageRequestArgs(Id));
#pragma warning restore CS0618 // Type or member is obsolete
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Getting camera color image {Id} failed.", response.Messages);
            }
            await UnlockAsync();

            var imageBytes = Encoding.GetEncoding("iso-8859-1").GetBytes(response.Data);
            return imageBytes;
        }


        /// <summary>
        /// Retrieves camera color intrinsic parameters.
        /// </summary>
        /// <remarks>
        /// The scene must be online and the action object must be of the Camera type.
        /// </remarks>
        /// <returns>A <see cref="CameraParameters"/> object containing the intrinsic parameters of the camera's color sensor.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<CameraParameters> GetCameraColorParametersAsync() {
            await LockAsync();
            var response = await Session.client.GetCameraColorParametersAsync(new CameraColorParametersRequestArgs(Id));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Getting camera color parameters {Id} failed.", response.Messages);
            }
            await UnlockAsync();
            return response.Data;
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
            await LockAsync();
            var response = await Session.client.UpdateActionObjectParametersAsync(new UpdateObjectParametersRequestArgs(Id, parameters.ToList()));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating parameters of action object {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Updates an existing parameter of an action object.
        /// </summary>
        /// <param name="parameter">The modified parameter.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateParameterAsync(Parameter parameter) {
            var newParameters = Data.Meta.Parameters
                .Where(p => p.Name != parameter.Name && p.Type != parameter.Type)
                .Append(parameter)
                .ToList();
            await LockAsync();
            var response = await Session.client.UpdateActionObjectParametersAsync(new UpdateObjectParametersRequestArgs(Id, newParameters));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating parameters of action object {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Adds a new parameter to an action object.
        /// </summary>
        /// <param name="parameter">The new parameter.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddParameterAsync(Parameter parameter) {
            await LockAsync();
            var response = await Session.client.UpdateActionObjectParametersAsync(new UpdateObjectParametersRequestArgs(Id, Data.Meta.Parameters.Append(parameter).ToList()));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating parameters of action object {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Removes a parameter from an action object.
        /// </summary>
        /// <param name="parameter">The removed parameter.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveParameterAsync(Parameter parameter) {
            var newParameters = Data.Meta.Parameters
                .Where(p => p.Name != parameter.Name && p.Type != parameter.Type)
                .ToList();
            await LockAsync();
            var response = await Session.client.UpdateActionObjectParametersAsync(new UpdateObjectParametersRequestArgs(Id, newParameters));
            if(!response.Result) {
                await TryUnlockAsync();
                throw new Arcor2Exception($"Updating parameters of action object {Id} failed.", response.Messages);
            }

            await UnlockAsync();
        }

        /// <summary>
        /// Reloads the arms and their end effector of the robot.
        /// </summary>
        /// <remarks>
        /// This method is invoked automatically and should only be invoked
        /// in edge cases (the library not being able to subscribe on registration
        /// due to locked object type).
        /// </remarks>
        public async Task ReloadRobotArmsAndEefPose() {
            if (ObjectType.Data.RobotMeta?.MultiArm ?? false) {
                var armsResponse = await Session.client.GetRobotArmsAsync(new GetRobotArmsRequestArgs(Id));
                // Do not throw, it may be single-armed... despite the property
                if(armsResponse.Result) {
                    Data.Arms = armsResponse.Data;
                }
            }

            var eefResponse = await Session.client.GetRobotEndEffectorsAsync(new GetEndEffectorsRequestArgs(Id));
            if (!eefResponse.Result) {
                throw new Arcor2Exception($"Getting end effectors for action object {Id} failed.", eefResponse.Messages);
            }

            var endEffectors = eefResponse.Data.Select(id => new EndEffector(id)).ToList();
            foreach (var endEffector in endEffectors) {
                var poseResponse = await Session.client.GetEndEffectorPoseAsync(new GetEndEffectorPoseRequestArgs(Id, endEffector.Id, endEffector.ArmId!));
                if(!eefResponse.Result) {
                    throw new Arcor2Exception($"Could not get end effector '{endEffector.Id}' pose for action object {Id}.", eefResponse.Messages);
                }
                endEffector.Pose = poseResponse.Data;
            }
            Data.EefPoses = endEffectors;

            OnUpdated();
        }

        /// <summary>
        /// Reloads the joints and their values of the robot.
        /// </summary>
        /// <remarks>
        /// This method is invoked automatically and should only be invoked
        /// in edge cases (the library not being able to subscribe on registration
        /// due to locked object type).
        /// </remarks>
        public async Task ReloadRobotJoints() {
            var jointsResponse = await Session.client.GetRobotJointsAsync(new GetRobotJointsRequestArgs(Id));
            if(jointsResponse.Result) {
                Data.Joints = jointsResponse.Data.Select(j => j.ToCustomJointObject()).ToList();
                OnUpdated();
            }
        }

        /// <summary>
        /// Register the robot for updates of eef pose/joints.
        /// </summary>
        /// <remarks>
        /// This method is invoked automatically and should only be invoked
        /// in edge cases (the library not being able to subscribe on registration
        /// due to locked object type). The scene must be online, and the user registered.
        /// </remarks>
        /// <param name="type">The type of registration.</param>
        /// <param name="enabled">Toggle on/off.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RegisterForUpdatesAsync(RobotUpdateType type, bool enabled = true) {
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

            Data.Meta = actionObject;
            OnUpdated();
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.client.SceneActionObjectUpdated += OnSceneActionObjectUpdated;
            Session.client.SceneActionObjectRemoved += OnSceneActionObjectRemoved;
            Session.client.RobotJointsUpdated += OnRobotJointsUpdated;
            Session.client.RobotEndEffectorUpdated += OnRobotEndEffectorUpdated;
        }
        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.client.SceneActionObjectUpdated -= OnSceneActionObjectUpdated;
            Session.client.SceneActionObjectRemoved -= OnSceneActionObjectRemoved;
            Session.client.RobotJointsUpdated -= OnRobotJointsUpdated;
            Session.client.RobotEndEffectorUpdated -= OnRobotEndEffectorUpdated;
        }

        private void OnSceneActionObjectUpdated(object sender, SceneActionObjectEventArgs e) {
            if(Id == e.SceneObject.Id) {
                Data.Meta = e.SceneObject;
                OnUpdated();
            }
        }

        private void OnSceneActionObjectRemoved(object sender, SceneActionObjectEventArgs e) {
            if(Id == e.SceneObject.Id) {
                RemoveData();
                Scene.ActionObjects?.Remove(this);
                Dispose();
            }
        }
        private void OnRobotEndEffectorUpdated(object sender, RobotEndEffectorUpdatedEventArgs e) {
            if (e.Data.RobotId == Id) {
                Data.EefPoses = e.Data.EndEffectors.Select(r => r.ToEndEffector()).ToList();
                OnUpdated();
            }
        }

        private void OnRobotJointsUpdated(object sender, RobotJointsUpdatedEventArgs e) {
            if (Id == e.Data.RobotId) {
                Data.Joints = e.Data.Joints.Select(j => j.ToCustomJointObject()).ToList();
                OnUpdated();
            }
        }

    }
}

