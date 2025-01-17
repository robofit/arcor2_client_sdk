using System.Collections.Generic;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Action = Arcor2.ClientSdk.Communication.OpenApi.Models.Action;

namespace Arcor2.ClientSdk.Communication.Extensions {
    public static class ActionPointExtensions {
        public static ActionPoint ToActionPoint(this BareActionPoint bareActionPoint) {
            return new ActionPoint(bareActionPoint.Name, bareActionPoint.Position, bareActionPoint.Parent,
                bareActionPoint.DisplayName, bareActionPoint.Description, bareActionPoint.Id,
                new List<NamedOrientation>(),
                new List<ProjectRobotJoints>(),
                new List<Action>());
        }

        public static BareActionPoint ToBareActionPoint(this ActionPoint actionPoint) {
            return new BareActionPoint(actionPoint.Name, actionPoint.Position, actionPoint.Parent,
                actionPoint.DisplayName, actionPoint.Description, actionPoint.Id);
        }
    }
}
