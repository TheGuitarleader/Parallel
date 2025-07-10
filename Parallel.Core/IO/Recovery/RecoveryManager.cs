// Copyright 2025 Kyle Ebbinga

using System.Data;
using Parallel.Core.Database;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Recovery
{
    /// <summary>
    /// Represents the way to manage recovery points on the system.
    /// </summary>
    public class RecoveryManager
    {
        private readonly string _dbPath = Path.Combine(PathBuilder.ProgramData, Environment.MachineName + ".db");

        public IDatabase Database { get; set; }
        public IFileSystem FileSystem { get; set; }
        public ProfileConfig Profile { get; set; }
        public string MachineName { get; } = Environment.MachineName;
        public string RootFolder { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecoveryManager"/> class.
        /// </summary>
        /// <param name="credentials"></param>
        public RecoveryManager(ProfileConfig profile)
        {
            Profile = profile;
            Database = DatabaseConnection.CreateNew(profile);
            FileSystem = FileSystemManager.CreateNew(profile.FileSystem);
        }

        public bool Initialize()
        {
            try
            {
                Database = DatabaseConnection.CreateNew(Profile);
                bool fsInit = (FileSystem != null) && FileSystem.PingAsync().Result >= 0;
                Profile.IgnoreDirectories.Add(Profile.FileSystem.RootDirectory);
                if (Profile != null) Profile.SaveToFile();
                return fsInit;
            }
            catch (Exception ex)
            {
                Log.Error(ex.GetBaseException().ToString());
                return false;
            }
        }

        /// <summary>
        /// Loads a <see cref="RecoveryPoint"/> to restore the 
        /// </summary>
        /// <param name="recoveryPoint"></param>
        public void Load(RecoveryPoint recoveryPoint)
        {

        }

        /// <summary>
        /// Saves the current file system state as a <see cref="RecoveryPoint"/>.
        /// </summary>
        /// <returns></returns>
        public RecoveryPoint Save()
        {
            RecoveryPoint rp = new(Profile.BackupDirectories, Profile.IgnoreDirectories);
            DataTable dt = Database.GetFiles();
            foreach (DataRow row in dt.Rows)
            {
                SystemFile lf = new SystemFile(row);
                if (lf.IsDeleted)
                {
                    rp.DeletedFiles.Add(lf);
                }
                else
                {
                    rp.LocalFiles.Add(lf);
                }
            }

            return rp;
        }
    }
}