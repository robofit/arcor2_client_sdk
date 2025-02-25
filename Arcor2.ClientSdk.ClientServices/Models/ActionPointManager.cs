using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    public class ActionPointManager : Arcor2ObjectManager {
        internal ProjectManager Project { get; }
        public BareActionPoint Meta { get; private set; }

        public ActionPointManager(Arcor2Session session, ProjectManager project, BareActionPoint actionPointMeta) : base(
            session, actionPointMeta.Id) {
            Project = project;
            Meta = actionPointMeta;
        }
    }
}
