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
    /// NamedOrientation(name: &#39;str&#39;, orientation: &#39;Orientation&#39;, id: &#39;str&#39; &#x3D; &#39;&#39;)
    /// </summary>
    [DataContract(Name = "NamedOrientation")]
    public partial class NamedOrientation : IEquatable<NamedOrientation>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NamedOrientation" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected NamedOrientation() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="NamedOrientation" /> class.
        /// </summary>
        /// <param name="name">name (required).</param>
        /// <param name="orientation">orientation (required).</param>
        /// <param name="id">id (default to &quot;&quot;).</param>
        public NamedOrientation(string name = default(string), Orientation orientation = default(Orientation), string id = @"")
        {
            // to ensure "name" is required (not null)
            if (name == null)
            {
                throw new ArgumentNullException("name is a required property for NamedOrientation and cannot be null");
            }
            this.Name = name;
            // to ensure "orientation" is required (not null)
            if (orientation == null)
            {
                throw new ArgumentNullException("orientation is a required property for NamedOrientation and cannot be null");
            }
            this.Orientation = orientation;
            // use default value if no "id" provided
            this.Id = id ?? @"";
        }

        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets Orientation
        /// </summary>
        [DataMember(Name = "orientation", IsRequired = true, EmitDefaultValue = true)]
        public Orientation Orientation { get; set; }

        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class NamedOrientation {\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Orientation: ").Append(Orientation).Append("\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
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
            return this.Equals(input as NamedOrientation);
        }

        /// <summary>
        /// Returns true if NamedOrientation instances are equal
        /// </summary>
        /// <param name="input">Instance of NamedOrientation to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(NamedOrientation input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    this.Name == input.Name ||
                    (this.Name != null &&
                    this.Name.Equals(input.Name))
                ) && 
                (
                    this.Orientation == input.Orientation ||
                    (this.Orientation != null &&
                    this.Orientation.Equals(input.Orientation))
                ) && 
                (
                    this.Id == input.Id ||
                    (this.Id != null &&
                    this.Id.Equals(input.Id))
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
                if (this.Name != null)
                {
                    hashCode = (hashCode * 59) + this.Name.GetHashCode();
                }
                if (this.Orientation != null)
                {
                    hashCode = (hashCode * 59) + this.Orientation.GetHashCode();
                }
                if (this.Id != null)
                {
                    hashCode = (hashCode * 59) + this.Id.GetHashCode();
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
