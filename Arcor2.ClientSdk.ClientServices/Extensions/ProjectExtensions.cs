using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    public static class ProjectExtensions {
        public static BareProject MapToBareProject(this Project project) {
            return new BareProject(project.Name, project.SceneId, project.Description,project.HasLogic, project.Created, project.Modified, project.IntModified,
                project.Id);
        }
    }
}
