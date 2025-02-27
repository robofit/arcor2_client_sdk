using Arcor2.ClientSdk.ClientServices.Models.Extras;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    internal static class JointsExtensions {
        public static Joint MapToCustomJointObject(this Communication.OpenApi.Models.Joint joint) {
            return new Joint(joint.Name, joint.Value);
        }
    }
}
