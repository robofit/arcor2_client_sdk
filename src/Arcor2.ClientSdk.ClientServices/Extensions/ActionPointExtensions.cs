using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    public static class ActionPointExtensions {
        public static BareActionPoint MapToBareActionPoint(this ActionPoint actionPoint) => new BareActionPoint(
            actionPoint.Name, actionPoint.Position, actionPoint.Parent, actionPoint.DisplayName,
            actionPoint.Description, actionPoint.Id);
    }
}