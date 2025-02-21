using System.Collections.Generic;
using System.Linq;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models.Extras {
    public class MeshCollisionModel : CollisionModel {
        public string AssetId { get; set; }
        public List<Pose> Points { get; set; }

        public MeshCollisionModel(string assetId, ICollection<Pose> points) {
            AssetId = assetId;
            Points = points.ToList();
        }
        internal override ObjectModel ToOpenApiObjectModel(string id) {
            return new ObjectModel(ObjectModel.TypeEnum.Mesh, mesh: new Mesh(id, AssetId, Points));
        }
    }
}
