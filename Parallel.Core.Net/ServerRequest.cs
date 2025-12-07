// Copyright 2025 Kyle Ebbinga

using Newtonsoft.Json;

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
        /// Initializes new instance of the <see cref="ServerRequest"/> class with a request name and a <see cref="Dictionary{TKey, TValue}"/> of parameters.
        /// </summary>
        /// <param name="name">The request name.</param>
        /// <param name="parameters">A collection of parameter keys and values.</param>
        [JsonConstructor]
        public ServerRequest(string name, Dictionary<string, string> parameters)
        {
            Name = name.ToLower();
            Parameters = parameters;
        }
    }
}