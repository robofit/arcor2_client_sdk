namespace Arcor2.ClientSdk.ClientServices.Enums {
    /// <summary>
    /// Represents a parameter validation rule type.
    /// </summary>
    public enum ParameterValidationType {
        /// <summary>
        /// The parameter is numeric and must be within a range of values.
        /// </summary>
        Range = 0,
        /// <summary>
        /// The parameter is a string and must be one of the listed values.
        /// </summary>
        Values
    }
}
