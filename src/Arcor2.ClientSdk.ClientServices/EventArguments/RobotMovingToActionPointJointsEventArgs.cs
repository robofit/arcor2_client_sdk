using System;
using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Managers;

namespace Arcor2.ClientSdk.ClientServices.EventArguments
{
    public class RobotMovingToActionPointJointsEventArgs : EventArgs
    {
        /// <summary>
        /// The type of move event.
        /// </summary>
        public RobotMoveType MoveEventType { get; set; }
        /// <summary>
        /// The target action point joints.
        /// </summary>
        public JointsManager TargetJoints { get; set; }
        /// <summary>
        /// The target joints ID.
        /// </summary>
        public string TargetJointsId { get; set; }
        /// <summary>
        /// Is the movement safe?
        /// </summary>
        public bool Safe { get; set; }
        /// <summary>
        /// The optional message.
        /// </summary>
        public string? Message { get; set; }
        /// <summary>
        /// The arm ID.
        /// </summary>
        public string? ArmId { get; set; }

        public RobotMovingToActionPointJointsEventArgs(RobotMoveType moveEventType, JointsManager targetJoints, bool safe, string?
            message, string? armId)
        {
            MoveEventType = MoveEventType;
            TargetJoints = targetJoints;
            TargetJointsId = targetJoints.Id;
            Safe = safe;
            Message = message;
            ArmId = armId;
        }
    }
}