using Arcor2.ClientSdk.ClientServices.Enums;

namespace Arcor2.ClientSdk.ClientServices {
    public class Arcor2SessionSettings {
        /// <summary>
        ///     Determines the locking behavior of the library.
        /// </summary>
        public LockingMode LockingMode { get; set; } = LockingMode.AutoLock;

        /// <summary>
        ///     Loads scene, project, package, and object type data on initialization. By default, <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     If disabled, null type hints are no longer accurate and caution must be taken while accessing objects.
        ///     To fata can be later loaded manually using methods with 'Reload' prefix.
        /// </remarks>
        public bool LoadData { get; set; } = true;

        /// <summary>
        ///     RPC response timeout in milliseconds. By default, <c>10,000</c>ms.
        /// </summary>
        public uint RpcTimeout { get; set; } = 10_000;

        /// <summary>
        ///     The target ARCOR2 server version. Changing this to a specific version may disable automatic configuration 
        /// </summary>
        public Arcor2ServerVersion ServerVersion = Arcor2ServerVersion.Automatic;
    }
}