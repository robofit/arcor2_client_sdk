﻿using Arcor2.ClientSdk.ClientServices.Enums;
using System;

namespace Arcor2.ClientSdk.ClientServices.EventArguments {
    /// <summary>
    ///     Event args for navigation state changes.
    /// </summary>
    public class NavigationStateEventArgs : EventArgs {
        /// <summary>
        ///     Initializes a new instance of <see cref="NavigationStateEventArgs" /> class.
        /// </summary>
        public NavigationStateEventArgs(NavigationState state, string? id = null) {
            State = state;
            Id = id;
        }

        /// <summary>
        ///     The new navigation state.
        /// </summary>
        public NavigationState State { get; }

        /// <summary>
        ///     The opened or highlighted scene, project, or package.
        /// </summary>
        public string? Id { get; }
    }
}