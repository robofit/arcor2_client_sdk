using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    ///     Represents an end effector and its pose on action object.
    /// </summary>
    public class EndEffector {
        /// <summary>
        ///     Initializes a new instance of <see cref="EndEffector" /> class.
        /// </summary>
        /// <param name="id">The end effector ID.</param>
        /// <param name="pose">The end effector pose.</param>
        /// <param name="armId">The arm ID. Null if robot is single-armed.</param>
        public EndEffector(string id, Pose pose, string? armId = null) {
            Id = id;
            Pose = pose;
            ArmId = armId;
        }

        /// <summary>
        ///     Initializes a new instance of <see cref="EndEffector" /> class.
        /// </summary>
        /// <param name="id">The end effector ID.</param>
        /// <param name="armId">The arm ID. Null if robot is single-armed.</param>
        public EndEffector(string id, string? armId = null) {
            Id = id;
            ArmId = armId;
        }

        /// <summary>
        ///     The end effector ID.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        ///     The ID of the arm. Null if robot is single-armed.
        /// </summary>
        public string? ArmId { get; private set; }

        /// <summary>
        ///     The pose of the end effector.
        /// </summary>
        /// <value>
        ///     <c>null</c> if unknown.
        /// </value>
        public Pose? Pose { get; set; }
    }
}