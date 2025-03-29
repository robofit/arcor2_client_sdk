namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    ///     Represents different compatibility modes for different versions of the ARCOR2 server.
    /// </summary>
    public enum Arcor2ServerVersion {
        /// <summary>
        ///     Automatically set the best mode for the server.
        /// </summary>
        Automatic,
        /// <summary>
        ///     The ARCOR2 server version 1.3.1.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        V1_3_1
    }

    internal static class Arcor2ServerVersionExtensions {
        public static Arcor2ServerVersion ParseVersion(string version) {
            // To the developer reading this into a future:
            // This is used when the user specifies `Automatic` mode in the settings.
            // As I am writing this, there is just one version, so I always return the sames value.
            // In the future, you should actually parse the version and compare it with supported version.
            // If the version is too new, return the latest available version. If it is too old, return
            // the oldest available version to maintain compatibility.
            return version switch {
                "1.3.1" => Arcor2ServerVersion.V1_3_1,
                _ => Arcor2ServerVersion.V1_3_1
            };
        }
    }
}
