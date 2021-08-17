using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.AssetDirectories
{
    internal sealed class RandomAccessDownloadStream : Stream
    {
        private readonly AssetDirectoryItem item;
        private readonly AssetDirectoryClient client;

        public RandomAccessDownloadStream(AssetDirectoryItem item, AssetDirectoryClient client)
        {
            this.item = item;
            this.client = client;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => this.item.Length.GetValueOrDefault();
        public override long Position { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length)
                throw new ArgumentException("The sum of offset and count exceeded the length of the array.");

            int bytesToRead = (int)Math.Min(count, this.Length - this.Position);
            if (bytesToRead == 0)
                return 0;

            var request = this.client.CreateRequest("content/" + Uri.EscapeUriString(this.item.FullName));
            request.AddRange(this.Position, this.Position + bytesToRead);

            using var response = request.GetResponse();
            using var responseStream = response.GetResponseStream();

            int bytesRead = 0;
            int n;
            do
            {
                n = responseStream.Read(buffer, offset + bytesRead, bytesToRead);
                bytesRead += n;
                bytesToRead -= n;
            }
            while (n > 0 && bytesToRead > 0);

            this.Position += bytesRead;

            return bytesRead;
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (offset + count > buffer.Length)
                throw new ArgumentException("The sum of offset and count exceeded the length of the array.");

            int bytesToRead = (int)Math.Min(count, this.Length - this.Position);
            if (bytesToRead == 0)
                return 0;

            var request = this.client.CreateRequest("content/" + Uri.EscapeUriString(this.item.FullName));
            request.AddRange(this.Position, this.Position + bytesToRead);

            using var response = await request.GetResponseAsync().ConfigureAwait(false);
            using var responseStream = response.GetResponseStream();

            int bytesRead = 0;
            int n;
            do
            {
                n = await responseStream.ReadAsync(buffer, offset + bytesRead, bytesToRead).ConfigureAwait(false);
                bytesRead += n;
                bytesToRead -= n;
            }
            while (n > 0 && bytesToRead > 0);

            this.Position += bytesRead;

            return bytesRead;
        }
#if NET5_0_OR_GREATER
        public override int Read(Span<byte> buffer)
        {
            if (buffer.IsEmpty)
                return 0;

            int bytesToRead = (int)Math.Min(buffer.Length, this.Length - this.Position);

            var request = this.client.CreateRequest("content/" + Uri.EscapeUriString(this.item.FullName));
            request.AddRange(this.Position, this.Position + bytesToRead);

            using var response = request.GetResponse();
            using var responseStream = response.GetResponseStream();

            int bytesRead = 0;
            int n;
            do
            {
                n = responseStream.Read(buffer.Slice(bytesRead, bytesToRead));
                bytesRead += n;
                bytesToRead -= n;
            }
            while (n > 0 && bytesToRead > 0);

            this.Position += bytesRead;

            return bytesRead;
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (buffer.IsEmpty)
                return 0;

            int bytesToRead = (int)Math.Min(buffer.Length, this.Length - this.Position);

            var request = this.client.CreateRequest("content/" + Uri.EscapeUriString(this.item.FullName));
            request.AddRange(this.Position, this.Position + bytesToRead);

            using var response = await request.GetResponseAsync().ConfigureAwait(false);
            using var responseStream = response.GetResponseStream();

            int bytesRead = 0;
            int n;
            do
            {
                n = await responseStream.ReadAsync(buffer.Slice(bytesRead, bytesToRead)).ConfigureAwait(false);
                bytesRead += n;
                bytesToRead -= n;
            }
            while (n > 0 && bytesToRead > 0);

            this.Position += bytesRead;

            return bytesRead;
        }
#endif
        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => this.Position + offset,
                SeekOrigin.End => this.Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };
        }
        public override void Flush()
        {
        }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
