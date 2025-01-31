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
    /// Request(id: int, args: arcor2_arserver_data.rpc.scene.CloseScene.Request.Args &#x3D; &lt;factory&gt;, dry_run: bool &#x3D; False)
    /// </summary>
    [DataContract(Name = "CloseSceneRequest")]
    public partial class CloseSceneRequest : IEquatable<CloseSceneRequest>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseSceneRequest" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected CloseSceneRequest() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseSceneRequest" /> class.
        /// </summary>
        /// <param name="id">id (required).</param>
        /// <param name="request">request (required).</param>
        /// <param name="args">args.</param>
        /// <param name="dryRun">dryRun (default to false).</param>
        public CloseSceneRequest(int id = default(int), string request = default(string), CloseSceneRequestArgs args = default(CloseSceneRequestArgs), bool dryRun = false)
        {
            this.Id = id;
            // to ensure "request" is required (not null)
            if (request == null)
            {
                throw new ArgumentNullException("request is a required property for CloseSceneRequest and cannot be null");
            }
            this.Request = request;
            this.Args = args;
            this.DryRun = dryRun;
        }

        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name = "id", IsRequired = true, EmitDefaultValue = true)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or Sets Request
        /// </summary>
        [DataMember(Name = "request", IsRequired = true, EmitDefaultValue = true)]
        public string Request { get; set; }

        /// <summary>
        /// Gets or Sets Args
        /// </summary>
        [DataMember(Name = "args", EmitDefaultValue = false)]
        public CloseSceneRequestArgs Args { get; set; }

        /// <summary>
        /// Gets or Sets DryRun
        /// </summary>
        [DataMember(Name = "dry_run", EmitDefaultValue = true)]
        public bool DryRun { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class CloseSceneRequest {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Request: ").Append(Request).Append("\n");
            sb.Append("  Args: ").Append(Args).Append("\n");
            sb.Append("  DryRun: ").Append(DryRun).Append("\n");
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
            return this.Equals(input as CloseSceneRequest);
        }

        /// <summary>
        /// Returns true if CloseSceneRequest instances are equal
        /// </summary>
        /// <param name="input">Instance of CloseSceneRequest to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(CloseSceneRequest input)
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
                    this.Request == input.Request ||
                    (this.Request != null &&
                    this.Request.Equals(input.Request))
                ) && 
                (
                    this.Args == input.Args ||
                    (this.Args != null &&
                    this.Args.Equals(input.Args))
                ) && 
                (
                    this.DryRun == input.DryRun ||
                    this.DryRun.Equals(input.DryRun)
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
                if (this.Request != null)
                {
                    hashCode = (hashCode * 59) + this.Request.GetHashCode();
                }
                if (this.Args != null)
                {
                    hashCode = (hashCode * 59) + this.Args.GetHashCode();
                }
                hashCode = (hashCode * 59) + this.DryRun.GetHashCode();
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
