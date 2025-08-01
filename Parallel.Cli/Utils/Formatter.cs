// Copyright 2025 Entex Interactive, LLC

namespace Parallel.Cli.Utils
{
    public class Formatter
    {
        public static string FromBytes(long bytes)
        {
            string[] sizeSuffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int sizeIndex = 0;
            double size = bytes;

            while (size >= 1000 && sizeIndex < sizeSuffixes.Length - 1)
            {
                sizeIndex++;
                size /= 1000;
            }

            return $"{size:N2} {sizeSuffixes[sizeIndex]}";
        }

        public static string FromDateTime(DateTime dateTime)
        {
            return dateTime.ToLocalTime().ToString("MM/dd/yyyy hh:mmtt");
        }
        
        public static string FromTimeSpan(TimeSpan timeSpan)
        {
            return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}.{timeSpan.Milliseconds:N2}";
        }
    }
}