using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.AssetDirectories
{
    internal sealed class MultipartUploadStream : Stream
    {
        private readonly AssetDirectoryClient client;
        private readonly string url;
        private readonly string id;
        private readonly int defaultPartSize;
        private readonly long totalSize;
        private HttpWebRequest request;
        private Stream requestStream;
        private int partSize;
        private int partsWritten;
        private int bytesWrittenInCurrentPart;
        private bool disposed;

        public MultipartUploadStream(AssetDirectoryClient client, string url, long totalSize, int defaultPartSize, string id, HttpWebRequest request, Stream requestStream)
        {
            this.client = client;
            this.defaultPartSize = defaultPartSize;
            this.partSize = defaultPartSize;
            this.id = id;
            this.url = url;
            this.totalSize = totalSize;
            this.request = request;
            this.requestStream = requestStream;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length)
                throw new ArgumentException("The sum of offset and count cannot exceed the buffer length.");

            if (count == 0)
                return;

            int bytesToWrite = Math.Min(count, this.partSize - this.bytesWrittenInCurrentPart);
            while (bytesToWrite > 0)
            {
                this.requestStream.Write(buffer, offset, bytesToWrite);

                count -= bytesToWrite;
                offset += bytesToWrite;
                this.bytesWrittenInCurrentPart += bytesToWrite;
                if (this.bytesWrittenInCurrentPart == this.partSize)
                    this.CompleteRequest();

                bytesToWrite = Math.Min(count, this.partSize - this.bytesWrittenInCurrentPart);
            }
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length)
                throw new ArgumentException("The sum of offset and count cannot exceed the buffer length.");

            if (count == 0)
                return;

            int bytesToWrite = Math.Min(count, this.partSize - this.bytesWrittenInCurrentPart);
            while (bytesToWrite > 0)
            {
                await this.requestStream.WriteAsync(buffer, offset, bytesToWrite, cancellationToken).ConfigureAwait(false);

                count -= bytesToWrite;
                offset += bytesToWrite;
                this.bytesWrittenInCurrentPart += bytesToWrite;
                if (this.bytesWrittenInCurrentPart == this.partSize)
                    await this.CompleteRequestAsync(cancellationToken).ConfigureAwait(false);

                bytesToWrite = Math.Min(count, this.partSize - this.bytesWrittenInCurrentPart);
            }
        }

#if NET5_0_OR_GREATER
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            int count = buffer.Length;
            int offset = 0;
            int bytesToWrite = Math.Min(count, this.partSize - this.bytesWrittenInCurrentPart);
            while (bytesToWrite > 0)
            {
                this.requestStream.Write(buffer.Slice(offset, bytesToWrite));

                count -= bytesToWrite;
                offset += bytesToWrite;
                this.bytesWrittenInCurrentPart += bytesToWrite;
                if (this.bytesWrittenInCurrentPart == this.partSize)
                    this.CompleteRequest();

                bytesToWrite = Math.Min(count, this.partSize - this.bytesWrittenInCurrentPart);
            }
        }
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int count = buffer.Length;
            int offset = 0;
            int bytesToWrite = Math.Min(count, this.partSize - this.bytesWrittenInCurrentPart);
            while (bytesToWrite > 0)
            {
                await this.requestStream.WriteAsync(buffer.Slice(offset, bytesToWrite), cancellationToken).ConfigureAwait(false);

                count -= bytesToWrite;
                offset += bytesToWrite;
                this.bytesWrittenInCurrentPart += bytesToWrite;
                if (this.bytesWrittenInCurrentPart == this.partSize)
                    await this.CompleteRequestAsync(cancellationToken).ConfigureAwait(false);

                bytesToWrite = Math.Min(count, this.partSize - this.bytesWrittenInCurrentPart);
            }
        }
#endif

        public override void Flush() => this.requestStream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => this.requestStream.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

#if NET5_0_OR_GREATER
        public override async ValueTask DisposeAsync()
        {
            if (!this.disposed)
            {
                await this.requestStream.DisposeAsync().ConfigureAwait(false);
                using (await this.request.GetResponseAsync().ConfigureAwait(false))
                {
                }

                this.disposed = true;
            }
        }
#endif

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.requestStream.Dispose();
                    using (this.request.GetResponse())
                    {
                    }
                }

                this.disposed = true;
            }

            base.Dispose(disposing);
        }

        private async Task CompleteRequestAsync(CancellationToken cancellationToken)
        {
#if NET5_0_OR_GREATER
            await this.requestStream.DisposeAsync().ConfigureAwait(false);
#else
            this.requestStream.Dispose();
#endif
            
            using (await AssetDirectoryClient.GetResponseAsync(this.request, cancellationToken).ConfigureAwait(false))
            {
            }

            this.partsWritten++;
            this.bytesWrittenInCurrentPart = 0;
            this.request = this.CreateNextRequest();
            this.requestStream = await this.request.GetRequestStreamAsync().ConfigureAwait(false);
        }
        private void CompleteRequest()
        {
            this.requestStream.Dispose();
            using (AssetDirectoryClient.GetResponse(this.request))
            {
            }

            this.partsWritten++;
            this.bytesWrittenInCurrentPart = 0;
            this.request = this.CreateNextRequest();
            this.requestStream = this.request.GetRequestStream();
        }
        private HttpWebRequest CreateNextRequest()
        {
            long offset = (long)this.partsWritten * this.defaultPartSize;
            int totalParts = (int)(this.totalSize / this.defaultPartSize);
            if ((this.totalSize % this.defaultPartSize) != 0)
                totalParts++;

            if (this.partsWritten < totalParts - 1)
                this.partSize = this.defaultPartSize;
            else
                this.partSize = (int)(this.totalSize - offset);

            if (this.partSize > 0)
            {
                var request = this.client.CreateRequest($"content/{url}?multipart=upload&id={this.id}&index={this.partsWritten}&offset={offset}&totalSize={this.totalSize}&partSize={this.partSize}&totalParts={totalParts}");
                request.Method = "POST";
                request.AllowWriteStreamBuffering = false;
                request.ContentLength = this.partSize;
                return request;
            }
            else
            {
                return this.CreateCompleteRequest();
            }
        }
        private HttpWebRequest CreateCompleteRequest()
        {
            var request = this.client.CreateRequest($"content/{url}?multipart=complete&id={this.id}");
            request.Method = "POST";
            return request;
        }
    }
}
