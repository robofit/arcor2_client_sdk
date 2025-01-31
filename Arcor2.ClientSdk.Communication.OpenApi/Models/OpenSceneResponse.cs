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
    /// Response(id: int &#x3D; 0, result: bool &#x3D; True, messages: Optional[list[str]] &#x3D; None)
    /// </summary>
    [DataContract(Name = "OpenSceneResponse")]
    public partial class OpenSceneResponse : IEquatable<OpenSceneResponse>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenSceneResponse" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected OpenSceneResponse() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenSceneResponse" /> class.
        /// </summary>
        /// <param name="id">id (default to 0).</param>
        /// <param name="response">response (required).</param>
        /// <param name="result">result (default to true).</param>
        /// <param name="messages">messages.</param>
        public OpenSceneResponse(int id = 0, string response = default(string), bool result = true, List<string> messages = default(List<string>))
        {
            // to ensure "response" is required (not null)
            if (response == null)
            {
                throw new ArgumentNullException("response is a required property for OpenSceneResponse and cannot be null");
            }
            this.Response = response;
            this.Id = id;
            this.Result = result;
            this.Messages = messages;
        }

        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or Sets Response
        /// </summary>
        [DataMember(Name = "response", IsRequired = true, EmitDefaultValue = true)]
        public string Response { get; set; }

        /// <summary>
        /// Gets or Sets Result
        /// </summary>
        [DataMember(Name = "result", EmitDefaultValue = true)]
        public bool Result { get; set; }

        /// <summary>
        /// Gets or Sets Messages
        /// </summary>
        [DataMember(Name = "messages", EmitDefaultValue = false)]
        public List<string> Messages { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class OpenSceneResponse {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Response: ").Append(Response).Append("\n");
            sb.Append("  Result: ").Append(Result).Append("\n");
            sb.Append("  Messages: ").Append(Messages).Append("\n");
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
            return this.Equals(input as OpenSceneResponse);
        }

        /// <summary>
        /// Returns true if OpenSceneResponse instances are equal
        /// </summary>
        /// <param name="input">Instance of OpenSceneResponse to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(OpenSceneResponse input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    this.Id == input.Id ||
                    this.Id.Equals(input.Id)
                ) && 
                (
                    this.Response == input.Response ||
                    (this.Response != null &&
                    this.Response.Equals(input.Response))
                ) && 
                (
                    this.Result == input.Result ||
                    this.Result.Equals(input.Result)
                ) && 
                (
                    this.Messages == input.Messages ||
                    this.Messages != null &&
                    input.Messages != null &&
                    this.Messages.SequenceEqual(input.Messages)
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
                hashCode = (hashCode * 59) + this.Id.GetHashCode();
                if (this.Response != null)
                {
                    hashCode = (hashCode * 59) + this.Response.GetHashCode();
                }
                hashCode = (hashCode * 59) + this.Result.GetHashCode();
                if (this.Messages != null)
                {
                    hashCode = (hashCode * 59) + this.Messages.GetHashCode();
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
