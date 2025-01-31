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
    /// Data(state: arcor2_arserver_data.events.scene.SceneState.Data.StateEnum, message: Optional[str] &#x3D; None)
    /// </summary>
    [DataContract(Name = "SceneStateData")]
    public partial class SceneStateData : IEquatable<SceneStateData>, IValidatableObject
    {
        /// <summary>
        /// Defines State
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum StateEnum
        {
            /// <summary>
            /// Enum Stopped for value: stopped
            /// </summary>
            [EnumMember(Value = "stopped")]
            Stopped = 1,

            /// <summary>
            /// Enum Starting for value: starting
            /// </summary>
            [EnumMember(Value = "starting")]
            Starting = 2,

            /// <summary>
            /// Enum Started for value: started
            /// </summary>
            [EnumMember(Value = "started")]
            Started = 3,

            /// <summary>
            /// Enum Stopping for value: stopping
            /// </summary>
            [EnumMember(Value = "stopping")]
            Stopping = 4
        }


        /// <summary>
        /// Gets or Sets State
        /// </summary>
        [DataMember(Name = "state", IsRequired = true, EmitDefaultValue = true)]
        public StateEnum State { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="SceneStateData" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected SceneStateData() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="SceneStateData" /> class.
        /// </summary>
        /// <param name="state">state (required).</param>
        /// <param name="message">message.</param>
        public SceneStateData(StateEnum state = default(StateEnum), string message = default(string))
        {
            this.State = state;
            this.Message = message;
        }

        /// <summary>
        /// Gets or Sets Message
        /// </summary>
        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class SceneStateData {\n");
            sb.Append("  State: ").Append(State).Append("\n");
            sb.Append("  Message: ").Append(Message).Append("\n");
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
            return this.Equals(input as SceneStateData);
        }

        /// <summary>
        /// Returns true if SceneStateData instances are equal
        /// </summary>
        /// <param name="input">Instance of SceneStateData to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(SceneStateData input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    this.State == input.State ||
                    this.State.Equals(input.State)
                ) && 
                (
                    this.Message == input.Message ||
                    (this.Message != null &&
                    this.Message.Equals(input.Message))
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
                hashCode = (hashCode * 59) + this.State.GetHashCode();
                if (this.Message != null)
                {
                    hashCode = (hashCode * 59) + this.Message.GetHashCode();
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
