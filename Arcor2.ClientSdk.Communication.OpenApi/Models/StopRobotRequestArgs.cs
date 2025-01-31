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
    /// Args(robot_id: str)
    /// </summary>
    [DataContract(Name = "StopRobotRequestArgs")]
    public partial class StopRobotRequestArgs : IEquatable<StopRobotRequestArgs>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopRobotRequestArgs" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected StopRobotRequestArgs() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="StopRobotRequestArgs" /> class.
        /// </summary>
        /// <param name="robotId">robotId (required).</param>
        public StopRobotRequestArgs(string robotId = default(string))
        {
            // to ensure "robotId" is required (not null)
            if (robotId == null)
            {
                throw new ArgumentNullException("robotId is a required property for StopRobotRequestArgs and cannot be null");
            }
            this.RobotId = robotId;
        }

        /// <summary>
        /// Gets or Sets RobotId
        /// </summary>
        [DataMember(Name = "robot_id", IsRequired = true, EmitDefaultValue = true)]
        public string RobotId { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class StopRobotRequestArgs {\n");
            sb.Append("  RobotId: ").Append(RobotId).Append("\n");
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
            return this.Equals(input as StopRobotRequestArgs);
        }

        /// <summary>
        /// Returns true if StopRobotRequestArgs instances are equal
        /// </summary>
        /// <param name="input">Instance of StopRobotRequestArgs to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(StopRobotRequestArgs input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    this.RobotId == input.RobotId ||
                    (this.RobotId != null &&
                    this.RobotId.Equals(input.RobotId))
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
                if (this.RobotId != null)
                {
                    hashCode = (hashCode * 59) + this.RobotId.GetHashCode();
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
