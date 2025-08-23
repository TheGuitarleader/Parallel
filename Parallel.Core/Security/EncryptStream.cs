// Copyright 2025 Kyle Ebbinga

using System.Security.Cryptography;

namespace Parallel.Core.Utils
{
    public class EncryptStream : Stream
    {
        private readonly CryptoStream _cryptoStream;

        public EncryptStream(Stream output, string masterKey, UnixTime timestamp)
        {
            byte[] salt = HashGenerator.RandomBytes(16);
            byte[] iv = HashGenerator.RandomBytes(16);
            byte[] derivedKey = HashGenerator.HKDF(masterKey, salt, timestamp.ToISOString(), 32); // 256-bit key

            // Optionally write salt and IV to the output stream for later decryption
            output.Write(salt, 0, salt.Length);
            output.Write(iv, 0, iv.Length);

            using var aes = Aes.Create();
            aes.Key = derivedKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;

            _cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead { get; }
        public override bool CanSeek { get; }
        public override bool CanWrite { get; }
        public override long Length { get; }
        public override long Position { get; set; }
    }
}