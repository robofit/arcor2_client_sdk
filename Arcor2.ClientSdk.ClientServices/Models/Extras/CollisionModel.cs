using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models.Extras {
    public abstract class CollisionModel {
        internal abstract ObjectModel ToOpenApiObjectModel(string id);
    }
}
