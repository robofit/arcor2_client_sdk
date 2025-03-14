using Arcor2.ClientSdk.Communication.OpenApi.Models;
using System.Collections.Generic;
using Action = Arcor2.ClientSdk.Communication.OpenApi.Models.Action;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    internal static class ActionExtensions {
        public static BareAction MapToBareAction(this Action action) =>
            new BareAction(action.Name, action.Type, action.Id);

        public static Action MapToAction(this BareAction action) => new Action(action.Name, action.Type, action.Id,
            new List<ActionParameter>(), new List<Flow>());
    }
}