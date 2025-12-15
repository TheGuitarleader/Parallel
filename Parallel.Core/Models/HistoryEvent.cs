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
        public string CheckSum { get; set; }

        public HistoryEvent(long timestamp, string path, string checksum, long type)
        {
            CreatedAt = UnixTime.FromMilliseconds(timestamp);
            Fullname = path;
            CheckSum = checksum;
            Type = (HistoryType)type;
        }
    }
}