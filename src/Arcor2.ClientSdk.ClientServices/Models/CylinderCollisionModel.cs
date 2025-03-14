using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    ///     Represents a cylinder object model.
    /// </summary>
    public class CylinderCollisionModel : CollisionModel {
        /// <summary>
        ///     Initializes a new instance of <see cref="CylinderCollisionModel" /> class.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="height">The length.</param>
        public CylinderCollisionModel(decimal radius, decimal height) {
            Radius = radius;
            Height = height;
        }

        /// <summary>
        ///     The radius.
        /// </summary>
        public decimal Radius { get; set; }

        /// <summary>
        ///     The height.
        /// </summary>
        public decimal Height { get; set; }

        /// <inheritdoc cref="CollisionModel" />
        public override ObjectModel ToObjectModel(string id) => new ObjectModel(ObjectModel.TypeEnum.Cylinder,
            cylinder: new Cylinder(id, Radius, Height));
    }
}