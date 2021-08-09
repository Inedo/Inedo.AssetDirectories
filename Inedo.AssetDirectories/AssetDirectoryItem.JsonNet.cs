#if NET452
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Inedo.AssetDirectories
{
    public partial class AssetDirectoryItem
    {
        private protected AssetDirectoryItem(JObject obj)
        {
            this.Name = (string?)obj.Property("name") ?? throw new InvalidDataException("Missing \"name\" property.");
            var parentPath = ((string?)obj.Property("parent"))?.Trim('/');
            this.FullName = !string.IsNullOrEmpty(parentPath) ? (parentPath + "/" + this.Name) : this.Name;
            this.Created = (DateTimeOffset)obj.Property("created")!;
            this.Modified = (DateTimeOffset?)obj.Property("modified") ?? this.Created;
            this.Length = (long?)obj.Property("size");
            this.md5 = ReadHash(obj, "md5");
            this.sha1 = ReadHash(obj, "sha1");
            this.sha256 = ReadHash(obj, "sha256");
            this.sha512 = ReadHash(obj, "sha512");

            var type = (string?)obj.Property("type");
            if (type == "dir")
                this.Directory = true;
            else
                this.ContentType = type;

            if (obj.Property("userMetadata")?.Value is JObject userMetadataObj)
            {
                var d = new Dictionary<string, UserMetadataValue>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in userMetadataObj.Properties())
                {
                    string value;
                    bool includeInResponse = false;
                    if (property.Value is JObject valueObj)
                    {
                        value = (string?)valueObj.Property("value") ?? string.Empty;
                        includeInResponse = (bool?)valueObj.Property("includeInResponseHeader") ?? false;
                    }
                    else
                    {
                        value = (string?)property ?? string.Empty;
                    }

                    d[property!.Name] = new UserMetadataValue(value, includeInResponse);
                }

                this.UserMetadata = new ReadOnlyDictionary<string, UserMetadataValue>(d);
            }
        }

        internal static ExtendedAssetDirectoryItem ReadFromJson(Stream stream)
        {
            using var reader = new JsonTextReader(new StreamReader(stream, Encoding.UTF8, false, 4096, true));
            return new ExtendedAssetDirectoryItem(JObject.Load(reader));
        }
        internal static IEnumerable<AssetDirectoryItem> ReadFromJsonArray(Stream stream)
        {
            using var reader = new JsonTextReader(new StreamReader(stream, Encoding.UTF8, false, 4096, true));
            if (!reader.Read() || reader.TokenType != JsonToken.StartArray)
                yield break;

            while (reader.Read() && reader.TokenType == JsonToken.StartObject)
            {
                yield return new AssetDirectoryItem((JObject)JToken.ReadFrom(reader));
            }
        }

        private static byte[]? ReadHash(JObject obj, string name)
        {
            var text = (string?)obj.Property(name);
            if (!string.IsNullOrEmpty(text))
                return ParseHex(text!);
            else
                return null;
        }
    }
}
#endif
