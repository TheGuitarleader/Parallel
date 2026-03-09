// Copyright 2026 Entex Interactive, LLC

using Parallel.Core.Database;
using Parallel.Core.Utils;

namespace Parallel.Core.Models
{
    public class HistoryEvent
    {
        public HistoryType Type { get; set; }
        public UnixTime CreatedAt { get; set; }
        public string Fullname { get; set; }

        public HistoryEvent(long timestamp, string path, long type)
        {
            CreatedAt = UnixTime.FromMilliseconds(timestamp);
            Fullname = path;
            Type = (HistoryType)type;
        }
    }
}