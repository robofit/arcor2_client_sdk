using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    public static class SceneExtensions {
        public static BareScene MapToBareScene(this Scene scene) {
            return new BareScene(scene.Name, scene.Description, scene.Created, scene.Modified, scene.IntModified,
                scene.Id);
        }
    }
}
