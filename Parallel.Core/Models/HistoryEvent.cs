// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Database;
using Parallel.Core.Utils;

namespace Parallel.Core.Models
{
    public class HistoryEvent
    {
        public HistoryType Type { get; set; }
        public UnixTime CreatedAt { get; set; }
        public string Vault { get; set; }
        public string Fullname { get; set; }
    }
}