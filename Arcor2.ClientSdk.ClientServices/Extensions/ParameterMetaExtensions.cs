using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    public static class ParameterMetaExtensions {
        public static Parameter ToParameter(this ParameterMeta meta) {
            return new Parameter(meta.Name, meta.Type, meta.DefaultValue);
        }

        public static Parameter ToParameter(this ParameterMeta meta, string value) {
            return new Parameter(meta.Name, meta.Type, value);
        }
    }
}
