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
        
        public HashStream(Stream inner)
        {
            _inner = inner;
        }
        
        public HashStream(Stream inner, Action<long> reportBytes)
        {
            _inner = inner;
            _reportBytes = reportBytes;
        }
        
        public byte[] GetHash()
        {
            _sha.TransformFinalBlock([], 0, 0);
            return _sha.Hash!;
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

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, count);
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