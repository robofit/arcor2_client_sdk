using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models
{
    public abstract class CollisionModel
    {
        public abstract ObjectModel ToObjectModel(string id);
    }
}
