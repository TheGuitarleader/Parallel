// Copyright 2025 Kyle Ebbinga

namespace Parallel.Core.Workers
{
    /// <summary>
    /// Represents a worker responsible for uploading files.
    /// </summary>
    public class UploadWorker : BaseWorker
    {
        public byte[] Data { get; }

        public string Filename { get; }


        public UploadWorker(byte[] data, string filename, string remotePath, Action<Exception>? onError = null) : base(remotePath, onError)
        {
            Data = data;
            Filename = filename;

        }
    }
}