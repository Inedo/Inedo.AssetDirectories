#if !NET452
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;

namespace Inedo.AssetDirectories
{
    public partial class AssetDirectoryItem
    {
        private protected AssetDirectoryItem(JsonElement element)
        {
            this.Name = element.GetProperty("name").GetString() ?? throw new InvalidDataException("Missing \"name\" property.");

            string? parentPath = null;
            if (element.TryGetProperty("parent", out var parentElement))
                parentPath = parentElement.GetString();
            
            this.FullName = !string.IsNullOrEmpty(parentPath) ? (parentPath + "/" + this.Name) : this.Name;
            this.Created = element.GetProperty("created").GetDateTimeOffset();
            this.Modified = element.TryGetProperty("modified", out var modifiedElement) ? modifiedElement.GetDateTimeOffset() : this.Created;
            if (element.TryGetProperty("size", out var sizeElement))
                this.Length = sizeElement.GetInt64();

            this.md5 = ReadHash(element, "md5");
            this.sha1 = ReadHash(element, "sha1");
            this.sha256 = ReadHash(element, "sha256");
            this.sha512 = ReadHash(element, "sha512");

            if (element.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();
                if (type == "dir")
                    this.Directory = true;
                else
                    this.ContentType = type;
            }

            if (element.TryGetProperty("userMetadata", out var userMetadataElement))
            {
                var d = ImmutableDictionary.CreateBuilder<string, UserMetadataValue>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in userMetadataElement.EnumerateObject())
                {
                    string value = string.Empty;
                    bool includeInResponse = false;
                    if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        if (property.Value.TryGetProperty("value", out var valueElement))
                            value = valueElement.GetString() ?? string.Empty;
                        if (property.Value.TryGetProperty("includeInResponseHeader", out var includeElement))
                            includeInResponse = includeElement.GetBoolean();
                    }
                    else
                    {
                        value = property.Value.GetString() ?? string.Empty;
                    }

                    d[property.Name] = new UserMetadataValue(value, includeInResponse);
                }

                this.UserMetadata = d.ToImmutable();
            }
        }

        internal static ExtendedAssetDirectoryItem ReadFromJson(Stream stream)
        {
            using var doc = JsonDocument.Parse(stream);
            return new ExtendedAssetDirectoryItem(doc.RootElement);
        }
        internal static IEnumerable<AssetDirectoryItem> ReadFromJsonArray(Stream stream)
        {
            using var doc = JsonDocument.Parse(stream);
            foreach (var element in doc.RootElement.EnumerateArray())
                yield return new AssetDirectoryItem(element);
        }

        private static byte[]? ReadHash(JsonElement element, string name)
        {
            if (element.TryGetProperty(name, out var property))
            {
                var value = property.GetString();
                if (!string.IsNullOrEmpty(value))
                    return ParseHex(value!);
            }

            return null;
        }
    }
}
#endif
