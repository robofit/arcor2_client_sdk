using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Action = Arcor2.ClientSdk.Communication.OpenApi.Models.Action;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    internal static class ActionExtensions {
        public static BareAction MapToBareAction(this Action action) {
            return new BareAction(action.Name, action.Type, action.Id);
        }
    }
}
