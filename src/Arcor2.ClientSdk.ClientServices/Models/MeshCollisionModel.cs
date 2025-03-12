using System.Collections.Generic;
using System.Linq;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models
{
    /// <summary>
    /// Represents a mesh object model.
    /// </summary>
    public class MeshCollisionModel : CollisionModel
    {
        /// <summary>
        /// The asset ID.
        /// </summary>
        public string AssetId { get; set; }
        /// <summary>
        /// The list of points.
        /// </summary>
        public List<Pose> Points { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="MeshCollisionModel"/> class.
        /// </summary>
        /// <param name="assetId">An asset ID.</param>
        /// <param name="points">A collection of points.</param>
        public MeshCollisionModel(string assetId, ICollection<Pose> points)
        {
            AssetId = assetId;
            Points = points.ToList();
        }

        /// <inheritdoc cref="CollisionModel"/>
        public override ObjectModel ToObjectModel(string id)
        {
            return new ObjectModel(ObjectModel.TypeEnum.Mesh, mesh: new Mesh(id, AssetId, Points));
        }
    }
}
