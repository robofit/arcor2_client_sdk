using System;
using Arcor2.ClientSdk.ClientServices.Extensions;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    public class ActionPointManager : LockableArcor2ObjectManager {
        internal ProjectManager Project { get; }
        public BareActionPoint Meta { get; private set; }

        public ActionPointManager(Arcor2Session session, ProjectManager project, BareActionPoint actionPointMeta) : base(
            session, actionPointMeta.Id) {
            Project = project;
            Meta = actionPointMeta;
        }

        public ActionPointManager(Arcor2Session session, ProjectManager project, ActionPoint actionPoint) : base(
            session, actionPoint.Id) {
            Project = project;
            Meta = actionPoint.MapToBareActionPoint();

            // TODO: Rest
        }

        /// <summary>
        /// Updates the action point according to the <paramref name="actionPoint"/> instance.
        /// </summary>
        /// <param name="actionPoint">Newer version of the action point.</param>
        /// <exception cref="InvalidOperationException"></exception>>
        internal void UpdateAccordingToNewObject(ActionPoint actionPoint) {
            if(Id != actionPoint.Id) {
                throw new InvalidOperationException($"Can't update an ActionPointManager ({Id}) using a action point data object ({actionPoint.Id}) with different ID.");
            }
            // TODO: Rest
        }
    }
}
