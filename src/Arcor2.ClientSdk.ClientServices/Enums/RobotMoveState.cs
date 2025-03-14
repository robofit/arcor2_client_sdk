using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    ///     Represents a state of a robot movement event.
    /// </summary>
    public enum RobotMoveType {
        /// <summary>
        ///     The movement task has started and the robot is moving.
        /// </summary>
        Started = 0,

        /// <summary>
        ///     The movement task has finished.
        /// </summary>
        Finished,

        /// <summary>
        ///     The movement task has failed.
        /// </summary>
        Failed
    }

    internal static class MoveEventTypeEnumExtensions {
        public static RobotMoveType MapToCustomRobotMoveTypeEnum(
            this RobotMoveToActionPointJointsData.MoveEventTypeEnum type) =>
            type switch {
                RobotMoveToActionPointJointsData.MoveEventTypeEnum.Start => RobotMoveType.Started,
                RobotMoveToActionPointJointsData.MoveEventTypeEnum.End => RobotMoveType.Finished,
                RobotMoveToActionPointJointsData.MoveEventTypeEnum.Failed => RobotMoveType.Failed,
                _ => throw new InvalidOperationException(
                    "Invalid RobotMoveToActionPointJointsData.MoveEventTypeEnum value.")
            };

        public static RobotMoveType MapToCustomRobotMoveTypeEnum(
            this RobotMoveToActionPointOrientationData.MoveEventTypeEnum type) =>
            type switch {
                RobotMoveToActionPointOrientationData.MoveEventTypeEnum.Start => RobotMoveType.Started,
                RobotMoveToActionPointOrientationData.MoveEventTypeEnum.End => RobotMoveType.Finished,
                RobotMoveToActionPointOrientationData.MoveEventTypeEnum.Failed => RobotMoveType.Failed,
                _ => throw new InvalidOperationException(
                    "Invalid RobotMoveToActionPointOrientationData.MoveEventTypeEnum value.")
            };

        public static RobotMoveType MapToCustomRobotMoveTypeEnum(this RobotMoveToJointsData.MoveEventTypeEnum type) =>
            type switch {
                RobotMoveToJointsData.MoveEventTypeEnum.Start => RobotMoveType.Started,
                RobotMoveToJointsData.MoveEventTypeEnum.End => RobotMoveType.Finished,
                RobotMoveToJointsData.MoveEventTypeEnum.Failed => RobotMoveType.Failed,
                _ => throw new InvalidOperationException("Invalid RobotMoveToJointsData.MoveEventTypeEnum value.")
            };

        public static RobotMoveType MapToCustomRobotMoveTypeEnum(this RobotMoveToPoseData.MoveEventTypeEnum type) =>
            type switch {
                RobotMoveToPoseData.MoveEventTypeEnum.Start => RobotMoveType.Started,
                RobotMoveToPoseData.MoveEventTypeEnum.End => RobotMoveType.Finished,
                RobotMoveToPoseData.MoveEventTypeEnum.Failed => RobotMoveType.Failed,
                _ => throw new InvalidOperationException("Invalid RobotMoveToPoseData.MoveEventTypeEnum value.")
            };
    }
}