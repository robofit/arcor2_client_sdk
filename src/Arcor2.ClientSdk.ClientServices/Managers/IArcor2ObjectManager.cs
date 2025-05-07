using System;
using System.ComponentModel;

namespace Arcor2.ClientSdk.ClientServices.Managers {
    public interface IArcor2ObjectManager<out TData> : IDisposable, INotifyPropertyChanged {
        /// <summary>
        ///     The data managed by this instance.
        /// </summary>
        TData Data { get; }

        /// <summary>
        ///     Event raised when before the instance id deleted.
        /// </summary>
        event EventHandler? Removing;
    }
}