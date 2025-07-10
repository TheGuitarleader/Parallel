// Copyright 2025 Kyle Ebbinga

using System.Data;
using Parallel.Core.Data;
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
        public bool IsHidden { get; set; } = false;

        /// <summary>
        /// If the file is currently read-only on the local machine.
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// If the file is currently deleted on the local machine.
        /// </summary>
        public bool IsDeleted { get; set; } = false;


        /// <summary>
        /// Initializes a new instance of the <see cref="SystemFile"/> class with default properties.
        /// </summary>
        public SystemFile(string path)
        {
            Id = HashGenerator.CreateSHA1(path);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemFile"/> class from a <see cref="FileInfo"/>.
        /// </summary>
        /// <param name="fileInfo"></param>
        public SystemFile(FileInfo fileInfo)
        {
            Id = HashGenerator.CreateSHA1(fileInfo.FullName);
            Name = fileInfo.Name;
            LocalPath = fileInfo.FullName;
            LocalSize = fileInfo.Length;
            RemoteSize = fileInfo.Length;
            Type = FileTypes.GetFileCategory(Path.GetExtension(fileInfo.Name));
            LastWrite = new UnixTime(fileInfo.LastWriteTime);
            LastUpdate = UnixTime.Now;
            IsDeleted = !fileInfo.Exists;

            if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
            {
                IsHidden = true;
            }

            if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                IsReadOnly = true;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemFile"/> class from a <see cref="SystemFile"/>.
        /// </summary>
        /// <param name="row"></param>
        public SystemFile(DataRow row)
        {
            Id = row.Field<string>("id");
            Name = row.Field<string>("name");
            LocalPath = row.Field<string>("localpath");
            RemotePath = row.Field<string>("remotepath");
            LocalSize = Convert.ToInt64(row.Field<object>("localsize"));
            RemoteSize = Convert.ToInt64(row.Field<object>("remotesize"));
            LastWrite = UnixTime.FromMilliseconds(row.Field<long>("lastwrite"));
            LastUpdate = UnixTime.FromMilliseconds(row.Field<long>("lastupdate"));
            Type = (FileCategory)Enum.Parse(typeof(FileCategory), row.Field<string>("type"));
            IsHidden = Converter.ToBool(Convert.ToInt32(row.Field<object>("hidden")));
            IsReadOnly = Converter.ToBool(Convert.ToInt32(row.Field<object>("readonly")));
            IsDeleted = Converter.ToBool(Convert.ToInt32(row.Field<object>("deleted")));
        }

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
                value?.IsHidden != null ? this.IsHidden.Equals(value.IsHidden) : (bool?)null,
                value?.IsReadOnly != null ? this.IsReadOnly.Equals(value.IsReadOnly) : (bool?)null,
                value?.IsDeleted != null ? this.IsDeleted.Equals(value.IsDeleted) : (bool?)null
            ];

            return results.All(b => b != null && (bool)b);
        }
    }
}