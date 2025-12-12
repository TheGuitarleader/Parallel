// Copyright 2025 Kyle Ebbinga

namespace Parallel.Core.Workers
{
    public class FileAggregator
    {
        private readonly string _localPath;
        private readonly byte[][] _chunks;
        private readonly Action? _onComplete;
        private readonly Action<Exception>? _onError;
        private int _count;
    }
}