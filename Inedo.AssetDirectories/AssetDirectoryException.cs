using System;
using System.Net;

namespace Inedo.AssetDirectories
{
    /// <summary>
    /// Represents an error that occurred communicating with an asset directory.
    /// </summary>
    public class AssetDirectoryException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetDirectoryException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="statusCode">The returned status code.</param>
        public AssetDirectoryException(string message, Exception innerException, int? statusCode) : base(message, innerException)
        {
            this.ResponseCode = statusCode;
        }

        /// <summary>
        /// Gets the HTTP response code if applicable.
        /// </summary>
        public int? ResponseCode { get; }
    }
}
