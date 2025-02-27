using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Extensions {
    internal static class PackageInfoDataExtensions {
        public static PackageMeta MapToPackageMeta(this PackageInfoData package) {
            return new PackageMeta(package.PackageName);
        }
    }
}