using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {

    /// <summary>
    /// Represents parameter validation rules included in <see cref="ParameterMeta.Extra"/> property.
    /// </summary>
    public abstract class ParameterValidator {
        /// <summary>
        /// Validates the string representation of the parameter value.
        /// </summary>
        public abstract bool Validate(string value);
    }

    /// <summary>
    /// Validates if parameter value is within a given range.
    /// </summary>
    public class RangeParameterValidator : ParameterValidator {
        /// <summary>
        /// The minimum value of the parameter.
        /// </summary>
        [DataMember(Name = "minimum")]
        public decimal Minimum { get; }
        
        /// <summary>
        /// The maximum value of the parameter.
        /// </summary>
        [DataMember(Name = "maximum")]
        public decimal Maximum { get; }

        /// <summary>
        /// Validates if parameter value is within a given range.
        /// </summary>
        /// <param name="value">The string representation of the value.</param>
        /// <returns><c>true</c> if yes, <c>false</c> if no.</returns>
        public override bool Validate(string value) {
            var dValue = decimal.Parse(value, CultureInfo.InvariantCulture);
            if (dValue < Minimum) {
                return false;
            }

            return dValue <= Maximum && dValue >= Minimum;
        }

        /// <summary>
        /// Validates if parameter value is within a given range.
        /// </summary>
        /// <param name="dValue">The value.</param>
        /// <returns><c>true</c> if yes, <c>false</c> if no.</returns>
        public bool Validate(decimal dValue) {
            return dValue <= Maximum && dValue >= Minimum;
        }
    }

    /// <summary>
    /// Validates if parameter value is within the allowed values.
    /// </summary>
    public class ValuesParameterValidator : ParameterValidator {
        /// <summary>
        /// The allowed parameter values.
        /// </summary>
        [DataMember(Name = "allowed_values")] 
        public IList<string> AllowedValues = null!;

        /// <summary>
        /// Validates if parameter value is within the allowed values using the <see cref="StringComparison.InvariantCulture"/> comparer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if yes, <c>false</c> if no.</returns>
        public override bool Validate(string value) {
            return AllowedValues.Any(v => v.Equals(value, StringComparison.InvariantCulture));
        }

        /// <summary>
        /// Validates if parameter value is within the allowed values.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="comparisonType">The string comparision type.</param>
        /// <returns><c>true</c> if yes, <c>false</c> if no.</returns>
        public bool Validate(string value, StringComparison comparisonType) {
            return AllowedValues.Any(v => v.Equals(value, comparisonType));
        }
    }
}
