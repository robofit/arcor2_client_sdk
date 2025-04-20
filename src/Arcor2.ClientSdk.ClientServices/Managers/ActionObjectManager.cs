using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.EventArguments;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Joint = Arcor2.ClientSdk.ClientServices.Models.Joint;

namespace Arcor2.ClientSdk.ClientServices.Managers {
    /// <summary>
    ///     Manages a lifetime of a scene's action object.
    /// </summary>
    public class ActionObjectManager : LockableArcor2ObjectManager<ActionObject> {
        private ObjectTypeManager? cachedObjectType;

        /// <summary>
        ///     Initializes a new instance of <see cref="ActionObjectManager" /> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="scene">The parent scene.</param>
        /// <param name="meta">The action object data.</param>
        internal ActionObjectManager(Arcor2Session session, SceneManager scene, SceneObject meta) : base(session,
            new ActionObject(meta), meta.Id) {
            Scene = scene;
        }

        /// <summary>
        ///     The parent scene.
        /// </summary>
        public SceneManager Scene { get; }

        /// <summary>
        ///     The type of action object.
        /// </summary>
        public ObjectTypeManager ObjectType =>
            cachedObjectType ??= Session.ObjectTypes.First(o => o.Data.Meta.Type == Data.Meta.Type);

        /// <summary>
        ///     Raised when state of long-running process of the action object changes (e.g., camera or robot calibration).
        /// </summary>
        public event EventHandler<ProcessStateChangedEventArgs>? ProcessStateChanged;

        /// <summary>
        ///     Raised when the state of movement of robot to joints changes.
        /// </summary>
        public event EventHandler<RobotMovingToJointsEventArgs>? MovingToJoints;

        /// <summary>
        ///     Raised when the state of movement of robot to joints changes.
        /// </summary>
        public event EventHandler<RobotMovingToPoseEventArgs>? MovingToPose;

        /// <summary>
        ///     Raised when the state of movement of robot to joints changes.
        /// </summary>
        public event EventHandler<RobotMovingToActionPointJointsEventArgs>? MovingToActionPointJoints;

        /// <summary>
        ///     Raised when the state of movement of robot to joints changes.
        /// </summary>
        public event EventHandler<RobotMovingToActionPointOrientationEventArgs>? MovingToActionPointOrientation;

        /// <summary>
        ///     Removes an action object from the scene.
        /// </summary>
        /// <param name="force">If <c>true</c>, the operation will ignore any warnings.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveAsync(bool force = false) {
            var response =
                await Session.Client.RemoveActionObjectFromSceneAsync(new RemoveFromSceneRequestArgs(Id, force));
            if(!response.Result) {
                throw new Arcor2Exception($"Removing action object {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Updates a pose of an action object.
        /// </summary>
        /// <param name="pose">The pose.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdatePoseAsync(Pose pose) {
            await LibraryLockAsync();
            var response = await Session.Client.UpdateActionObjectPoseAsync(new UpdateObjectPoseRequestArgs(Id, pose));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Updating pose of action object {Id} failed.", response.Messages);
            }

            await LibraryUnlockAsync();
        }

        /// <summary>
        ///     Steps the robot position along an axis.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        ///     A pose is needed when
        ///     <param name="mode" />
        ///     is <see cref="StepMode.User" /> or <see cref="StepMode.Relative" />.
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
        public async Task StepPositionAsync(Axis axis, decimal step, string endEffectorId = "default",
            string? armId = null, bool safe = true, bool linear = false, decimal speed = 1,
            StepMode mode = StepMode.Robot) {
            await LibraryLockAsync();
            var response = await Session.Client.StepRobotEndEffectorAsync(new StepRobotEefRequestArgs(Id, endEffectorId,
                axis.MapToOpenApiAxisEnum(), StepRobotEefRequestArgs.WhatEnum.Position, mode.MapToOpenApiModeEnum(),
                step, safe, null!, speed, linear, armId!));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Stepping robot {Id} failed.", response.Messages);
            }

            await LibraryUnlockAsync();
        }

