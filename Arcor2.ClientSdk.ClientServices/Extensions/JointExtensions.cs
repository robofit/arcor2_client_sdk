using Joint = Arcor2.ClientSdk.ClientServices.Models.Joint;

namespace Arcor2.ClientSdk.ClientServices.Extensions
{
    internal static class JointExtensions {
        public static Joint MapToCustomJointObject(this Communication.OpenApi.Models.Joint joint) {
            return new Joint(joint.Name, joint.Value);
        }
    }
}
