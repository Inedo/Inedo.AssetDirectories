using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.AssetDirectories
{
    internal sealed class UploadStream : Stream
    {
        private readonly HttpWebRequest request;
        private readonly Stream requestStream;
        private bool disposed;

        public UploadStream(HttpWebRequest request, Stream requestStream)
        {
            this.request = request;
            this.requestStream = requestStream;
        }
    
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() => this.requestStream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => this.requestStream.FlushAsync(cancellationToken);
        public override void Write(byte[] buffer, int offset, int count) => this.requestStream.Write(buffer, offset, count);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => this.requestStream.WriteAsync(buffer, offset, count, cancellationToken);

#if NET5_0_OR_GREATER
        public override void Write(ReadOnlySpan<byte> buffer) => this.requestStream.Write(buffer);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => this.requestStream.WriteAsync(buffer, cancellationToken);
        public override async ValueTask DisposeAsync()
        {
            if (!this.disposed)
            {
                await this.requestStream.DisposeAsync().ConfigureAwait(false);
                using (await AssetDirectoryClient.GetResponseAsync(this.request, default).ConfigureAwait(false))
                {
                }

                this.disposed = true;
            }
        }
#endif

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.requestStream.Dispose();
                using (AssetDirectoryClient.GetResponse(this.request))
                {
                }


                this.disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