        /// <summary>
        ///     Steps the robot position along an axis.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        ///     A pose is needed when
        ///     <param name="mode" />
        ///     is <see cref="StepMode.User" /> or <see cref="StepMode.Relative" />.
        /// </remarks>
        /// <param name="axis">The axis to step.</param>
        /// <param name="step">The step site.</param>
        /// <param name="endEffector">The end effector. </param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <param name="mode">The mode. By default, <c>Robot</c></param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StepPositionAsync(Axis axis, decimal step, EndEffector endEffector, bool safe = true,
            bool linear = false, decimal speed = 1, StepMode mode = StepMode.Robot) => await StepPositionAsync(axis,
            step, endEffector.Id, endEffector.ArmId, safe, linear, speed, mode);

        /// <summary>
        ///     Steps the robot orientation along an axis.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        ///     A pose is needed when
        ///     <param name="mode" />
        ///     is <see cref="StepMode.User" /> or <see cref="StepMode.Relative" />.
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
        public async Task StepOrientationAsync(Axis axis, decimal step, string endEffectorId = "default",
            string? armId = null, bool safe = true, bool linear = false, decimal speed = 1,
            StepMode mode = StepMode.Robot) {
            await LibraryLockAsync();
            var response = await Session.Client.StepRobotEndEffectorAsync(new StepRobotEefRequestArgs(Id, endEffectorId,
                axis.MapToOpenApiAxisEnum(), StepRobotEefRequestArgs.WhatEnum.Orientation, mode.MapToOpenApiModeEnum(),
                step, safe, null!, speed, linear, armId!));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Stepping robot {Id} failed.", response.Messages);
            }

            await LibraryUnlockAsync();
        }

        /// <summary>
        ///     Steps the robot orientation along an axis.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        ///     A pose is needed when
        ///     <param name="mode" />
        ///     is <see cref="StepMode.User" /> or <see cref="StepMode.Relative" />.
        /// </remarks>
        /// <param name="axis">The axis to step.</param>
        /// <param name="step">The step site.</param>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <param name="mode">The mode. By default, <c>Robot</c></param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StepOrientationAsync(Axis axis, decimal step, EndEffector endEffector, bool safe = true,
            bool linear = false, decimal speed = 1, StepMode mode = StepMode.Robot) => await StepOrientationAsync(axis,
            step, endEffector.Id, endEffector.ArmId, safe, linear, speed, mode);

        /// <summary>
        ///     Moves the robot into a pose. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>MoveToPose</c> feature
        ///     flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="pose">The target pose.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToPoseAsync(Pose pose, string? armId = null, bool safe = true, bool linear = false,
            decimal speed = 1) {
            var response = await Session.Client.MoveToPoseAsync(new MoveToPoseRequestArgs(Id, "default", speed,
                pose.Position, pose.Orientation, safe, linear, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to a pose failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Moves the robot into a pose.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>MoveToPose</c> feature
        ///     flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="pose">The target pose.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToPoseAsync(string endEffectorId, Pose pose, string? armId = null, bool safe = true,
            bool linear = false, decimal speed = 1) {
            var response = await Session.Client.MoveToPoseAsync(new MoveToPoseRequestArgs(Id, endEffectorId, speed,
                pose.Position, pose.Orientation, safe, linear, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to a pose failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Moves the robot into a pose.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>MoveToPose</c> feature
        ///     flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="pose">The target pose.</param>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToPoseAsync(EndEffector? endEffector, Pose pose, bool safe = true, bool linear = false,
            decimal speed = 1) {
            var response = await Session.Client.MoveToPoseAsync(new MoveToPoseRequestArgs(Id,
                endEffector?.Id ?? "default", speed, pose.Position, pose.Orientation, safe, linear,
                endEffector?.ArmId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to a pose failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Moves the robot joints.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>MoveToJoints</c> feature
        ///     flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The target joint values.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToJointsAsync(IList<Joint> joints, string? armId = null, bool safe = true,
            decimal speed = 1) {
            var response = await Session.Client.MoveToJointsAsync(new MoveToJointsRequestArgs(Id, speed,
                joints.Select(j => j.ToOpenApiJointObject()).ToList(), safe, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to a pose failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Moves the robot into orientation of action point. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="orientationId">The orientation ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointOrientationAsync(string orientationId, string? armId = null,
            bool safe = true, bool linear = false, decimal speed = 1) {
            var response = await Session.Client.MoveToActionPointAsync(
                new MoveToActionPointRequestArgs(Id, speed, "default", orientationId, null!, safe, linear, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to action point orientation failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Moves the robot into orientation of action point. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="orientation">The orientation.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointOrientationAsync(OrientationManager orientation, string? armId = null,
            bool safe = true, bool linear = false, decimal speed = 1) =>
            await MoveToActionPointOrientationAsync(orientation.Id, "default", armId, safe, linear, speed);

        /// <summary>
        ///     Moves the robot into orientation of action point.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="orientationId">The orientation ID.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointOrientationAsync(string orientationId, string endEffectorId,
            string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) {
            var response = await Session.Client.MoveToActionPointAsync(
                new MoveToActionPointRequestArgs(Id, speed, endEffectorId, orientationId, null!, safe, linear, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to action point orientation failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Moves the robot into orientation of action point.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="orientation">The orientation.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointOrientationAsync(OrientationManager orientation, string endEffectorId,
            string? armId = null, bool safe = true, bool linear = false, decimal speed = 1) =>
            await MoveToActionPointOrientationAsync(orientation.Id, endEffectorId, armId, safe, linear, speed);

        /// <summary>
        ///     Moves the robot into orientation of action point.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="orientationId">The orientation ID.</param>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointOrientationAsync(string orientationId, EndEffector endEffector,
            bool safe = true, bool linear = false, decimal speed = 1) =>
            await MoveToActionPointOrientationAsync(orientationId, endEffector.Id, endEffector.ArmId, safe, linear,
                speed);

        /// <summary>
        ///     Moves the robot into orientation of action point.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="orientation">The orientation.</param>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointOrientationAsync(OrientationManager orientation, EndEffector endEffector,
            bool safe = true, bool linear = false, decimal speed = 1) =>
            await MoveToActionPointOrientationAsync(orientation.Id, endEffector.Id, endEffector.ArmId, safe, linear,
                speed);

        /// <summary>
        ///     Moves the robot into joints of action point.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>MoveToJoints</c> feature
        ///     flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="jointsId">The joints ID.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(string jointsId, string endEffectorId, string? armId = null,
            bool safe = true, bool linear = false, decimal speed = 1) {
            var response = await Session.Client.MoveToActionPointAsync(
                new MoveToActionPointRequestArgs(Id, speed, endEffectorId, null!, jointsId, safe, linear, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Moving robot {Id} to action point joints failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Moves the robot into joints of action point.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>MoveToJoints</c> feature
        ///     flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The joints.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(JointsManager joints, string endEffectorId, string? armId = null,
            bool safe = true, bool linear = false, decimal speed = 1) =>
            await MoveToActionPointJointsAsync(joints.Id, endEffectorId, armId, safe, linear, speed);

        /// <summary>
        ///     Moves the robot into joints of action point.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>MoveToJoints</c> feature
        ///     flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="jointsId">The joints ID.</param>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(string jointsId, EndEffector endEffector, bool safe = true,
            bool linear = false, decimal speed = 1) =>
            await MoveToActionPointJointsAsync(jointsId, endEffector.Id, endEffector.ArmId, safe, linear, speed);

        /// <summary>
        ///     Moves the robot into joints of action point.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>MoveToJoints</c> feature
        ///     flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The joints.</param>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(JointsManager joints, EndEffector endEffector, bool safe = true,
            bool linear = false, decimal speed = 1) => await MoveToActionPointJointsAsync(joints.Id, endEffector.Id,
            endEffector?.ArmId, safe, linear, speed);

        /// <summary>
        ///     Moves the robot into joints of action point. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>MoveToJoints</c> feature
        ///     flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="jointsId">The joints ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(string jointsId, string? armId = null, bool safe = true,
            bool linear = false, decimal speed = 1) =>
            await MoveToActionPointJointsAsync(jointsId, "default", armId, safe, linear, speed);

        /// <summary>
        ///     Moves the robot into joints of action point. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>MoveToJoints</c> feature
        ///     flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The joints.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task MoveToActionPointJointsAsync(JointsManager joints, string? armId = null, bool safe = true,
            bool linear = false, decimal speed = 1) =>
            await MoveToActionPointJointsAsync(joints.Id, "default", armId, safe, linear, speed);

        /// <summary>
        ///     Sets the robot's end effector perpendicular to world.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SetEndEffectorPerpendicularToWorldAsync(string endEffectorId, string? armId = null,
            bool safe = true, bool linear = false, decimal speed = 1) {
            await LibraryLockAsync();
            var response = await Session.Client.SetEndEffectorPerpendicularToWorldAsync(
                new SetEefPerpendicularToWorldRequestArgs(Id, endEffectorId, safe, speed, linear, armId!));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Setting robot {Id} eef perpendicular to world failed.", response.Messages);
            }

            await LibraryUnlockAsync();
        }

        /// <summary>
        ///     Sets the robot's end effector perpendicular to world.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="endEffector">The end effector. By default, <c>"default"</c>.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SetEndEffectorPerpendicularToWorldAsync(EndEffector endEffector, bool safe = true,
            bool linear = false, decimal speed = 1) =>
            await SetEndEffectorPerpendicularToWorldAsync(endEffector.Id, endEffector.ArmId, safe, linear, speed);

        /// <summary>
        ///     Sets the robot's end effector perpendicular to world. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="safe">Signifies if the movement be verifies as safe. By default, <c>"true"</c>.</param>
        /// <param name="linear">Signifies if the movement should be linear. By default, <c>"false"</c></param>
        /// <param name="speed">The speed in 0..1 interval.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SetEndEffectorPerpendicularToWorldAsync(string? armId = null, bool safe = true,
            bool linear = false, decimal speed = 1) =>
            await SetEndEffectorPerpendicularToWorldAsync("default", armId, safe, linear, speed);

        /// <summary>
        ///     Enables or disables hand teaching mode for the robot and optionally its arm.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>HandTeaching</c> feature
        ///     flag in object type's robot meta of this action object.
        ///     The robot must not be moving.
        /// </remarks>
        /// <param name="enable">Should hand teaching mode be enabled?</param>
        /// <param name="armId">The optional arm ID.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task SetHandTeachingModeAsync(bool enable = true, string? armId = null) {
            await LibraryLockAsync();
            var response =
                await Session.Client.SetHandTeachingModeAsync(new HandTeachingModeRequestArgs(Id, enable, armId!));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Setting hand teaching mode for robot {Id} failed.", response.Messages);
            }

            await LibraryUnlockAsync();
        }

        /// <summary>
        ///     Calculates the forwards kinematics for the robot and optionally its arm. Uses default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The joints and their values.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync(IList<Joint> joints, string? armId = null) =>
            await GetForwardKinematicsAsync("default", joints, armId);

        /// <summary>
        ///     Calculates the forwards kinematics for the robot and optionally its arm.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The joints and their values.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync(string endEffectorId, IList<Joint> joints,
            string? armId = null) {
            var response = await Session.Client.ForwardKinematicsAsync(new ForwardKinematicsRequestArgs(Id,
                endEffectorId, joints.Select(j => j.ToOpenApiJointObject()).ToList(), armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Getting forward kinematics for robot {Id} failed.", response.Messages);
            }

            return response.Data;
        }

        /// <summary>
        ///     Calculates the forwards kinematics for the robot and optionally its arm.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="joints">The joints and their values.</param>
        /// <param name="endEffector">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync(EndEffector endEffector, IList<Joint> joints) =>
            await GetForwardKinematicsAsync(endEffector.Id, joints, endEffector.ArmId);

        /// <summary>
        ///     Calculates the forwards kinematics using the current joint values.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync(string endEffectorId, string? armId = null) {
            var joints = Data.Joints!.Select(j => j.ToOpenApiJointObject()).ToList();
            var response =
                await Session.Client.ForwardKinematicsAsync(
                    new ForwardKinematicsRequestArgs(Id, endEffectorId, joints, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Getting forward kinematics for robot {Id} failed.", response.Messages);
            }

            return response.Data;
        }

        /// <summary>
        ///     Calculates the forwards kinematics using the current joint values. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync() => await GetForwardKinematicsAsync("default");

        /// <summary>
        ///     Calculates the forwards kinematics using the current joint values. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>ForwardKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns>The calculated pose.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<Pose> GetForwardKinematicsAsync(EndEffector endEffector) =>
            await GetForwardKinematicsAsync(endEffector.Id, endEffector.ArmId);

        /// <summary>
        ///     Calculates the inverse kinematics using the current pose and joints.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>> GetInverseKinematicsAsync(string endEffectorId, bool avoidCollisions = true,
            string? armId = null) {
            var pose = Data.EefPoses!.First(e => e.Id == endEffectorId).Pose!;
            var joints = Data.Joints!.Select(j => j.ToOpenApiJointObject()).ToList();
            var response = await Session.Client.InverseKinematicsAsync(
                new InverseKinematicsRequestArgs(Id, endEffectorId, pose, joints, avoidCollisions, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Getting inverse kinematics for robot {Id} failed.", response.Messages);
            }

            return response.Data.Select(j => j.MapToCustomJointObject()).ToList();
        }

        /// <summary>
        ///     Calculates the inverse kinematics using the current pose and joints. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>> GetInverseKinematicsAsync(bool avoidCollisions = true, string? armId = null) =>
            await GetInverseKinematicsAsync("default", avoidCollisions, armId);

        /// <summary>
        ///     Calculates the inverse kinematics using the current pose and joints.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>>
            GetInverseKinematicsAsync(EndEffector endEffector, bool avoidCollisions = true) =>
            await GetInverseKinematicsAsync(endEffector.Id, avoidCollisions, endEffector.ArmId);

        /// <summary>
        ///     Calculates the inverse kinematics.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="pose">THe pose for which to calculate inverse kinematics.</param>
        /// <param name="startingJoints">
        ///     The starting joint values to help IK solver to converge to close solution. By default,
        ///     uses the current joint values.
        /// </param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>> GetInverseKinematicsAsync(string endEffectorId, Pose pose,
            IList<Joint>? startingJoints = null, bool avoidCollisions = true, string? armId = null) {
            var joints = (startingJoints ?? Data.Joints)?.Select(j => j.ToOpenApiJointObject()).ToList();
            var response = await Session.Client.InverseKinematicsAsync(
                new InverseKinematicsRequestArgs(Id, endEffectorId, pose, joints!, avoidCollisions, armId!));
            if(!response.Result) {
                throw new Arcor2Exception($"Getting inverse kinematics for robot {Id} failed.", response.Messages);
            }

            return response.Data.Select(j => j.MapToCustomJointObject()).ToList();
        }

        /// <summary>
        ///     Calculates the inverse kinematics.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="pose">THe pose for which to calculate inverse kinematics.</param>
        /// <param name="startingJoints">
        ///     The starting joint values to help IK solver to converge to close solution. By default,
        ///     uses the current joint values.
        /// </param>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>> GetInverseKinematicsAsync(EndEffector endEffector, Pose pose,
            IList<Joint>? startingJoints = null, bool avoidCollisions = true) =>
            await GetInverseKinematicsAsync(endEffector.Id, pose, startingJoints, avoidCollisions, endEffector.ArmId);

        /// <summary>
        ///     Calculates the inverse kinematics. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature. See <c>InverseKinematics</c>
        ///     feature flag in object type's robot meta of this action object.
        /// </remarks>
        /// <param name="pose">THe pose for which to calculate inverse kinematics.</param>
        /// <param name="startingJoints">
        ///     The starting joint values to help IK solver to converge to close solution. By default,
        ///     uses the current joint values.
        /// </param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="avoidCollisions">Should the calculation avoid collisions?</param>
        /// <returns>The calculated joints and their values.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<Joint>> GetInverseKinematicsAsync(Pose pose, IList<Joint>? startingJoints = null,
            bool avoidCollisions = true) =>
            await GetInverseKinematicsAsync("default", pose, startingJoints, avoidCollisions);

        /// <summary>
        ///     Calibrates the robot.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object must be a robot.
        /// </remarks>
        /// <param name="cameraActionObjectId">The ID of the camera action object.</param>
        /// <param name="moveToCalibrationPose">Should the robot move to the calibration pose? By default, <c>true</c>.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CalibrateRobotAsync(string cameraActionObjectId, bool moveToCalibrationPose = true) {
            await LibraryLockAsync();
            var response =
                await Session.Client.CalibrateRobotAsync(new CalibrateRobotRequestArgs(Id, cameraActionObjectId,
                    moveToCalibrationPose));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Calibrating robot {Id} failed.", response.Messages);
            }

            await LibraryUnlockAsync();
        }

        /// <summary>
        ///     Calibrates the robot.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object must be a robot.
        /// </remarks>
        /// <param name="cameraActionObject">The camera action object.</param>
        /// <param name="moveToCalibrationPose">Should the robot move to the calibration pose? By default, <c>true</c>.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task
            CalibrateRobotAsync(ActionObjectManager cameraActionObject, bool moveToCalibrationPose = true) =>
            await CalibrateRobotAsync(cameraActionObject.Id, moveToCalibrationPose);

        /// <summary>
        ///     Calibrates the camera.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object must be of the Camera type.
        /// </remarks>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CalibrateCameraAsync() {
            await LibraryLockAsync();
            var response = await Session.Client.CalibrateCameraAsync(new CalibrateCameraRequestArgs(Id));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Calibrating camera {Id} failed.", response.Messages);
            }

            await LibraryUnlockAsync();
        }

        /// <summary>
        ///     Retrieves camera color image.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object must be of the Camera type.
        /// </remarks>
        /// <returns>
        ///     Raw byte array representing the image in JPEG format.
        /// </returns>
        /// <exception cref="Arcor2Exception"></exception>
        [Obsolete("Not implemented as of ARServer 1.5.0.")]
        public async Task<byte[]> GetCameraColorImageAsync() {
            await LibraryLockAsync();
            var response = await Session.Client.GetCameraColorImageAsync(new CameraColorImageRequestArgs(Id));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Getting camera color image {Id} failed.", response.Messages);
            }

            await LibraryUnlockAsync();

            var imageBytes = Encoding.GetEncoding("iso-8859-1").GetBytes(response.Data);
            return imageBytes;
        }

        /// <summary>
        ///     Retrieves camera color intrinsic parameters.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object must be of the Camera type.
        /// </remarks>
        /// <returns>A <see cref="CameraParameters" /> object containing the intrinsic parameters of the camera's color sensor.</returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<CameraParameters> GetCameraColorParametersAsync() {
            await LibraryLockAsync();
            var response = await Session.Client.GetCameraColorParametersAsync(new CameraColorParametersRequestArgs(Id));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Getting camera color parameters {Id} failed.", response.Messages);
            }

            await LibraryUnlockAsync();
            return response.Data;
        }

        /// <summary>
        ///     Renames an action object.
        /// </summary>
        /// <param name="newName">New name.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RenameAsync(string newName) {
            await LibraryLockAsync();
            var response = await Session.Client.RenameActionObjectAsync(new RenameArgs(Id, newName));
            if(!response.Result) {
                throw new Arcor2Exception($"Renaming action object {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Updates parameters of an action object.
        /// </summary>
        /// <param name="parameters">The new list of parameters.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateParametersAsync(ICollection<Parameter> parameters) {
            await LibraryLockAsync();
            var response =
                await Session.Client.UpdateActionObjectParametersAsync(
                    new UpdateObjectParametersRequestArgs(Id, parameters.ToList()));
            if(!response.Result) {
                await TryLibraryUnlockAsync();
                throw new Arcor2Exception($"Updating parameters of action object {Id} failed.", response.Messages);
            }

            await LibraryUnlockAsync();
        }

        /// <summary>
        ///     Updates an existing parameter of an action object.
        /// </summary>
        /// <param name="parameter">The modified parameter.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdateParameterAsync(Parameter parameter) {
            var newParameters = Data.Meta.Parameters
                .Where(p => p.Name != parameter.Name && p.Type != parameter.Type)
                .Append(parameter)
                .ToList();
            await UpdateParametersAsync(newParameters);
        }

        /// <summary>
        ///     Adds a new parameter to an action object.
        /// </summary>
        /// <param name="parameter">The new parameter.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddParameterAsync(Parameter parameter) =>
            await UpdateParametersAsync(Data.Meta.Parameters.Append(parameter).ToList());

        /// <summary>
        ///     Removes a parameter from an action object.
        /// </summary>
        /// <param name="parameter">The parameter to remove.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RemoveParameterAsync(Parameter parameter) {
            var newParameters = Data.Meta.Parameters
                .Where(p => p.Name != parameter.Name && p.Type != parameter.Type)
                .ToList();
            await UpdateParametersAsync(newParameters);
        }

        /// <summary>
        ///     Removes a parameter from an action object.
        /// </summary>
        /// <param name="parameterName">The name of parameter to remove.</param>
        /// <exception cref="Arcor2Exception"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task RemoveParameterAsync(string parameterName) {
            var param = Data.Meta.Parameters.FirstOrDefault(p => p.Name == parameterName) ??
                        throw new InvalidOperationException(
                            $"The parameter with name '{parameterName}' was not found in the client data.");

            await RemoveParameterAsync(param);
        }

        /// <summary>
        ///     Gets possible values of action object parameters.
        /// </summary>
        /// <remarks>
        ///     This RPC should only be called if the parameter has <c>DynamicValue</c> flag in its metadata
        ///     Values of parent parameters (also listed in metadata) should be provided.
        /// </remarks>
        /// <returns>The possible values for the parameter.</returns>
        /// <param name="parameter">The parameter.</param>
        /// <param name="parentParameters">The parent parameters</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<string>> GetParameterValuesAsync(Parameter parameter,
            IList<Parameter>? parentParameters = null) {
            parentParameters ??= new List<Parameter>();
            var response = await Session.Client.GetActionParameterValuesAsync(
                new ActionParamValuesRequestArgs(Id, parameter.Name,
                    parentParameters.Select(s => s.ToIdValue()).ToList()));
            if(!response.Result) {
                throw new Arcor2Exception($"Getting parameter values for action object {Id} failed.",
                    response.Messages);
            }

            return response.Data;
        }

        /// <summary>
        ///     Gets possible values of action object parameters.
        /// </summary>
        /// <remarks>
        ///     This RPC should only be called if the parameter has <c>DynamicValue</c> flag in its metadata
        ///     Values of parent parameters (also listed in metadata) should be provided.
        /// </remarks>
        /// <returns>The possible values for the parameter.</returns>
        /// <param name="parameter">The parameter name.</param>
        /// <param name="parentParameters">The parent parameters</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task<IList<string>> GetParameterValuesAsync(string parameter,
            IList<Parameter>? parentParameters = null) {
            parentParameters ??= new List<Parameter>();
            var response = await Session.Client.GetActionParameterValuesAsync(
                new ActionParamValuesRequestArgs(Id, parameter, parentParameters.Select(s => s.ToIdValue()).ToList()));
            if(!response.Result) {
                throw new Arcor2Exception($"Getting parameter values for action object {Id} failed.",
                    response.Messages);
            }

            return response.Data;
        }

        /// <summary>
        ///     Stops the current movement action of a robot.
        /// </summary>
        /// <remarks>
        ///     The scene must be online and the action object a robot with corresponding feature.
        /// </remarks>
        public async Task StopAsync() {
            var response = await Session.Client.StopRobotAsync(new StopRobotRequestArgs(Id));
            if(!response.Result) {
                throw new Arcor2Exception($"Stopping robot {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Updates the pose of the action object using robot's end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online.
        /// </remarks>
        /// <param name="robotId">The robot ID.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="pivot">The pivot.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdatePoseUsingRobotAsync(string robotId, string endEffectorId, string? armId = null,
            Pivot pivot = Pivot.Middle) {
            var response = await Session.Client.UpdateObjectPoseUsingRobotAsync(
                new UpdateObjectPoseUsingRobotRequestArgs(Id, new RobotArg(robotId, endEffectorId, armId!),
                    pivot.MapToOpenApiPivotEnum()));
            if(!response.Result) {
                throw new Arcor2Exception($"Updating pose of action object {Id} using robot failed.",
                    response.Messages);
            }
        }

        /// <summary>
        ///     Updates the pose of the action object using robot's end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online.
        /// </remarks>
        /// <param name="robot">The robot.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="pivot">The pivot.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdatePoseUsingRobotAsync(ActionObjectManager robot, string endEffectorId,
            string? armId = null, Pivot pivot = Pivot.Middle) =>
            await UpdatePoseUsingRobotAsync(robot.Id, endEffectorId, armId, pivot);

        /// <summary>
        ///     Updates the pose of the action object using robot's end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online.
        /// </remarks>
        /// <param name="robot">The robot.</param>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="pivot">The pivot.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdatePoseUsingRobotAsync(ActionObjectManager robot, EndEffector endEffector,
            string? armId = null, Pivot pivot = Pivot.Middle) =>
            await UpdatePoseUsingRobotAsync(robot.Id, endEffector.Id, armId, pivot);

        /// <summary>
        ///     Updates the pose of the action object using robot's end effector. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online.
        /// </remarks>
        /// <param name="robot">The robot.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="pivot">The pivot.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdatePoseUsingRobotAsync(ActionObjectManager robot, string? armId = null,
            Pivot pivot = Pivot.Middle) => await UpdatePoseUsingRobotAsync(robot.Id, "default", armId, pivot);

        /// <summary>
        ///     Updates the pose of the action object using robot's end effector. Uses the default end effector.
        /// </summary>
        /// <remarks>
        ///     The scene must be online.
        /// </remarks>
        /// <param name="robotId">The robot ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <param name="pivot">The pivot.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task UpdatePoseUsingRobotAsync(string robotId, string? armId = null, Pivot pivot = Pivot.Middle) =>
            await UpdatePoseUsingRobotAsync(robotId, "default", armId, pivot);

        /// <summary>
        ///     Starts an object aiming process for this action object.
        /// </summary>
        /// <remarks>
        ///     Before starting the process, a lock must be acquired manually (using the locking methods) for both the action
        ///     object and the robot.
        ///     In case of successful process, the robot and the action object must be unlocked.
        ///     On failure, it is possible to do another attempt or call <see cref="CancelObjectAimingAsync" />.
        ///     The scene must be online. The action object must have a pose and a mesh model with defined focus point.
        /// </remarks>
        /// <param name="robotId">The robot ID.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StartObjectAimingAsync(string robotId, string endEffectorId, string? armId = null) {
            var response =
                await Session.Client.ObjectAimingStartAsync(
                    new ObjectAimingStartRequestArgs(Id, new RobotArg(robotId, endEffectorId, armId!)));
            if(!response.Result) {
                throw new Arcor2Exception($"Starting object aiming process for action object {Id} failed.",
                    response.Messages);
            }
        }

        /// <summary>
        ///     Starts an object aiming process for this action object.
        /// </summary>
        /// <param name="robot">The robot.</param>
        /// <param name="endEffectorId">The end effector ID.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task
            StartObjectAimingAsync(ActionObjectManager robot, string endEffectorId, string? armId = null) =>
            await StartObjectAimingAsync(robot.Id, endEffectorId, armId);

        /// <summary>
        ///     Starts an object aiming process for this action object.
        /// </summary>
        /// <param name="robot">The robot.</param>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StartObjectAimingAsync(ActionObjectManager robot, EndEffector endEffector,
            string? armId = null) => await StartObjectAimingAsync(robot.Id, endEffector.Id, armId);

        /// <summary>
        ///     Starts an object aiming process for this action object.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="robotId">The robot ID.</param>
        /// <param name="endEffector">The end effector.</param>
        /// <param name="armId">The arm ID. By default, <c>null</c>.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StartObjectAimingAsync(string robotId, EndEffector endEffector, string? armId = null) =>
            await StartObjectAimingAsync(robotId, endEffector.Id, armId);

        /// <summary>
        ///     Starts an object aiming process for this action object. Uses the default end effector and arm.
        /// </summary>
        /// <param name="robotId">The robot ID.</param>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task StartObjectAimingAsync(string robotId) => await StartObjectAimingAsync(robotId, "default");

        /// <summary>
        ///     Cancels object aiming process for this action object.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task CancelObjectAimingAsync() {
            var response = await Session.Client.ObjectAimingCancelAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Cancelling object aiming process for action object {Id} failed.",
                    response.Messages);
            }
        }

        /// <summary>
        ///     Finishes object aiming process for this action object.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task FinishObjectAimingAsync() {
            var response = await Session.Client.ObjectAimingDoneAsync();
            if(!response.Result) {
                throw new Arcor2Exception($"Finishing object aiming process for action object {Id} failed.",
                    response.Messages);
            }
        }

        /// <summary>
        ///     Adds a point index for object aiming process for this action object.
        /// </summary>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task AddPointForObjectAimingAsync(int pointId) {
            var response = await Session.Client.ObjectAimingAddPointAsync(new ObjectAimingAddPointRequestArgs(pointId));
            if(!response.Result) {
                throw new Arcor2Exception(
                    $"Adding point index for object aiming process for action object {Id} failed.", response.Messages);
            }
        }

        /// <summary>
        ///     Reloads the arms and their end effector of the robot.
        /// </summary>
        /// <remarks>
        ///     This method is invoked automatically and should only be invoked
        ///     in edge cases (the library not being able to subscribe on registration
        ///     due to locked object type).
        /// </remarks>
        public async Task ReloadRobotArmsAndEefPoseAsync() {
            if(ObjectType.Data.RobotMeta?.MultiArm ?? false) {
                var armsResponse = await Session.Client.GetRobotArmsAsync(new GetRobotArmsRequestArgs(Id));
                // Do not throw, it may be single-armed... despite the property
                if(armsResponse.Result) {
                    Data.Arms = armsResponse.Data;
                }
            }

            var eefResponse = await Session.Client.GetRobotEndEffectorsAsync(new GetEndEffectorsRequestArgs(Id));
            if(!eefResponse.Result) {
                throw new Arcor2Exception($"Getting end effectors for action object {Id} failed.",
                    eefResponse.Messages);
            }

            var endEffectors = eefResponse.Data.Select(id => new EndEffector(id)).ToList();
            foreach(var endEffector in endEffectors) {
                var poseResponse =
                    await Session.Client.GetEndEffectorPoseAsync(
                        new GetEndEffectorPoseRequestArgs(Id, endEffector.Id, endEffector.ArmId!));
                if(!eefResponse.Result) {
                    throw new Arcor2Exception(
                        $"Could not get end effector '{endEffector.Id}' pose for action object {Id}.",
                        eefResponse.Messages);
                }

                endEffector.Pose = poseResponse.Data;
            }

            Data.EefPoses = endEffectors;

            OnUpdated();
        }

        /// <summary>
        ///     Reloads the joints and their values of the robot.
        /// </summary>
        /// <remarks>
        ///     This method is invoked automatically and should only be invoked
        ///     in edge cases (the library not being able to subscribe on registration
        ///     due to locked object type).
        /// </remarks>
        public async Task ReloadRobotJointsAsync() {
            var jointsResponse = await Session.Client.GetRobotJointsAsync(new GetRobotJointsRequestArgs(Id));
            if(jointsResponse.Result) {
                Data.Joints = jointsResponse.Data.Select(j => j.MapToCustomJointObject()).ToList();
                OnUpdated();
            }
        }

        /// <summary>
        ///     Register the robot for updates of eef pose/joints.
        /// </summary>
        /// <remarks>
        ///     This method is invoked automatically and should only be invoked
        ///     in edge cases (the library not being able to subscribe on registration
        ///     due to locked object type). The scene must be online, and the user registered.
        /// </remarks>
        /// <param name="type">The type of registration.</param>
        /// <param name="enabled">Toggle on/off.</param>
        /// <exception cref="Arcor2Exception"></exception>
        public async Task RegisterForUpdatesAsync(RobotUpdateType type, bool enabled = true) {
            var response = await Session.Client.RegisterForRobotEventAsync(
                new RegisterForRobotEventRequestArgs(Id, send: true, what: type.MapToOpenApiWhatEnum()));
            if(!response.Result) {
                throw new Arcor2Exception($"Registering for robot updates for action object {Id} failed.",
                    response.Messages);
            }
        }

        /// <summary>
        ///     Updates the action object according to the <paramref name="actionObject" /> instance.
        /// </summary>
        /// <param name="actionObject">Newer version of the action object.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// >
        internal void UpdateAccordingToNewObject(SceneObject actionObject) {
            if(Id != actionObject.Id) {
                throw new InvalidOperationException(
                    $"Can't update an ActionObjectManager ({Id}) using a action object data object ({actionObject.Id}) with different ID.");
            }

            Data.Meta = actionObject;
            OnUpdated();
        }

        protected override void RegisterHandlers() {
            base.RegisterHandlers();
            Session.Client.ActionObjectUpdated += OnActionObjectUpdated;
            Session.Client.ActionObjectRemoved += OnSceneActionObjectRemoved;
            Session.Client.RobotJointsUpdated += OnRobotJointsUpdated;
            Session.Client.RobotEndEffectorUpdated += OnRobotEndEffectorUpdated;
            Session.Client.ProcessState += OnProcessState;
            Session.Client.RobotMoveToPose += OnRobotMoveToPose;
            Session.Client.RobotMoveToJoints += OnRobotMoveToJoints;
            Session.Client.RobotMoveToActionPointJoints += OnRobotMoveToActionPointJoints;
            Session.Client.RobotMoveToActionPointOrientation += OnRobotMoveToActionPointOrientation;
        }

        protected override void UnregisterHandlers() {
            base.UnregisterHandlers();
            Session.Client.ActionObjectUpdated -= OnActionObjectUpdated;
            Session.Client.ActionObjectRemoved -= OnSceneActionObjectRemoved;
            Session.Client.RobotJointsUpdated -= OnRobotJointsUpdated;
            Session.Client.RobotEndEffectorUpdated -= OnRobotEndEffectorUpdated;
            Session.Client.ProcessState -= OnProcessState;
            Session.Client.RobotMoveToPose -= OnRobotMoveToPose;
            Session.Client.RobotMoveToJoints -= OnRobotMoveToJoints;
            Session.Client.RobotMoveToActionPointJoints -= OnRobotMoveToActionPointJoints;
            Session.Client.RobotMoveToActionPointOrientation -= OnRobotMoveToActionPointOrientation;
        }

        private void OnActionObjectUpdated(object sender, ActionObjectEventArgs e) {
            if(Id == e.Data.Id) {
                Data.Meta = e.Data;
                OnUpdated();
            }
        }

        private void OnSceneActionObjectRemoved(object sender, ActionObjectEventArgs e) {
            if(Id == e.Data.Id) {
                RemoveData();
                Scene.actionObjects?.Remove(this);
                Dispose();
            }
        }

        private void OnRobotEndEffectorUpdated(object sender, RobotEndEffectorUpdatedEventArgs e) {
            if(e.Data.RobotId == Id) {
                Data.EefPoses = e.Data.EndEffectors.Select(r => r.MapToCustomEndEffectorObject()).ToList();
                OnUpdated();
            }
        }

        private void OnRobotJointsUpdated(object sender, RobotJointsUpdatedEventArgs e) {
            if(Id == e.Data.RobotId) {
                Data.Joints = e.Data.Joints.Select(j => j.MapToCustomJointObject()).ToList();
                OnUpdated();
            }
        }

        private void OnProcessState(object sender, ProcessStateEventArgs e) {
            if(e.Data.Id == Id) {
                ProcessStateChanged?.Invoke(this,
                    new ProcessStateChangedEventArgs(e.Data.State.MapToCustomProcessStateEnum(), e.Data.Message));
            }
        }

        private void OnRobotMoveToActionPointOrientation(object sender, RobotMoveToActionPointOrientationEventArgs e) {
            if(e.Data.RobotId == Id) {
                // Get Manager
                var project = Session.NavigationState == NavigationState.Project
                    ? Session.Projects.FirstOrDefault(p => Session.NavigationId == p.Id)
                    : Session.Packages.FirstOrDefault(p => Session.NavigationId == p.Id)?.Project;
                var orientation = project?.ActionPoints?
                    .SelectMany(a => a.Orientations, (a, o) => o)
                    .FirstOrDefault(o => o.Id == e.Data.OrientationId);
                if(orientation == null) {
                    Session.Logger?.LogError(
                        $"Could not get OrientationManager ({e.Data.OrientationId}) for RobotMoveToActionPointOrientation.");
                    return;
                }

                MovingToActionPointOrientation?.Invoke(this, new RobotMovingToActionPointOrientationEventArgs(
                    e.Data.MoveEventType.MapToCustomRobotMoveTypeEnum(),
                    orientation,
                    e.Data.Safe,
                    e.Data.Message,
                    e.Data.ArmId,
                    e.Data.EndEffectorId));
            }
        }

        private void OnRobotMoveToActionPointJoints(object sender, RobotMoveToActionPointJointsEventArgs e) {
            if(e.Data.RobotId == Id) {
                // Get Manager
                var project = Session.NavigationState == NavigationState.Project
                    ? Session.Projects.FirstOrDefault(p => Session.NavigationId == p.Id)
                    : Session.Packages.FirstOrDefault(p => Session.NavigationId == p.Id)?.Project;
                var joints = project?.ActionPoints?
                    .SelectMany(a => a.Joints, (a, o) => o)
                    .FirstOrDefault(o => o.Id == e.Data.JointsId);
                if(joints == null) {
                    Session.Logger?.LogError(
                        $"Could not get JointsManager ({e.Data.JointsId}) for RobotMoveToActionPointJoints.");
                    return;
                }

                MovingToActionPointJoints?.Invoke(this, new RobotMovingToActionPointJointsEventArgs(
                    e.Data.MoveEventType.MapToCustomRobotMoveTypeEnum(),
                    joints,
                    e.Data.Safe,
                    e.Data.Message,
                    e.Data.ArmId));
            }
        }

        private void OnRobotMoveToJoints(object sender, RobotMoveToJointsEventArgs e) {
            if(e.Data.RobotId == Id) {
                MovingToJoints?.Invoke(this, new RobotMovingToJointsEventArgs(
                    e.Data.MoveEventType.MapToCustomRobotMoveTypeEnum(),
                    e.Data.TargetJoints.Select(j => j.MapToCustomJointObject()).ToList(),
                    e.Data.Safe,
                    e.Data.Message,
                    e.Data.ArmId));
            }
        }

        private void OnRobotMoveToPose(object sender, RobotMoveToPoseEventArgs e) {
            if(e.Data.RobotId == Id) {
                MovingToPose?.Invoke(this, new RobotMovingToPoseEventArgs(
                    e.Data.MoveEventType.MapToCustomRobotMoveTypeEnum(),
                    e.Data.EndEffectorId,
                    e.Data.TargetPose,
                    e.Data.Safe,
                    e.Data.Linear,
                    e.Data.Message,
                    e.Data.ArmId));
            }
        }
    }
}