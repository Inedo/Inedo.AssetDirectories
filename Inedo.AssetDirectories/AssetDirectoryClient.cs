using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.AssetDirectories
{
    /// <summary>
    /// Client for communicating with a ProGet Asset Directory.
    /// </summary>
    public sealed partial class AssetDirectoryClient
    {
        private readonly string? apiKey;
        private readonly string? basicAuthToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetDirectoryClient"/> class.
        /// </summary>
        /// <param name="endpointUrl">API endpoint URL of the asset directory.</param>
        /// <param name="apiKey">ProGet API key used to authenticate requests.</param>
        /// <param name="userName">The user name to supply when using basic authentication.</param>
        /// <param name="password">The password to supply when using basic authentication.</param>
        /// <exception cref="ArgumentNullException"><paramref name="endpointUrl"/> is null or empty.</exception>
        public AssetDirectoryClient(string endpointUrl, string? apiKey = null, string? userName = null, string? password = null)
        {
            if (string.IsNullOrWhiteSpace(endpointUrl))
                throw new ArgumentNullException(nameof(endpointUrl));

            this.apiKey = apiKey != string.Empty ? apiKey : null;
            this.EndpointUrl = endpointUrl;
            if (!endpointUrl.EndsWith("/"))
                this.EndpointUrl += "/";

            if (!string.IsNullOrEmpty(userName))
                this.basicAuthToken = "Basic " + Convert.ToBase64String(new UTF8Encoding(false).GetBytes(userName + ":" + password));
        }

        /// <summary>
        /// Gets the asset directory API endpoint URL.
        /// </summary>
        public string EndpointUrl { get; }

        /// <summary>
        /// Returns metadata for the asset with the specified path.
        /// </summary>
        /// <param name="path">Full path to the asset.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        /// <returns>Extended metadata for the specified asset.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty.</exception>
        /// <exception cref="AssetDirectoryException">Asset was not found.</exception>
        public Task<ExtendedAssetDirectoryItem> GetItemMetadataAsync(string path, CancellationToken cancellationToken = default) => this.GetItemMetadataAsync(path, true, cancellationToken)!;
        /// <summary>
        /// Returns metadata for the asset with the specified path if it exists.
        /// </summary>
        /// <param name="path">Full path to the asset.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        /// <returns>Extended metadata for the specified asset if it was found; otherwise null.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty.</exception>
        public Task<ExtendedAssetDirectoryItem?> TryGetItemMetadataAsync(string path, CancellationToken cancellationToken = default) => this.GetItemMetadataAsync(path, false, cancellationToken);
        /// <summary>
        /// Updates the metadata of the specified asset.
        /// </summary>
        /// <param name="path">Full path to the asset.</param>
        /// <param name="contentType">New Content-Type of the asset if specified. When null, it will not be updated.</param>
        /// <param name="userMetadata">New user-defined metadata entries for the asset if specified. When null, it will not be updated.</param>
        /// <param name="userMetadataUpdateMode">Specifies how the <paramref name="userMetadata"/> parameter is interpreted.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty.</exception>
        public async Task UpdateItemMetadataAsync(string path, string? contentType = null, IReadOnlyDictionary<string, UserMetadataValue>? userMetadata = null, UserMetadataUpdateMode userMetadataUpdateMode = UserMetadataUpdateMode.CreateOrUpdate, CancellationToken cancellationToken = default)
        {
            CanonicalizePath(ref path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (contentType == null && userMetadata == null)
                return;

            var request = this.CreateRequest("metadata/" + Uri.EscapeUriString(path));
            request.Method = "POST";
            request.ContentType = "application/json";

            using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
                WriteUpdateMetadataRequest(requestStream, contentType, userMetadata, userMetadataUpdateMode);
            }

            using (await GetResponseAsync(request, cancellationToken).ConfigureAwait(false))
            {
            }
        }
        /// <summary>
        /// Returns the contents of the specified asset folder.
        /// </summary>
        /// <param name="path">Full path to the asset. May be null or empty for the asset root.</param>
        /// <param name="recursive">When true, contents of all subfolders are also recursively returned.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        /// <returns>Contents of the specified asset folder.</returns>
        public async Task<IReadOnlyCollection<AssetDirectoryItem>> ListContentsAsync(string? path = null, bool recursive = false, CancellationToken cancellationToken = default)
        {
            CanonicalizeOptionalPath(ref path);
            var request = this.CreateRequest($"dir/{Uri.EscapeUriString(path?.Trim('/') ?? string.Empty)}?recursive={recursive.ToString().ToLowerInvariant()}", true);
            request.Accept = "application/json";
            using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.ContentType?.StartsWith("application/json") != true)
                throw new InvalidDataException($"Incorrect Content-Type returned from server: {response.ContentType} (expected application/json)");

            using var stream = response.GetResponseStream();
            return AssetDirectoryItem.ReadFromJsonArray(stream).ToList();
        }
        /// <summary>
        /// Opens a <see cref="Stream"/> used to upload data to an asset.
        /// </summary>
        /// <param name="path">Full path of the asset to upload.</param>
        /// <param name="contentType">Content-Type of the uploaded asset. When null, it will be determined by the server.</param>
        /// <param name="totalSize">Total size of the asset. Data written to the returned <see cref="Stream"/> must not exceed this size if it is specified.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        /// <returns><see cref="Stream"/> used to upload an asset.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty.</exception>
        public async Task<Stream> UploadFileAsync(string path, string? contentType = null, long? totalSize = null, CancellationToken cancellationToken = default)
        {
            CanonicalizePath(ref path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var request = this.CreateRequest("content/" + Uri.EscapeUriString(path));
            request.Method = "POST";
            request.ContentType = contentType;
            request.AllowWriteStreamBuffering = false;

            if (totalSize >= 0)
                request.ContentLength = totalSize.GetValueOrDefault();
            else
                request.SendChunked = true;

            return new UploadStream(request, await request.GetRequestStreamAsync().ConfigureAwait(false));
        }
        /// <summary>
        /// Opens a <see cref="Stream"/> used to upload data to an asset and performs a multipart upload as necessary.
        /// </summary>
        /// <param name="path">Full path of the asset to upload.</param>
        /// <param name="totalSize">Total size of the asset. Data written to the returned <see cref="Stream"/> must not exceed this size.</param>
        /// <param name="contentType">Content-Type of the uploaded asset. When null, it will be determined by the server.</param>
        /// <param name="partSize">Size in bytes of each individually-uploaded part.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        /// <returns><see cref="Stream"/> used to upload an asset.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="totalSize"/> is invalid.</exception>
        public async Task<Stream> UploadMultipartFileAsync(string path, long totalSize, string? contentType = null, int partSize = 5 * 1024 * 1024, CancellationToken cancellationToken = default)
        {
            CanonicalizePath(ref path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (totalSize < 0)
                throw new ArgumentOutOfRangeException(nameof(totalSize));

            // don't bother with multipart upload if there would only be one part
            if (totalSize <= partSize)
                return await this.UploadFileAsync(path, contentType, cancellationToken: cancellationToken).ConfigureAwait(false);

            var id = Guid.NewGuid().ToString("N");
            int totalParts = (int)(totalSize / partSize);
            if ((totalSize % partSize) != 0)
                totalParts++;

            var request = this.CreateRequest($"content/{Uri.EscapeUriString(path)}?multipart=upload&id={id}&index=0&offset=0&totalSize={totalSize}&partSize={partSize}&totalParts={totalParts}");
            request.Method = "POST";
            request.ContentType = contentType;
            request.AllowWriteStreamBuffering = false;
            request.ContentLength = partSize;

            return new MultipartUploadStream(this, path, totalSize, partSize, id, request, await request.GetRequestStreamAsync().ConfigureAwait(false));
        }
        /// <summary>
        /// Begins a download of the specified asset.
        /// </summary>
        /// <param name="path">Full path of the asset to download.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        /// <returns><see cref="Stream"/> of the downloaded asset.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty.</exception>
        public async Task<Stream> DownloadFileAsync(string path, CancellationToken cancellationToken = default)
        {
            CanonicalizePath(ref path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var request = this.CreateRequest("content/" + Uri.EscapeUriString(path));
            var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
            return response.GetResponseStream();
        }
        /// <summary>
        /// Opens the specified asset as a random access stream.
        /// </summary>
        /// <param name="path">Full path of the asset to download.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        /// <returns><see cref="Stream"/> of the downloaded asset.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty.</exception>
        /// <remarks>
        /// The returned stream is not buffered in any way. It is recommended to either read in large blocks
        /// or to add a buffering layer on top. To read a file sequentially, use <see cref="DownloadFileAsync(string, CancellationToken)"/> instead.
        /// </remarks>
        public async Task<Stream> OpenRandomAccessFileAsync(string path, CancellationToken cancellationToken = default)
        {
            CanonicalizePath(ref path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var metadata = await this.GetItemMetadataAsync(path, cancellationToken).ConfigureAwait(false);
            if (metadata.Directory)
                throw new InvalidOperationException("Cannot open remote directory as a file.");

            return new RandomAccessDownloadStream(metadata, this);
        }
        /// <summary>
        /// Deletes an asset item or folder.
        /// </summary>
        /// <param name="path">Full path of the asset to delete.</param>
        /// <param name="recursive">When the path refers to a directory, recursively delete contents if <c>true</c>.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty.</exception>
        public async Task DeleteItemAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
        {
            CanonicalizePath(ref path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var request = this.CreateRequest($"delete/{Uri.EscapeUriString(path)}?recursive={recursive}");
            request.Method = "POST";
            using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Creates the specified subdirectory if it does not already exist.
        /// </summary>
        /// <param name="path">Full path of the directory to create.</param>
        /// <param name="cancellationToken">Token used to cancel asynchronous operation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty.</exception>
        /// <remarks>
        /// It is not an error to create a directory that already exists.
        /// </remarks>
        public async Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
        {
            CanonicalizePath(ref path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var request = this.CreateRequest("dir/" + Uri.EscapeUriString(path));
            request.Method = "POST";
            using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
        }

        internal HttpWebRequest CreateRequest(string url, bool decompression = false)
        {
            var request = WebRequest.CreateHttp(this.EndpointUrl + url);
            if (this.basicAuthToken != null)
                request.Headers["Authorization"] = this.basicAuthToken;
            if (this.apiKey != null)
                request.Headers["X-ProGet-ApiKey"] = this.apiKey;

            if (decompression)
            {
#if NET5_0_OR_GREATER
                request.AutomaticDecompression = DecompressionMethods.All;
#else
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
#endif
            }

            return request;
        }
        internal static async Task<HttpWebResponse> GetResponseAsync(HttpWebRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);
            }
            catch (WebException ex)
            {
                throw new AssetDirectoryException(GetErrorMessage(ex), ex);
            }
        }
        internal static HttpWebResponse GetResponse(HttpWebRequest request)
        {
            try
            {
                return (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                throw new AssetDirectoryException(GetErrorMessage(ex), ex);
            }
        }

        private async Task<ExtendedAssetDirectoryItem?> GetItemMetadataAsync(string path, bool throwIfNotFound, CancellationToken cancellationToken)
        {
            CanonicalizePath(ref path);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var request = this.CreateRequest("metadata/" + Uri.EscapeUriString(path), true);
            using var response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
            using var stream = response.GetResponseStream();

            try
            {
                return AssetDirectoryItem.ReadFromJson(stream);
            }
            catch (AssetDirectoryException ex) when (!throwIfNotFound && ex.ResponseCode == 404)
            {
                return null;
            }
        }
        private static string GetErrorMessage(WebException ex)
        {
            using var response = ex.Response;
            string message = string.Empty;
            if (response != null && response.ContentType == "text/plain")
            {
                using var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                message = reader.ReadToEnd();
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                if (response is HttpWebResponse webResponse)
                    message = (int)webResponse.StatusCode + " " + webResponse.StatusDescription;
                else
                    message = "Unknown error.";
            }

            return message;
        }
        private static void CanonicalizePath(ref string path)
        {
            if (!string.IsNullOrEmpty(path))
                path = Regex.Replace(path, @"[/\\]", "/").Trim('/');
        }
        private static void CanonicalizeOptionalPath(ref string? path)
        {
            if (!string.IsNullOrEmpty(path))
                path = Regex.Replace(path, @"[/\\]", "/").Trim('/');
        }
    }
}
