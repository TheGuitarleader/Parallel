// Copyright 2025 Kyle Ebbinga

namespace Parallel.Core.Workers
{
    public class DownloadWorker : BaseWorker
    {
        public FileAggregator Aggregator { get; }

        public DownloadWorker(string remotePath, Action<Exception>? onException = null) : base(remotePath, onException)
        {

        }
    }
}