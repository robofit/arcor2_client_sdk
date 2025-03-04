using Arcor2.ClientSdk.ClientServices.Models;
using Arcor2.ClientSdk.Communication.OpenApi.Models;
using Newtonsoft.Json;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    public static class ParameterMetaExtensions {
        public static Parameter ToParameter(this ParameterMeta meta) {
            return new Parameter(meta.Name, meta.Type, meta.DefaultValue);
        }

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

            ParameterValidator? validator = null;
            try {
                validator = JsonConvert.DeserializeObject<ValuesParameterValidator>(meta.Extra);
            }
            catch { /* Wrong Type */ }
            try {
                validator = JsonConvert.DeserializeObject<RangeParameterValidator>(meta.Extra);
            }
            catch { /* Wrong Type */ }

            return validator;
        }
    }
}
