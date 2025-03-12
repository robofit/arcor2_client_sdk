using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models
{
    /// <summary>
    /// Represents a cylinder object model.
    /// </summary>
    public class SphereCollisionModel : CollisionModel
    {
        /// <summary>
        /// The radius.
        /// </summary>
        public decimal Radius { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="SphereCollisionModel"/> class.
        /// </summary>
        /// <param name="radius">The radius.</param>
        public SphereCollisionModel(decimal radius)
        {
            Radius = radius;
        }

        /// <inheritdoc cref="CollisionModel"/>
        public override ObjectModel ToObjectModel(string id)
        {
            return new ObjectModel(ObjectModel.TypeEnum.Sphere, sphere: new Sphere(id, Radius));
        }
    }
}
