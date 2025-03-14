using Arcor2.ClientSdk.Communication.OpenApi.Models;

namespace Arcor2.ClientSdk.ClientServices.Models {
    /// <summary>
    ///     Represents a project override of action object parameter.
    /// </summary>
    public class ProjectOverride {
        /// <summary>
        ///     Initializes a new instance of <see cref="ProjectOverride" /> class.
        /// </summary>
        public ProjectOverride(string actionObjectId, Parameter parameter) {
            ActionObjectId = actionObjectId;
            Parameter = parameter;
        }

        /// <summary>
        ///     The overriden action object ID.
        /// </summary>
        public string ActionObjectId { get; }

        /// <summary>
        ///     An overriden parameters.
        /// </summary>
        public Parameter Parameter { get; internal set; }
    }
}