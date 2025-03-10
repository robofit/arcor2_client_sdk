using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    public static class ParameterExtensions {
        public static IdValue ToIdValue(this Parameter param) {
            return new IdValue(param.Name, param.Value);
        }
    }
}