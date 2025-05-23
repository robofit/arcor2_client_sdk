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
    /// Position(x: &#39;float&#39; &#x3D; 0.0, y: &#39;float&#39; &#x3D; 0.0, z: &#39;float&#39; &#x3D; 0.0)
    /// </summary>
    [DataContract(Name = "Position")]
    public partial class Position : IEquatable<Position>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Position" /> class.
        /// </summary>
        /// <param name="x">x (default to 0.0M).</param>
        /// <param name="y">y (default to 0.0M).</param>
        /// <param name="z">z (default to 0.0M).</param>
        public Position(decimal x = 0.0M, decimal y = 0.0M, decimal z = 0.0M)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// Gets or Sets X
        /// </summary>
        [DataMember(Name = "x", EmitDefaultValue = false)]
        public decimal X { get; set; }

        /// <summary>
        /// Gets or Sets Y
        /// </summary>
        [DataMember(Name = "y", EmitDefaultValue = false)]
        public decimal Y { get; set; }

        /// <summary>
        /// Gets or Sets Z
        /// </summary>
        [DataMember(Name = "z", EmitDefaultValue = false)]
        public decimal Z { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class Position {\n");
            sb.Append("  X: ").Append(X).Append("\n");
            sb.Append("  Y: ").Append(Y).Append("\n");
            sb.Append("  Z: ").Append(Z).Append("\n");
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
            return this.Equals(input as Position);
        }

        /// <summary>
        /// Returns true if Position instances are equal
        /// </summary>
        /// <param name="input">Instance of Position to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Position input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    this.X == input.X ||
                    this.X.Equals(input.X)
                ) && 
                (
                    this.Y == input.Y ||
                    this.Y.Equals(input.Y)
                ) && 
                (
                    this.Z == input.Z ||
                    this.Z.Equals(input.Z)
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
                hashCode = (hashCode * 59) + this.X.GetHashCode();
                hashCode = (hashCode * 59) + this.Y.GetHashCode();
                hashCode = (hashCode * 59) + this.Z.GetHashCode();
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
