#if !NET452
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Inedo.AssetDirectories
{
    public partial class AssetDirectoryClient
    {
        private static void WriteUpdateMetadataRequest(Stream requestStream, string? contentType, IReadOnlyDictionary<string, UserMetadataValue>? userMetadata, UserMetadataUpdateMode updateMode)
        {
            using var writer = new Utf8JsonWriter(requestStream);
            writer.WriteStartObject();

            if (contentType != null)
                writer.WriteString("type", contentType);

            if (userMetadata != null)
            {
                writer.WriteString("userMetadataUpdateMode", updateMode == UserMetadataUpdateMode.ReplaceAll ? "replace" : "update");

                writer.WritePropertyName("userMetadata");
                writer.WriteStartObject();

                foreach (var item in userMetadata)
                {
                    writer.WritePropertyName(item.Key);
                    if (item.Value.IncludeInResponseHeader)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("value", item.Value.Value ?? string.Empty);
                        writer.WriteBoolean("includeInResponseHeader", item.Value.IncludeInResponseHeader);
                        writer.WriteEndObject();
                    }
                    else
                    {
                        writer.WriteStringValue(item.Value.Value ?? string.Empty);
                    }
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }
    }
}
#endif
