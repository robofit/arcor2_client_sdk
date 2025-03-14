using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    ///     Represents an object model.
    /// </summary>
    public abstract class CollisionModel {
        /// <summary>
        ///     Converts the <see cref="CollisionModel" /> object to the OpenAPI <see cref="ObjectModel" /> object.
        /// </summary>
        /// <param name="id">The object type.</param>
        public abstract ObjectModel ToObjectModel(string id);
    }
}