// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Security;

namespace Parallel.Core.Models
{
    /// <summary>
    /// Represents a manifest
    /// </summary>
    public class Manifest
    {
        public string Id { get; }
        public string Fullname { get; }

        public Manifest(string path)
        {
            Id = HashGenerator.CreateSHA1(path);
            Fullname = path;
        }
    }
}