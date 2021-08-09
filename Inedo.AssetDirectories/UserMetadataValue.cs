namespace Inedo.AssetDirectories
{
    /// <summary>
    /// Contains the value of a user-defined metadata item.
    /// </summary>
    public readonly struct UserMetadataValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserMetadataValue"/> struct.
        /// </summary>
        /// <param name="value">The metadata value.</param>
        /// <param name="includeInResponseHeader">Value indicating whether this property should be returned as an HTTP response header.</param>
        public UserMetadataValue(string value, bool includeInResponseHeader = false)
        {
            this.Value = value;
            this.IncludeInResponseHeader = includeInResponseHeader;
        }

        /// <summary>
        /// Returns a <see cref="UserMetadataValue"/> instance with the specified value.
        /// </summary>
        /// <param name="s">Value of the <see cref="UserMetadataValue"/> instance.</param>
        public static implicit operator UserMetadataValue(string s) => new(s);

        /// <summary>
        /// Gets the value.
        /// </summary>
        public string Value { get; }
        /// <summary>
        /// Gets a value indicating whether this property should be returned as an HTTP response header.
        /// </summary>
        public bool IncludeInResponseHeader { get; }

        /// <summary>
        /// Returns the value.
        /// </summary>
        /// <returns>The value.</returns>
        public override string ToString() => this.Value;
    }
}
