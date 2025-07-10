// Copyright 2025 Kyle Ebbinga

namespace Parallel.Core.Net
{
    public class ServerRequest
    {
        /// <summary>
        /// The request name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The request parameters.
        /// </summary>
        public Dictionary<string, string> Parameters { get; }

        /// <summary>
        /// The current session id.
        /// </summary>
        public string Session { get; } = string.Empty;
    }
}