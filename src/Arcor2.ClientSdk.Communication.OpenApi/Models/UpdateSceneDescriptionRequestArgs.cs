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
    /// Args(scene_id: str, new_description: str)
    /// </summary>
    [DataContract(Name = "UpdateSceneDescriptionRequestArgs")]
    public partial class UpdateSceneDescriptionRequestArgs : IEquatable<UpdateSceneDescriptionRequestArgs>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSceneDescriptionRequestArgs" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected UpdateSceneDescriptionRequestArgs() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSceneDescriptionRequestArgs" /> class.
        /// </summary>
        /// <param name="sceneId">sceneId (required).</param>
        /// <param name="newDescription">newDescription (required).</param>
        public UpdateSceneDescriptionRequestArgs(string sceneId = default(string), string newDescription = default(string))
        {
            // to ensure "sceneId" is required (not null)
            if (sceneId == null)
            {
                throw new ArgumentNullException("sceneId is a required property for UpdateSceneDescriptionRequestArgs and cannot be null");
            }
            this.SceneId = sceneId;
            // to ensure "newDescription" is required (not null)
            if (newDescription == null)
            {
                throw new ArgumentNullException("newDescription is a required property for UpdateSceneDescriptionRequestArgs and cannot be null");
            }
            this.NewDescription = newDescription;
        }

        /// <summary>
        /// Gets or Sets SceneId
        /// </summary>
        [DataMember(Name = "scene_id", IsRequired = true, EmitDefaultValue = true)]
        public string SceneId { get; set; }

        /// <summary>
        /// Gets or Sets NewDescription
        /// </summary>
        [DataMember(Name = "new_description", IsRequired = true, EmitDefaultValue = true)]
        public string NewDescription { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class UpdateSceneDescriptionRequestArgs {\n");
            sb.Append("  SceneId: ").Append(SceneId).Append("\n");
            sb.Append("  NewDescription: ").Append(NewDescription).Append("\n");
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
            return this.Equals(input as UpdateSceneDescriptionRequestArgs);
        }

        /// <summary>
        /// Returns true if UpdateSceneDescriptionRequestArgs instances are equal
        /// </summary>
        /// <param name="input">Instance of UpdateSceneDescriptionRequestArgs to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(UpdateSceneDescriptionRequestArgs input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    this.SceneId == input.SceneId ||
                    (this.SceneId != null &&
                    this.SceneId.Equals(input.SceneId))
                ) && 
                (
                    this.NewDescription == input.NewDescription ||
                    (this.NewDescription != null &&
                    this.NewDescription.Equals(input.NewDescription))
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
                if (this.SceneId != null)
                {
                    hashCode = (hashCode * 59) + this.SceneId.GetHashCode();
                }
                if (this.NewDescription != null)
                {
                    hashCode = (hashCode * 59) + this.NewDescription.GetHashCode();
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
