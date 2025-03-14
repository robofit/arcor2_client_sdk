using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Managers;
using System;

namespace Arcor2.ClientSdk.ClientServices.EventArguments {
    public class RobotMovingToActionPointOrientationEventArgs : EventArgs {
        public RobotMovingToActionPointOrientationEventArgs(RobotMoveType moveEventType,
            OrientationManager targetOrientation, bool safe, string?
                message, string? armId, string? endEffectorId) {
            MoveEventType = moveEventType;
            TargetOrientation = targetOrientation;
            TargetOrientationId = targetOrientation.Id;
            Safe = safe;
            Message = message;
            ArmId = armId;
            EndEffectorId = endEffectorId;
        }

        /// <summary>
        ///     The type of move event.
        /// </summary>
        public RobotMoveType MoveEventType { get; set; }

        /// <summary>
        ///     The end effector ID.
        /// </summary>
        public string? EndEffectorId { get; set; }

        /// <summary>
        ///     The target action point orientation.
        /// </summary>
        public OrientationManager TargetOrientation { get; set; }

        /// <summary>
        ///     The target orientation ID.
        /// </summary>
        public string TargetOrientationId { get; set; }

        /// <summary>
        ///     Is the movement safe?
        /// </summary>
        public bool Safe { get; set; }

        /// <summary>
        ///     Is the movement linear?
        /// </summary>
        public bool Linear { get; set; }

        /// <summary>
        ///     The optional message.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        ///     The arm ID.
        /// </summary>
        public string? ArmId { get; set; }
    }
}