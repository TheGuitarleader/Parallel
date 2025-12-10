// Copyright 2025 Kyle Ebbinga

namespace Parallel.Core.Utils
{
    public class TransferWorker
    {
        public byte[] Data { get; }
        public string RemotePath { get; }
        public string Filename { get; }
        public Action<Exception>? OnError { get; }

        public TransferWorker(byte[] data, string remotePath, string filename, Action<Exception>? onError = null)
        {
            Data = data;
            RemotePath = remotePath;
            Filename = filename;
            OnError = onError;
        }
    }
}