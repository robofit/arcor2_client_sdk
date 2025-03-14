﻿using Newtonsoft.Json;

namespace Arcor2.ClientSdk.Communication {
    public class Arcor2ClientSettings {
        /// <summary>
        ///     RPC response timeout in milliseconds. By default, <c>10,000</c>ms.
        /// </summary>
        public uint RpcTimeout { get; set; } = 10_000;

        /// <summary>
        ///     Should RPC responses have a valid name that corresponds to the RPC request? By default, <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     While setting this option to <c>false</c> can improve compatibility between different versions,
        ///     it is recommended to keep this option turned on to prevent malformed exchanges.
        /// </remarks>
        /// <value>
        ///     If <c>true</c>, the RPC response name must be valid and match the request RPC name. If <c>false</c>, the client
        ///     otherwise
        ///     only relies on the message ID and OpenApi JSON validity.
        /// </value>
        public bool ValidateRpcResponseName { get; set; } = true;

        /// <summary>
        ///     Should unrecognized JSON properties in ARCOR2 messages be ignored? By default, <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     It is recommended to keep this option turned on for backwards compatibility.
        /// </remarks>
        /// <value>
        ///     If <c>true</c>, unrecognized JSON properties will be ignored. If <c>false</c>, messages with unrecognized JSON
        ///     properties will be ignored as a whole.
        /// </value>
        public bool IgnoreUnrecognizedJsonProperties { get; set; } = true;

        internal JsonSerializerSettings ParseJsonSerializerSettings() =>
            new JsonSerializerSettings {
                MissingMemberHandling = IgnoreUnrecognizedJsonProperties
                    ? MissingMemberHandling.Ignore
                    : MissingMemberHandling.Error
            };
    }
}