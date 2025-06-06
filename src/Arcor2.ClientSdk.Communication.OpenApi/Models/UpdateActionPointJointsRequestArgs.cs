/*
 * ARCOR2 ARServer Data Models
 *
 * No description provided (generated by Openapi Generator https://github.com/openapitools/openapi-generator)
 *
 * The version of the OpenAPI document: 1.2.0
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace Arcor2.ClientSdk.Communication.OpenApi.Models
{
    /// <summary>
    /// Args(joints_id: str, joints: list[arcor2.data.common.Joint])
    /// </summary>
    [DataContract(Name = "UpdateActionPointJointsRequestArgs")]
    public partial class UpdateActionPointJointsRequestArgs : IEquatable<UpdateActionPointJointsRequestArgs>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateActionPointJointsRequestArgs" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected UpdateActionPointJointsRequestArgs() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateActionPointJointsRequestArgs" /> class.
        /// </summary>
        /// <param name="jointsId">jointsId (required).</param>
        /// <param name="joints">joints (required).</param>
        public UpdateActionPointJointsRequestArgs(string jointsId = default(string), List<Joint> joints = default(List<Joint>))
        {
            // to ensure "jointsId" is required (not null)
            if (jointsId == null)
            {
                throw new ArgumentNullException("jointsId is a required property for UpdateActionPointJointsRequestArgs and cannot be null");
            }
            this.JointsId = jointsId;
            // to ensure "joints" is required (not null)
            if (joints == null)
            {
                throw new ArgumentNullException("joints is a required property for UpdateActionPointJointsRequestArgs and cannot be null");
            }
            this.Joints = joints;
        }

        /// <summary>
        /// Gets or Sets JointsId
        /// </summary>
        [DataMember(Name = "joints_id", IsRequired = true, EmitDefaultValue = true)]
        public string JointsId { get; set; }

        /// <summary>
        /// Gets or Sets Joints
        /// </summary>
        [DataMember(Name = "joints", IsRequired = true, EmitDefaultValue = false)]
        public List<Joint> Joints { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class UpdateActionPointJointsRequestArgs {\n");
            sb.Append("  JointsId: ").Append(JointsId).Append("\n");
            sb.Append("  Joints: ").Append(Joints).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as UpdateActionPointJointsRequestArgs);
        }

        /// <summary>
        /// Returns true if UpdateActionPointJointsRequestArgs instances are equal
        /// </summary>
        /// <param name="input">Instance of UpdateActionPointJointsRequestArgs to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(UpdateActionPointJointsRequestArgs input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    this.JointsId == input.JointsId ||
                    (this.JointsId != null &&
                    this.JointsId.Equals(input.JointsId))
                ) && 
                (
                    this.Joints == input.Joints ||
                    this.Joints != null &&
                    input.Joints != null &&
                    this.Joints.SequenceEqual(input.Joints)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.JointsId != null)
                {
                    hashCode = (hashCode * 59) + this.JointsId.GetHashCode();
                }
                if (this.Joints != null)
                {
                    hashCode = (hashCode * 59) + this.Joints.GetHashCode();
                }
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

}
