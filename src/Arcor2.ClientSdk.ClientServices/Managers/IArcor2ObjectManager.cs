using System;

namespace Arcor2.ClientSdk.ClientServices.Managers {
    public interface IArcor2ObjectManager<out TData> : IDisposable {
        /// <summary>
        ///     The data managed by this instance.
        /// </summary>
        TData Data { get; }

        /// <summary>
        ///     Event raised when the data managed by this instance is updated.
        /// </summary>
        event EventHandler? Updated;

        /// <summary>
        ///     Event raised when before the instance id deleted.
        /// </summary>
        event EventHandler? Removing;
    }
}