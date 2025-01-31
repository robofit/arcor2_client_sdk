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
    /// BareScene(name: &#39;str&#39;, description: &#39;str&#39; &#x3D; &lt;factory&gt;, created: &#39;Optional[datetime]&#39; &#x3D; None, modified: &#39;Optional[datetime]&#39; &#x3D; None, int_modified: &#39;Optional[datetime]&#39; &#x3D; None, id: &#39;str&#39; &#x3D; &#39;&#39;)
    /// </summary>
    [DataContract(Name = "BareScene")]
    public partial class BareScene : IEquatable<BareScene>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BareScene" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected BareScene() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="BareScene" /> class.
        /// </summary>
        /// <param name="name">name (required).</param>
        /// <param name="description">description (default to &quot;&quot;).</param>
        /// <param name="created">created.</param>
        /// <param name="modified">modified.</param>
        /// <param name="intModified">intModified.</param>
        /// <param name="id">id (default to &quot;&quot;).</param>
        public BareScene(string name = default(string), string description = @"", DateTime created = default(DateTime), DateTime modified = default(DateTime), DateTime intModified = default(DateTime), string id = @"")
        {
            // to ensure "name" is required (not null)
            if (name == null)
            {
                throw new ArgumentNullException("name is a required property for BareScene and cannot be null");
            }
            this.Name = name;
            // use default value if no "description" provided
            this.Description = description ?? @"";
            this.Created = created;
            this.Modified = modified;
            this.IntModified = intModified;
            // use default value if no "id" provided
            this.Id = id ?? @"";
        }

        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets Description
        /// </summary>
        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or Sets Created
        /// </summary>
        [DataMember(Name = "created", EmitDefaultValue = false)]
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or Sets Modified
        /// </summary>
        [DataMember(Name = "modified", EmitDefaultValue = false)]
        public DateTime Modified { get; set; }

        /// <summary>
        /// Gets or Sets IntModified
        /// </summary>
        [DataMember(Name = "int_modified", EmitDefaultValue = false)]
        public DateTime IntModified { get; set; }

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
            sb.Append("class BareScene {\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Description: ").Append(Description).Append("\n");
            sb.Append("  Created: ").Append(Created).Append("\n");
            sb.Append("  Modified: ").Append(Modified).Append("\n");
            sb.Append("  IntModified: ").Append(IntModified).Append("\n");
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
            return this.Equals(input as BareScene);
        }

        /// <summary>
        /// Returns true if BareScene instances are equal
        /// </summary>
        /// <param name="input">Instance of BareScene to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(BareScene input)
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
                    this.Description == input.Description ||
                    (this.Description != null &&
                    this.Description.Equals(input.Description))
                ) && 
                (
                    this.Created == input.Created ||
                    (this.Created != null &&
                    this.Created.Equals(input.Created))
                ) && 
                (
                    this.Modified == input.Modified ||
                    (this.Modified != null &&
                    this.Modified.Equals(input.Modified))
                ) && 
                (
                    this.IntModified == input.IntModified ||
                    (this.IntModified != null &&
                    this.IntModified.Equals(input.IntModified))
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
                if (this.Description != null)
                {
                    hashCode = (hashCode * 59) + this.Description.GetHashCode();
                }
                if (this.Created != null)
                {
                    hashCode = (hashCode * 59) + this.Created.GetHashCode();
                }
                if (this.Modified != null)
                {
                    hashCode = (hashCode * 59) + this.Modified.GetHashCode();
                }
                if (this.IntModified != null)
                {
                    hashCode = (hashCode * 59) + this.IntModified.GetHashCode();
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
