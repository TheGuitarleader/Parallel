// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Security;

namespace Parallel.Core.IO.Blobs
{
    public class FileManifest
    {
        public string Id { get; }
        public string Fullname { get; set; }
        public List<string> Hashes { get; }
        public long Length { get; set; }

        public FileManifest(string path, IEnumerable<string> hashes, long length)
        {
            Id = HashGenerator.CreateSHA1(path);
            Fullname = path;
            Hashes = hashes.ToList();
            Length = length;
        }
    }
}