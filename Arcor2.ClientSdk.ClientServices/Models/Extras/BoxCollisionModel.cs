using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models.Extras
{
    public class BoxCollisionModel : CollisionModel {
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Z { get; set; }

        public BoxCollisionModel(decimal x, decimal y, decimal z) {
            X = x; Y = y; Z = z;
        }

        internal override ObjectModel ToOpenApiObjectModel(string id) {
            return new ObjectModel(ObjectModel.TypeEnum.Box, new Box(id, X, Y, Z));
        }
    }
}
