using System.Collections.Generic;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    /// ARCOR2 Object Type.
    /// </summary>
    public class ObjectType {
        /// <summary>
        /// Information about the object type.
        /// </summary>
        public ObjectTypeMeta Meta { get; set; }
        /// <summary>
        /// The available actions.
        /// </summary>
        public IList<ObjectAction> Actions { get; set; } = new List<ObjectAction>();

        public ObjectType(ObjectTypeMeta meta) {
            Meta = meta;
        }
    }
}
