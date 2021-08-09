using System;
using System.Collections.Generic;

namespace Inedo.AssetDirectories
{
    /// <summary>
    /// Represents either a file or a folder in an asset directory.
    /// </summary>
    /// <seealso cref="ExtendedAssetDirectoryItem"/>
    public partial class AssetDirectoryItem
    {
        private readonly byte[]? md5;
        private readonly byte[]? sha1;
        private readonly byte[]? sha256;
        private readonly byte[]? sha512;

        /// <summary>
        /// Gets the name of the item relative to its container.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets the full name of the item, which is its absolute path in the asset directory.
        /// </summary>
        public string FullName { get; }
        /// <summary>
        /// Gets the creation timestamp of the item.
        /// </summary>
        public DateTimeOffset Created { get; }
        /// <summary>
        /// Gets the last modified timestamp of the item.
        /// </summary>
        public DateTimeOffset Modified { get; }
        /// <summary>
        /// Gets a value indicating whether the item is a directory.
        /// </summary>
        public bool Directory { get; }
        /// <summary>
        /// Gets the length of the item in bytes if it is a file, or <c>null</c> if it is a directory.
        /// </summary>
        public long? Length { get; }
        /// <summary>
        /// Gets the Content-Type of the item if it is a file, or <c>null</c> if it is a directory or does not have one.
        /// </summary>
        public string? ContentType { get; }

        private protected IReadOnlyDictionary<string, UserMetadataValue>? UserMetadata { get; }

        /// <summary>
        /// Returns an array containing the specified hash of the item if it is available.
        /// </summary>
        /// <param name="hashAlgorithm">Desired hash algorithm.</param>
        /// <returns>Array containing the hash value if it is available; otherwise <c>null</c>.</returns>
        public byte[]? GetHash(AssetHashAlgorithm hashAlgorithm)
        {
            var data = hashAlgorithm switch
            {
                AssetHashAlgorithm.MD5 => this.md5,
                AssetHashAlgorithm.SHA1 => this.sha1,
                AssetHashAlgorithm.SHA256 => this.sha256,
                AssetHashAlgorithm.SHA512 => this.sha512,
                _ => null
            };

            if (data != null)
            {
                var copy = new byte[data.Length];
                Buffer.BlockCopy(data, 0, copy, 0, data.Length);
                return copy;
            }

            return null;
        }
        /// <summary>
        /// Returns the local name of the item.
        /// </summary>
        /// <returns>The local name of the item.</returns>
        public override string ToString() => this.Name;

        private static byte[] ParseHex(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(nameof(s));

            if ((s.Length % 2) != 0)
                throw new ArgumentException("String is not an even number of characters.", nameof(s));

            var bytes = new byte[s.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((GetNibble(s[i * 2]) << 4) | GetNibble(s[(i * 2) + 1]));

            return bytes;
        }
        private static int GetNibble(char c)
        {
            return c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 0xA,
                >= 'A' and <= 'F' => c - 'A' + 0xA,
                _ => throw new FormatException("Invalid character in hex string.")
            };
        }
    }
}
