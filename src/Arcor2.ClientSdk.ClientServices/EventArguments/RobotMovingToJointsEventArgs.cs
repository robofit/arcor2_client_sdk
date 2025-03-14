using Arcor2.ClientSdk.ClientServices.Enums;
using Arcor2.ClientSdk.ClientServices.Models;
using System;
using System.Collections.Generic;

namespace Arcor2.ClientSdk.ClientServices.EventArguments {
    public class RobotMovingToJointsEventArgs : EventArgs {
        public RobotMovingToJointsEventArgs(RobotMoveType moveEventType, List<Joint> targetJoints, bool safe, string?
            message, string? armId) {
            MoveEventType = MoveEventType;
            TargetJoints = targetJoints;
            Safe = safe;
            Message = message;
            ArmId = armId;
        }

        /// <summary>
        ///     The type of move event.
        /// </summary>
        public RobotMoveType MoveEventType { get; set; }

        /// <summary>
        ///     The target joint values.
        /// </summary>
        public List<Joint> TargetJoints { get; set; }

        /// <summary>
        ///     Is the movement safe?
        /// </summary>
        public bool Safe { get; set; }

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