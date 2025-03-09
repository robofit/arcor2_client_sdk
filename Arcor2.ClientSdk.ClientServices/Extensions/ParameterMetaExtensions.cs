using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Newtonsoft.Json;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    public static class ParameterMetaExtensions {
        /// <summary>
        /// Creates a new parameter from parameter metadata using the default value. If no default value is defined, uses empty string.
        /// </summary>
        /// <param name="meta">The parameter meta.</param>
        public static Parameter ToParameter(this ParameterMeta meta) {
            return new Parameter(meta.Name, meta.Type, meta.DefaultValue ?? "");
        }

        /// <summary>
        /// Creates a new parameter from parameter metadata.
        /// </summary>
        /// <remarks>
        /// The parameter only accepts string representation of its value.
        /// For boolean and numeric types, their default ToString method will suffice.
        /// Strings must be surrounded with double quotes. Complex types (such as pose) should use their JSON representation.
        /// </remarks>
        /// <param name="meta">The parameter meta.</param>
        /// <param name="value">The string representation of the value.</param>
        public static Parameter ToParameter(this ParameterMeta meta, string value) {
            return new Parameter(meta.Name, meta.Type, value);
        }

        /// <summary>
        /// Gets the validator object for this parameter metadata.
        /// The returned <see cref="ParameterValidator"/> can then be cast into specific instance with more options.
        /// </summary>
        /// <returns><c>null</c> if the parameter has no validation logic. The <see cref="ParameterValidator"/> otherwise.</returns>
        public static ParameterValidator? GetValidator(this ParameterMeta meta) {
            if (string.IsNullOrEmpty(meta.Extra)) {
                return null;
            }

            var settings = new JsonSerializerSettings {
                MissingMemberHandling = MissingMemberHandling.Error
            };

            ParameterValidator? validator = null;
            try {
                validator = JsonConvert.DeserializeObject<ValuesParameterValidator>(meta.Extra, settings);
                return validator;
            }
            catch { /* Wrong Type */ }
            try {
                validator = JsonConvert.DeserializeObject<RangeParameterValidator>(meta.Extra, settings);
                return validator;
            }
            catch { /* Wrong Type */ }

            return null;
        }
    }
}
