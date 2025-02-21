using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models.Extras {
    public class SphereCollisionModel : CollisionModel {
        public decimal Radius { get; set; }
        public SphereCollisionModel(decimal radius) {
            Radius = radius;
        }

        internal override ObjectModel ToOpenApiObjectModel(string id) {
            return new ObjectModel(ObjectModel.TypeEnum.Sphere, sphere: new Sphere(id, Radius));
        }
    }
}
