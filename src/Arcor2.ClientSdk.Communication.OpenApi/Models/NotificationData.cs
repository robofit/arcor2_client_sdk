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
    /// Data(message: str, level: arcor2.data.events.Notification.Data.Level)
    /// </summary>
    [DataContract(Name = "NotificationData")]
    public partial class NotificationData : IEquatable<NotificationData>, IValidatableObject
    {
        /// <summary>
        /// Defines Level
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum LevelEnum
        {
            /// <summary>
            /// Enum Info for value: Info
            /// </summary>
            [EnumMember(Value = "Info")]
            Info = 1,

            /// <summary>
            /// Enum Warn for value: Warn
            /// </summary>
            [EnumMember(Value = "Warn")]
            Warn = 2,

            /// <summary>
            /// Enum Error for value: Error
            /// </summary>
            [EnumMember(Value = "Error")]
            Error = 3
        }


        /// <summary>
        /// Gets or Sets Level
        /// </summary>
        [DataMember(Name = "level", IsRequired = true, EmitDefaultValue = true)]
        public LevelEnum Level { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationData" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected NotificationData() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationData" /> class.
        /// </summary>
        /// <param name="message">message (required).</param>
        /// <param name="level">level (required).</param>
        public NotificationData(string message = default(string), LevelEnum level = default(LevelEnum))
        {
            // to ensure "message" is required (not null)
            if (message == null)
            {
                throw new ArgumentNullException("message is a required property for NotificationData and cannot be null");
            }
            this.Message = message;
            this.Level = level;
        }

        /// <summary>
        /// Gets or Sets Message
        /// </summary>
        [DataMember(Name = "message", IsRequired = true, EmitDefaultValue = true)]
        public string Message { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class NotificationData {\n");
            sb.Append("  Message: ").Append(Message).Append("\n");
            sb.Append("  Level: ").Append(Level).Append("\n");
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
            return this.Equals(input as NotificationData);
        }

        /// <summary>
        /// Returns true if NotificationData instances are equal
        /// </summary>
        /// <param name="input">Instance of NotificationData to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(NotificationData input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    this.Message == input.Message ||
                    (this.Message != null &&
                    this.Message.Equals(input.Message))
                ) && 
                (
                    this.Level == input.Level ||
                    this.Level.Equals(input.Level)
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
                if (this.Message != null)
                {
                    hashCode = (hashCode * 59) + this.Message.GetHashCode();
                }
                hashCode = (hashCode * 59) + this.Level.GetHashCode();
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
