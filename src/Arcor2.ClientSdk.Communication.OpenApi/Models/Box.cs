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
    /// Box(id: str, size_x: float, size_y: float, size_z: float)
    /// </summary>
    [DataContract(Name = "Box")]
    public partial class Box : IEquatable<Box>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Box" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected Box() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Box" /> class.
        /// </summary>
        /// <param name="id">id (required).</param>
        /// <param name="sizeX">sizeX (required).</param>
        /// <param name="sizeY">sizeY (required).</param>
        /// <param name="sizeZ">sizeZ (required).</param>
        public Box(string id = default(string), decimal sizeX = default(decimal), decimal sizeY = default(decimal), decimal sizeZ = default(decimal))
        {
            // to ensure "id" is required (not null)
            if (id == null)
            {
                throw new ArgumentNullException("id is a required property for Box and cannot be null");
            }
            this.Id = id;
            this.SizeX = sizeX;
            this.SizeY = sizeY;
            this.SizeZ = sizeZ;
        }

        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name = "id", IsRequired = true, EmitDefaultValue = true)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or Sets SizeX
        /// </summary>
        [DataMember(Name = "size_x", IsRequired = true, EmitDefaultValue = true)]
        public decimal SizeX { get; set; }

        /// <summary>
        /// Gets or Sets SizeY
        /// </summary>
        [DataMember(Name = "size_y", IsRequired = true, EmitDefaultValue = true)]
        public decimal SizeY { get; set; }

        /// <summary>
        /// Gets or Sets SizeZ
        /// </summary>
        [DataMember(Name = "size_z", IsRequired = true, EmitDefaultValue = true)]
        public decimal SizeZ { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class Box {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  SizeX: ").Append(SizeX).Append("\n");
            sb.Append("  SizeY: ").Append(SizeY).Append("\n");
            sb.Append("  SizeZ: ").Append(SizeZ).Append("\n");
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
            return this.Equals(input as Box);
        }

        /// <summary>
        /// Returns true if Box instances are equal
        /// </summary>
        /// <param name="input">Instance of Box to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Box input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    this.Id == input.Id ||
                    (this.Id != null &&
                    this.Id.Equals(input.Id))
                ) && 
                (
                    this.SizeX == input.SizeX ||
                    this.SizeX.Equals(input.SizeX)
                ) && 
                (
                    this.SizeY == input.SizeY ||
                    this.SizeY.Equals(input.SizeY)
                ) && 
                (
                    this.SizeZ == input.SizeZ ||
                    this.SizeZ.Equals(input.SizeZ)
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
                if (this.Id != null)
                {
                    hashCode = (hashCode * 59) + this.Id.GetHashCode();
                }
                hashCode = (hashCode * 59) + this.SizeX.GetHashCode();
                hashCode = (hashCode * 59) + this.SizeY.GetHashCode();
                hashCode = (hashCode * 59) + this.SizeZ.GetHashCode();
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
