// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Storage;

namespace Parallel.Core.Workers
{
    /// <summary>
    /// Represents
    /// </summary>
    public abstract class BaseWorker
    {
        /// <summary>
        /// Gets the remote path in the <see cref="IStorageProvider"/>.
        /// </summary>
        public string RemotePath { get; }

        /// <summary>
        /// The <see cref="Action"/> to run when an <see cref="Exception"/> is thrown.
        /// </summary>
        public Action<Exception>? OnException { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseWorker"/> class with a remote path.
        /// </summary>
        /// <param name="remotePath"></param>
        /// <param name="onError"></param>
        protected BaseWorker(string remotePath, Action<Exception>? onException)
        {
            RemotePath = remotePath;
            OnException = onException;
        }
    }
}