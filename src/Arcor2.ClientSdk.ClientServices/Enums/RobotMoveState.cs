using System;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents a type of robot movement event.
    /// </summary>
    public enum RobotMoveType {
        Started = 0,
        Finished,
        Failed
    }

    internal static class MoveEventTypeEnumExtensions {
        public static RobotMoveType MapToCustomRobotMoveTypeEnum(this RobotMoveToActionPointJointsData.MoveEventTypeEnum type) {
            return type switch {
                RobotMoveToActionPointJointsData.MoveEventTypeEnum.Start => RobotMoveType.Started,
                RobotMoveToActionPointJointsData.MoveEventTypeEnum.End => RobotMoveType.Finished,
                RobotMoveToActionPointJointsData.MoveEventTypeEnum.Failed => RobotMoveType.Failed,
                _ => throw new InvalidOperationException("Invalid RobotMoveToActionPointJointsData.MoveEventTypeEnum value.")
            };
        }
        public static RobotMoveType MapToCustomRobotMoveTypeEnum(this RobotMoveToActionPointOrientationData.MoveEventTypeEnum type) {
            return type switch {
                RobotMoveToActionPointOrientationData.MoveEventTypeEnum.Start => RobotMoveType.Started,
                RobotMoveToActionPointOrientationData.MoveEventTypeEnum.End => RobotMoveType.Finished,
                RobotMoveToActionPointOrientationData.MoveEventTypeEnum.Failed => RobotMoveType.Failed,
                _ => throw new InvalidOperationException("Invalid RobotMoveToActionPointOrientationData.MoveEventTypeEnum value.")
            };
        }
        public static RobotMoveType MapToCustomRobotMoveTypeEnum(this RobotMoveToJointsData.MoveEventTypeEnum type) {
            return type switch {
                RobotMoveToJointsData.MoveEventTypeEnum.Start => RobotMoveType.Started,
                RobotMoveToJointsData.MoveEventTypeEnum.End => RobotMoveType.Finished,
                RobotMoveToJointsData.MoveEventTypeEnum.Failed => RobotMoveType.Failed,
                _ => throw new InvalidOperationException("Invalid RobotMoveToJointsData.MoveEventTypeEnum value.")
            };
        }
        public static RobotMoveType MapToCustomRobotMoveTypeEnum(this RobotMoveToPoseData.MoveEventTypeEnum type) {
            return type switch {
                RobotMoveToPoseData.MoveEventTypeEnum.Start => RobotMoveType.Started,
                RobotMoveToPoseData.MoveEventTypeEnum.End => RobotMoveType.Finished,
                RobotMoveToPoseData.MoveEventTypeEnum.Failed => RobotMoveType.Failed,
                _ => throw new InvalidOperationException("Invalid RobotMoveToPoseData.MoveEventTypeEnum value.")
            };
        }
    }
}
