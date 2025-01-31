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
    /// Data(package_id: str)
    /// </summary>
    [DataContract(Name = "BuildProjectResponseData")]
    public partial class BuildProjectResponseData : IEquatable<BuildProjectResponseData>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildProjectResponseData" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected BuildProjectResponseData() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildProjectResponseData" /> class.
        /// </summary>
        /// <param name="packageId">packageId (required).</param>
        public BuildProjectResponseData(string packageId = default(string))
        {
            // to ensure "packageId" is required (not null)
            if (packageId == null)
            {
                throw new ArgumentNullException("packageId is a required property for BuildProjectResponseData and cannot be null");
            }
            this.PackageId = packageId;
        }

        /// <summary>
        /// Gets or Sets PackageId
        /// </summary>
        [DataMember(Name = "package_id", IsRequired = true, EmitDefaultValue = true)]
        public string PackageId { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class BuildProjectResponseData {\n");
            sb.Append("  PackageId: ").Append(PackageId).Append("\n");
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
            return this.Equals(input as BuildProjectResponseData);
        }

        /// <summary>
        /// Returns true if BuildProjectResponseData instances are equal
        /// </summary>
        /// <param name="input">Instance of BuildProjectResponseData to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(BuildProjectResponseData input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    this.PackageId == input.PackageId ||
                    (this.PackageId != null &&
                    this.PackageId.Equals(input.PackageId))
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
                if (this.PackageId != null)
                {
                    hashCode = (hashCode * 59) + this.PackageId.GetHashCode();
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
