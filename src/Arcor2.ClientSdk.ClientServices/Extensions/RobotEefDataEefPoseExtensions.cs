using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    internal static class RobotEefDataEefPoseExtensions {
        public static EndEffector MapToCustomEndEffectorObject(this RobotEefDataEefPose eefData) =>
            new EndEffector(eefData.EndEffectorId, eefData.Pose, eefData.ArmId);
    }
}