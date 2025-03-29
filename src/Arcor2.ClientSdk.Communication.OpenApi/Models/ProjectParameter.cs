/*
 * ARCOR2 ARServer Data Models
 *
 * No description provided (generated by Openapi Generator https://github.com/openapitools/openapi-generator)
 *
 * The version of the OpenAPI document: 1.2.0
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace Arcor2.ClientSdk.Communication.OpenApi.Models
{
    /// <summary>
    /// ProjectParameter(name: &#39;str&#39;, type: &#39;str&#39;, value: &#39;str&#39;, range: &#39;Optional[Range]&#39; &#x3D; None, display_name: &#39;Optional[str]&#39; &#x3D; None, description: &#39;Optional[str]&#39; &#x3D; None, id: &#39;str&#39; &#x3D; &#39;&#39;)
    /// </summary>
    [DataContract(Name = "ProjectParameter")]
    public partial class ProjectParameter : IEquatable<ProjectParameter>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectParameter" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected ProjectParameter() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectParameter" /> class.
        /// </summary>
        /// <param name="name">name (required).</param>
        /// <param name="type">type (required).</param>
        /// <param name="value">value (required).</param>
        /// <param name="range">range.</param>
        /// <param name="displayName">displayName.</param>
        /// <param name="description">description.</param>
        /// <param name="id">id (default to &quot;&quot;).</param>
        public ProjectParameter(string name = default, string type = default, string value = default, Range range = default, string displayName = default, string description = default, string id = @"")
        {
            // to ensure "name" is required (not null)
            if (name == null)
            {
                throw new ArgumentNullException("name is a required property for ProjectParameter and cannot be null");
            }
            Name = name;
            // to ensure "type" is required (not null)
            if (type == null)
            {
                throw new ArgumentNullException("type is a required property for ProjectParameter and cannot be null");
            }
            Type = type;
            // to ensure "value" is required (not null)
            if (value == null)
            {
                throw new ArgumentNullException("value is a required property for ProjectParameter and cannot be null");
            }
            Value = value;
            Range = range;
            DisplayName = displayName;
            Description = description;
            // use default value if no "id" provided
            Id = id ?? @"";
        }

        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets Type
        /// </summary>
        [DataMember(Name = "type", IsRequired = true, EmitDefaultValue = true)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or Sets Value
        /// </summary>
        [DataMember(Name = "value", IsRequired = true, EmitDefaultValue = true)]
        public string Value { get; set; }

        /// <summary>
        /// Gets or Sets Range
        /// </summary>
        [DataMember(Name = "range", EmitDefaultValue = false)]
        public Range Range { get; set; }

        /// <summary>
        /// Gets or Sets DisplayName
        /// </summary>
        [DataMember(Name = "display_name", EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or Sets Description
        /// </summary>
        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }

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
            sb.Append("class ProjectParameter {\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("  Range: ").Append(Range).Append("\n");
            sb.Append("  DisplayName: ").Append(DisplayName).Append("\n");
            sb.Append("  Description: ").Append(Description).Append("\n");
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
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return Equals(input as ProjectParameter);
        }

        /// <summary>
        /// Returns true if ProjectParameter instances are equal
        /// </summary>
        /// <param name="input">Instance of ProjectParameter to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(ProjectParameter input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    Name == input.Name ||
                    (Name != null &&
                    Name.Equals(input.Name))
                ) && 
                (
                    Type == input.Type ||
                    (Type != null &&
                    Type.Equals(input.Type))
                ) && 
                (
                    Value == input.Value ||
                    (Value != null &&
                    Value.Equals(input.Value))
                ) && 
                (
                    Range == input.Range ||
                    (Range != null &&
                    Range.Equals(input.Range))
                ) && 
                (
                    DisplayName == input.DisplayName ||
                    (DisplayName != null &&
                    DisplayName.Equals(input.DisplayName))
                ) && 
                (
                    Description == input.Description ||
                    (Description != null &&
                    Description.Equals(input.Description))
                ) && 
                (
                    Id == input.Id ||
                    (Id != null &&
                    Id.Equals(input.Id))
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
                if (Name != null)
                {
                    hashCode = (hashCode * 59) + Name.GetHashCode();
                }
                if (Type != null)
                {
                    hashCode = (hashCode * 59) + Type.GetHashCode();
                }
                if (Value != null)
                {
                    hashCode = (hashCode * 59) + Value.GetHashCode();
                }
                if (Range != null)
                {
                    hashCode = (hashCode * 59) + Range.GetHashCode();
                }
                if (DisplayName != null)
                {
                    hashCode = (hashCode * 59) + DisplayName.GetHashCode();
                }
                if (Description != null)
                {
                    hashCode = (hashCode * 59) + Description.GetHashCode();
                }
                if (Id != null)
                {
                    hashCode = (hashCode * 59) + Id.GetHashCode();
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
