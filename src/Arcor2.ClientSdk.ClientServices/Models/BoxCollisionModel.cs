using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models
{
    /// <summary>
    /// Represents a box object model.
    /// </summary>
    public class BoxCollisionModel : CollisionModel
    {
        /// <summary>
        /// The length.
        /// </summary>
        public decimal X { get; set; }
        /// <summary>
        /// The width.
        /// </summary>
        public decimal Y { get; set; }
        /// <summary>
        /// The height.
        /// </summary>
        public decimal Z { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="BoxCollisionModel"/> class.
        /// </summary>
        /// <param name="x">The length.</param>
        /// <param name="y">The width.</param>
        /// <param name="z">The height.</param>
        public BoxCollisionModel(decimal x, decimal y, decimal z)
        {
            X = x; Y = y; Z = z;
        }

        /// <inheritdoc cref="CollisionModel"/>
        public override ObjectModel ToObjectModel(string id)
        {
            return new ObjectModel(ObjectModel.TypeEnum.Box, new Box(id, X, Y, Z));
        }
    }
}
