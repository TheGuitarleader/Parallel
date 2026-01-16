// Copyright 2025 Kyle Ebbinga

namespace Parallel.Core.Utils
{
    public class StreamProgress : Stream
    {
        private readonly Stream _inner;
        private readonly Action<long> _reportBytes;
        private long _totalWrite = 0;
        private long _totalRead = 0;

        public StreamProgress(Stream inner, Action<long> reportBytes)
        {
            _inner = inner;
            _reportBytes = reportBytes;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _inner.Read(buffer, offset, count);
            if (read > 0)
            {
                _totalRead += read;
                _reportBytes?.Invoke(_totalRead);
            }

            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
            if (read > 0)
            {
                _totalRead += read;
                _reportBytes?.Invoke(_totalRead);
            }

            return read;
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
            _totalWrite += count;
            _reportBytes?.Invoke(_totalWrite);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
            _totalWrite += count;
            _reportBytes?.Invoke(_totalWrite);
        }

        #region Stream required overrides

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _inner.FlushAsync(cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _inner.SetLength(value);
        }

        #endregion
    }
}