// Copyright 2025 Kyle Ebbinga

using System.Diagnostics;
using Parallel.Core.Settings;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Utils;

namespace Parallel.Core.IO.FileSystem
{
    /// <summary>
    /// Represents the wrapper for an SFTP file system through SSH.
    /// </summary>
    public class SftpFileSystem : IFileSystem
    {
        private readonly ConnectionInfo _connectionInfo;

        /// <summary>
        /// Represents an <see cref="IFileSystem"/> for interacting with an SSH server.
        /// </summary>
        /// <param name="credentials">The credentials to log in with.</param>
        public SftpFileSystem(FileSystemCredentials credentials)
        {
            Console.WriteLine(JObject.FromObject(credentials));
            _connectionInfo = new ConnectionInfo(credentials.Address, credentials.Username, new PasswordAuthenticationMethod(credentials.Username, Encryption.Decode(credentials.Password)));
        }

        /// <inheritdoc/>
        public async Task CreateDirectoryAsync(string path)
        {
            using (SftpClient sftp = new SftpClient(_connectionInfo))
            {
                sftp.Connect();
                if (sftp.IsConnected)
                {
                    string parentDir = string.Empty;
                    foreach (string subPath in path.Split('/'))
                    {
                        parentDir += $"/{subPath}";
                        if (!await sftp.ExistsAsync(parentDir))
                        {
                            await sftp.CreateDirectoryAsync(parentDir);
                        }
                    }
                }

                sftp.Disconnect();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteDirectoryAsync(string path)
        {
            using (SftpClient sftp = new SftpClient(_connectionInfo))
            {
                sftp.Connect();
                if (sftp.IsConnected && await sftp.ExistsAsync(path))
                {
                    await sftp.DeleteDirectoryAsync(path);
                }

                sftp.Disconnect();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteFileAsync(string path)
        {
            using (SftpClient sftp = new SftpClient(_connectionInfo))
            {
                sftp.Connect();
                if (sftp.IsConnected && await sftp.ExistsAsync(path))
                {
                    await sftp.DeleteAsync(path);
                }

                sftp.Disconnect();
            }
        }

        public Task DownloadFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetDirectoryNameAsync(string path)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, SystemFile>> GetFilesAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<SystemFile[]> GetFilesAsync(string path)
        {
            List<SystemFile> list = new();
            using (SftpClient sftp = new SftpClient(_connectionInfo))
            {
                sftp.Connect();
                if (sftp.IsConnected && await sftp.ExistsAsync(path))
                {
                    foreach (ISftpFile file in sftp.ListDirectory(path))
                    {
                        list.Add(new SystemFile(file.FullName)
                        {
                            Name = file.Name,
                            RemotePath = file.FullName,
                            RemoteSize = file.Length
                        });
                    }
                }

                sftp.Disconnect();
            }

            return list.ToArray();
        }

        /// <inheritdoc/>
        public async Task<SystemFile> GetFileAsync(string path)
        {
            SystemFile file = new SystemFile(path);
            using (SftpClient sftp = new SftpClient(_connectionInfo))
            {
                sftp.Connect();
                if (sftp.IsConnected && await sftp.ExistsAsync(path))
                {
                    ISftpFile sf = sftp.Get(path);
                    file = new SystemFile(sf.FullName)
                    {
                        Name = sf.Name,
                        RemotePath = sf.FullName,
                        RemoteSize = sf.Length,
                    };
                }

                sftp.Disconnect();
            }

            return file;
        }

        /// <inheritdoc />
        public async Task<long> PingAsync()
        {
            CancellationTokenSource cts = new();
            Stopwatch sw = Stopwatch.StartNew();
            using (SftpClient sftp = new SftpClient(_connectionInfo))
            {
                await sftp.ConnectAsync(cts.Token);
                if (!sftp.IsConnected) return -1;
                sftp.Disconnect();
            }

            return sw.ElapsedMilliseconds;
        }

        /// <inheritdoc />
        public async Task UploadFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            using SftpClient sftp = new SftpClient(_connectionInfo);
            sftp.Connect();
            if (sftp.IsConnected)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    SystemFile file = files[i];
                    Stopwatch sw = new Stopwatch();
                    progress.Report(ProgressOperation.Uploading, file, i, files.Length);
                    if (await sftp.ExistsAsync(file.RemotePath)) sftp.ChangePermissions(file.RemotePath, 644);

                    string parentDir = string.Empty;
                    foreach (string subPath in file.RemotePath.Split('/'))
                    {
                        parentDir += $"/{subPath}";
                        if (!await sftp.ExistsAsync(parentDir))
                        {
                            await sftp.CreateDirectoryAsync(parentDir);
                        }
                    }

                    await using SftpFileStream createStream = sftp.Create(file.RemotePath);
                    await using FileStream openStream = File.OpenRead(file.LocalPath);
                    await using GZipStream gzipStream = new GZipStream(createStream, CompressionLevel.SmallestSize);
                    await openStream.CopyToAsync(gzipStream);
                    sftp.ChangePermissions(file.RemotePath, 444);

                    Log.Debug($"Uploaded '{file.RemotePath}' in {sw.ElapsedMilliseconds}ms");
                }
            }

            sftp.Disconnect();
        }
    }
}