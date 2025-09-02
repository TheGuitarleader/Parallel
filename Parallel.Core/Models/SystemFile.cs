// Copyright 2025 Kyle Ebbinga

using System.Data;
using Parallel.Core.Data;
using Parallel.Core.Diagnostics;
using Parallel.Core.Security;
using Parallel.Core.Utils;

namespace Parallel.Core.Models
{
    /// <summary>
    /// Represents a file managed by Parallel.
    /// </summary>
    public class SystemFile
    {
        /// <summary>
        /// The unique identifier of the file.
        /// </summary>
        public string Id { get; } = string.Empty;

        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The path of the file on the local machine.
        /// </summary>
        public string LocalPath { get; set; } = string.Empty;

        /// <summary>
        /// The path of the file in the backup file system.
        /// </summary>
        public string RemotePath { get; set; } = string.Empty;

        /// <summary>
        /// The time the current file was last written to.
        /// </summary>
        public UnixTime LastWrite { get; set; } = UnixTime.Now;

        /// <summary>
        /// The time the file was either last saved or deleted.
        /// </summary>
        public UnixTime LastUpdate { get; set; } = UnixTime.Now;

        /// <summary>
        /// The size, in bytes, of the file on the local machine.
        /// </summary>
        public long LocalSize { get; set; } = 0;

        /// <summary>
        /// The size, in bytes, of the file in the remote backup.
        /// </summary>
        public long RemoteSize { get; set; } = 0;

        /// <summary>
        /// The category of the file.
        /// </summary>
        public FileCategory Type { get; set; } = FileCategory.Other;

        /// <summary>
        /// If the file is currently hidden on the local machine.
        /// </summary>
        public bool Hidden { get; set; } = false;

        /// <summary>
        /// If the file is currently read-only on the local machine.
        /// </summary>
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// If the file is currently deleted on the local machine.
        /// </summary>
        public bool Deleted { get; set; } = false;

        /// <summary>
        /// The checksum used to check if the file has changed.
        /// </summary>
        public string? CheckSum { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="SystemFile"/> class with default properties.
        /// </summary>
        public SystemFile(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            Id = HashGenerator.CreateSHA1(path);
            Name = fileInfo.Name;
            LocalPath = fileInfo.FullName;
            LocalSize = fileInfo.Length;
            RemoteSize = fileInfo.Length;
            LastWrite = new UnixTime(fileInfo.LastWriteTime);
            LastUpdate = UnixTime.Now;
            Type = FileTypes.GetFileCategory(Path.GetExtension(fileInfo.Name));
            Hidden = fileInfo.Attributes.HasFlag(FileAttributes.Hidden);
            ReadOnly = fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly);
            Deleted = !fileInfo.Exists;
            CheckSum = HashGenerator.CheckSum(path);
        }

        public SystemFile(string localPath, string remotePath)
        {
            LocalPath = localPath;
            RemotePath = remotePath;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemFile"/> class.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="localpath"></param>
        /// <param name="remotepath"></param>
        /// <param name="lastwrite"></param>
        /// <param name="lastupdate"></param>
        /// <param name="localsize"></param>
        /// <param name="remotesize"></param>
        /// <param name="type"></param>
        /// <param name="hidden"></param>
        /// <param name="readOnly"></param>
        /// <param name="deleted"></param>
        /// <param name="encrypted"></param>
        /// <param name="salt"></param>
        /// <param name="iv"></param>
        /// <param name="checksum"></param>
        public SystemFile(string id, string name, string localpath, string remotepath, long lastwrite, long lastupdate, long localsize, long remotesize, string type, long hidden, long readOnly, long deleted, string checksum)
        {
            Id = id;
            Name = name;
            LocalPath = localpath;
            RemotePath = remotepath;
            LastWrite = UnixTime.FromMilliseconds(lastwrite);
            LastUpdate = UnixTime.FromMilliseconds(lastupdate);
            LocalSize = localsize;
            RemoteSize = remotesize;
            Hidden = Converter.ToBool(hidden);
            ReadOnly = Converter.ToBool(readOnly);
            Deleted = Converter.ToBool(deleted);
            CheckSum = checksum;
        }

        /// <summary>
        /// Determines if this instance and another <see cref="SystemFile"/> have the same values.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True if equal, otherwise false.</returns>
        public bool Equals(SystemFile value)
        {
            bool?[] results =
            [
                this?.Id != null && value?.Id != null ? this.Id.Equals(value.Id) : (bool?)null,
                this?.Name != null && value?.Name != null ? this.Name.Equals(value.Name) : (bool?)null,
                this?.LocalPath != null && value?.LocalPath != null ? this.LocalPath.Equals(value.LocalPath) : (bool?)null,
                this?.RemotePath != null && value?.RemotePath != null ? this.RemotePath.Equals(value.RemotePath) : (bool?)null,
                value?.LocalSize != null ? this.LocalSize.Equals(value.LocalSize) : (bool?)null,
                value?.RemoteSize != null ? this.RemoteSize.Equals(value.RemoteSize) : (bool?)null,
                value?.Type != null ? this.Type.Equals(value.Type) : (bool?)null,
                value?.Hidden != null ? this.Hidden.Equals(value.Hidden) : (bool?)null,
                value?.ReadOnly != null ? this.ReadOnly.Equals(value.ReadOnly) : (bool?)null,
                value?.Deleted != null ? this.Deleted.Equals(value.Deleted) : (bool?)null,
                this?.CheckSum != null && value?.CheckSum != null ? this.CheckSum.SequenceEqual(value.CheckSum) : (bool?)null,
            ];

            return results.All(b => b != null && (bool)b);
        }
    }
}