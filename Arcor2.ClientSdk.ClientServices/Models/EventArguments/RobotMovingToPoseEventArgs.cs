using System;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models.EventArguments {
    public class RobotMovingToPoseEventArgs : EventArgs {
        /// <summary>
        /// The type of move event.
        /// </summary>
        public RobotMoveType MoveEventType { get; set; }
        /// <summary>
        /// The end effector ID.
        /// </summary>
        public string? EndEffectorId { get; set; }
        /// <summary>
        /// The target pose.
        /// </summary>
        public Pose TargetPose { get; set; }
        /// <summary>
        /// Is the movement safe?
        /// </summary>
        public bool Safe { get; set; }
        /// <summary>
        /// Is the movement linear?
        /// </summary>
        public bool Linear { get; set; }
        /// <summary>
        /// The optional message.
        /// </summary>
        public string? Message { get; set; }
        /// <summary>
        /// The arm ID.
        /// </summary>
        public string? ArmId { get; set; }

        public RobotMovingToPoseEventArgs(RobotMoveType moveEventType, string? endEffectorId, Pose targetPose, bool safe, bool linear, string? message, string? armId) {
            MoveEventType = moveEventType;
            EndEffectorId = endEffectorId;
            TargetPose = targetPose;
            Safe = safe;
            Linear = linear;
            Message = message;
            ArmId = armId;
        }
    }
}
