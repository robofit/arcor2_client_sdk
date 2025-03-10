namespace Arcor2.ClientSdk.ClientServices.Models
{
    /// <summary>
    /// Represents a joint and its value.
    /// </summary>
    public class Joint
    {
        /// <summary>
        /// The joint ID.
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// The joint value.
        /// </summary>
        public decimal Value { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Joint"/> class.
        /// </summary>
        /// <param name="id">The joint ID.</param>
        /// <param name="value">The joint value.</param>
        public Joint(string id, decimal value)
        {
            Id = id;
            Value = value;
        }

        internal Communication.OpenApi.Models.Joint ToOpenApiJointObject()
        {
            return new Communication.OpenApi.Models.Joint(Id, Value);
        }
    }
}