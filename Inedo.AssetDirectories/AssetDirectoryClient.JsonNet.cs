#if NET452
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Inedo.AssetDirectories
{
    public partial class AssetDirectoryClient
    {
        private static void WriteUpdateMetadataRequest(Stream requestStream, string? contentType, IReadOnlyDictionary<string, UserMetadataValue>? userMetadata, UserMetadataUpdateMode updateMode)
        {
            using var writer = new JsonTextWriter(new StreamWriter(requestStream, new UTF8Encoding(false)));
            writer.WriteStartObject();

            if (contentType != null)
            {
                writer.WritePropertyName("type");
                writer.WriteValue(contentType);
            }

            if (userMetadata != null)
            {
                writer.WritePropertyName("userMetadataUpdateMode");
                writer.WriteValue(updateMode == UserMetadataUpdateMode.ReplaceAll ? "replace" : "update");

                writer.WritePropertyName("userMetadata");
                writer.WriteStartObject();

                foreach (var item in userMetadata)
                {
                    writer.WritePropertyName(item.Key);
                    if (item.Value.IncludeInResponseHeader)
                    {
                        writer.WriteStartObject();

                        writer.WritePropertyName("value");
                        writer.WriteValue(item.Value.Value ?? string.Empty);

                        writer.WritePropertyName("includeInResponseHeader");
                        writer.WriteValue(item.Value.IncludeInResponseHeader);

                        writer.WriteEndObject();
                    }
                    else
                    {
                        writer.WriteValue(item.Value.Value ?? string.Empty);
                    }
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }
    }
}
#endif
