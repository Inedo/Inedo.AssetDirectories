using System.Collections.Generic;

namespace Inedo.AssetDirectories
{
    /// <summary>
    /// Represents either a file or a folder in an asset directory that has additional metadata.
    /// </summary>
    public sealed class ExtendedAssetDirectoryItem : AssetDirectoryItem
    {
#if NET452
        private static readonly IReadOnlyDictionary<string, UserMetadataValue> EmptyMetadata = new System.Collections.ObjectModel.ReadOnlyDictionary<string, UserMetadataValue>(new Dictionary<string, UserMetadataValue>());

        internal ExtendedAssetDirectoryItem(Newtonsoft.Json.Linq.JObject obj) : base(obj)
        {
        }
#else
        private static IReadOnlyDictionary<string, UserMetadataValue> EmptyMetadata => System.Collections.Immutable.ImmutableDictionary<string, UserMetadataValue>.Empty;

        internal ExtendedAssetDirectoryItem(System.Text.Json.JsonElement element) : base(element)
        {
        }
#endif

        /// <summary>
        /// Gets the user-defined metadata for the item.
        /// </summary>
        public new IReadOnlyDictionary<string, UserMetadataValue> UserMetadata => base.UserMetadata ?? EmptyMetadata;
    }
}
