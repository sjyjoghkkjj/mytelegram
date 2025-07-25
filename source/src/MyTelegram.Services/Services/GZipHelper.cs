using System.IO.Compression;

namespace MyTelegram.Services.Services;

public class GZipHelper : IGZipHelper, ITransientDependency
{
    public void Compress(ReadOnlySpan<byte> source, IBufferWriter<byte> writer)
    {
        using var stream = new BufferWriterStream(writer);
        using var gzip = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);
        gzip.Write(source);
    }

    public void Decompress(ReadOnlyMemory<byte> source, IBufferWriter<byte> writer)
    {
        using var input = new ReadOnlyMemoryStream(source);
        using var gzip = new GZipStream(input, CompressionMode.Decompress, leaveOpen: true);

        var bufferSize = 8192 * 10;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        var span = buffer.AsSpan(0, bufferSize);
        try
        {
            while (true)
            {
                int bytesRead = gzip.Read(buffer);
                if (bytesRead == 0) break;

                var destSpan = writer.GetSpan(bytesRead);
                span.Slice(0, bytesRead).CopyTo(destSpan);
                writer.Advance(bytesRead);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public sealed class BufferWriterStream(IBufferWriter<byte> writer) : Stream
    {
        private readonly IBufferWriter<byte> _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        private Memory<byte> _currentMemory = writer.GetMemory();
        private int _position = 0;

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if ((uint)offset > (uint)buffer.Length || (uint)count > (uint)(buffer.Length - offset))
                throw new ArgumentOutOfRangeException();

            Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            while (!buffer.IsEmpty)
            {
                if (_position >= _currentMemory.Length)
                    FlushInternal();

                int writable = Math.Min(_currentMemory.Length - _position, buffer.Length);
                buffer.Slice(0, writable).CopyTo(_currentMemory.Span.Slice(_position));
                _position += writable;
                buffer = buffer.Slice(writable);
            }
        }

        public override void WriteByte(byte value)
        {
            if (_position >= _currentMemory.Length)
                FlushInternal();

            _currentMemory.Span[_position++] = value;
        }

        private void FlushInternal()
        {
            if (_position > 0)
            {
                _writer.Advance(_position);
                _currentMemory = _writer.GetMemory();
                _position = 0;
            }
        }

        public override void Flush()
        {
            FlushInternal();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                FlushInternal();
            }
            base.Dispose(disposing);
        }

        #region Stream abstract members
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();
        #endregion
    }
    /// <summary>
    /// Wraps a ReadOnlyMemory<byte> as a Stream for input.
    /// </summary>
    private sealed class ReadOnlyMemoryStream(ReadOnlyMemory<byte> span) : Stream
    {
        private int _position = 0;

        public override int Read(byte[] buffer, int offset, int count)
            => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            int remaining = span.Length - _position;
            if (remaining <= 0) return 0;

            int toCopy = Math.Min(buffer.Length, remaining);
            span.Span.Slice(_position, toCopy).CopyTo(buffer);
            _position += toCopy;
            return toCopy;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => span.Length;
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}