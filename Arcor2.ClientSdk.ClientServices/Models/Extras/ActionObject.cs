using System.Collections.Generic;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models.Extras {
    /// <summary>
    /// Represents an action object optionally extended with robot information.
    /// </summary>
    public class ActionObject {
        /// <summary>
        /// Information about the object type.
        /// </summary>
        public SceneObject Meta { get; internal set; }

        /// <summary>
        /// The list of joints and its values.
        /// </summary>
        /// <value>
        /// <c>null</c> if not applicable (e.g. the object is not a robot).
        /// </value>
        public IList<Joint>? Joints { get; internal set; }

        /// <summary>
        /// The list of end effectors and its poses.
        /// </summary>
        /// <value>
        /// <c>null</c> if not applicable (e.g. the object is not a robot).
        /// </value>
        public IList<EndEffector>? EefPoses { get; internal set; }

        /// <summary>
        /// The list of joints and its values.
        /// </summary>
        /// <value>
        /// <c>null</c> if not applicable (e.g. the object is not a robot or is single-armed).
        /// </value>
        public IList<string>? Arms { get; internal set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ActionObject"/> class.
        /// </summary>
        public ActionObject(SceneObject meta, IList<Joint>? joints, IList<EndEffector>? eefPoses, IList<string>? arms) {
            Meta = meta;
            Joints = joints;
            EefPoses = eefPoses;
            Arms = arms;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ActionObject"/> class.
        /// </summary>
        public ActionObject(SceneObject meta) {
            Meta = meta;
            Joints = null;
            EefPoses = null;
            Arms = null;
        }
    }
}
