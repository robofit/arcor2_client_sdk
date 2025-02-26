using System.Collections.Generic;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models.Extras {
    /// <summary>
    /// Represents an object type optionally extended with robot information.
    /// </summary>
    public class ObjectType {
        /// <summary>
        /// Information about the object type.
        /// </summary>
        public ObjectTypeMeta Meta { get; internal set; }

        /// <summary>
        /// Information about robot capabilities (if the object type is a robot type).
        /// </summary>
        public RobotMeta? RobotMeta { get; internal set; }

        /// <summary>
        /// The available actions.
        /// </summary>
        public IList<ObjectAction> Actions { get; internal set; } = new List<ObjectAction>();

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectType"/> class.
        /// </summary>
        public ObjectType(ObjectTypeMeta meta, RobotMeta? robotMeta = null) {
            Meta = meta;
            RobotMeta = robotMeta;
        }

    }
}
