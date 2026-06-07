// Copyright 2026 Kyle Ebbinga

using System.Security.Cryptography;

namespace Parallel.Core.Utils
{
    public class HashStream : Stream
    {
        private readonly SHA256 _sha = SHA256.Create();
        private readonly Stream _inner;
        private readonly Action<long>? _reportBytes;
        private long _totalWrite = 0;
        private long _totalRead = 0;
        
        public HashStream(Stream inner)
        {
            _inner = inner;
        }
        
        public HashStream(Stream inner, Action<long> reportBytes)
        {
            _inner = inner;
            _reportBytes = reportBytes;
        }
        
        public string GetHashHexString()
        {
            _sha.TransformFinalBlock([], 0, 0);
            return Convert.ToHexStringLower(_sha.Hash!);
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _inner.Read(buffer, offset, count);
            if (bytesRead <= 0) return bytesRead;
            
            _sha.TransformBlock(buffer, offset, bytesRead, null, 0);
            _totalRead += bytesRead;
            _reportBytes?.Invoke(_totalRead);
            return bytesRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int bytesRead = await _inner.ReadAsync(buffer, cancellationToken);
            if (bytesRead <= 0) return bytesRead;
            
            _sha.TransformBlock(buffer.Span.Slice(0, bytesRead).ToArray(), 0, bytesRead, null, 0);
            _totalRead += bytesRead;
            _reportBytes?.Invoke(_totalRead);
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _sha.TransformBlock(buffer, offset, count, null, 0);
            _inner.Write(buffer, offset, count);
            _totalWrite += count;
            
            _reportBytes?.Invoke(_totalWrite);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _sha.TransformBlock(buffer.ToArray(), 0, buffer.Length, null, 0);
            await _inner.WriteAsync(buffer, cancellationToken);
            _totalWrite += buffer.Length;
            
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