// Copyright 2025 Kyle Ebbinga

using Newtonsoft.Json;
using Org.BouncyCastle.Math.EC;
using Parallel.Core.IO;

namespace Parallel.Core.Settings
{
    /// <summary>
    ///
    /// </summary>
    public class ParallelSettings
    {
        /// <summary>
        /// The location to the application configuration file.
        /// </summary>
        private static string ConfigFile { get; } = Path.Combine(PathBuilder.ProgramData, "settings.json");

        /// <summary>
        /// The location of files for different file system credentials./>.
        /// </summary>
        public static string ProfilesDir { get; } = Path.Combine(PathBuilder.ProgramData, "Profiles");

        /// <summary>
        /// The address that will accept incoming commands.
        /// <para>Default: 127.0.0.1</para>
        /// </summary>
        public string Address { get; set; } = "127.0.0.1";

        /// <summary>
        /// The port number to listen for commands on.
        /// <para>Default: 8192</para>
        /// </summary>
        public int ListenerPort { get; set; } = 8192;

        /// <summary>
        /// The profiles to use.
        /// <para>The CLI defaults to the first in the list.</para>
        /// </summary>
        public HashSet<string> Profiles { get; } = new HashSet<string>();


        /// <summary>
        /// Loads settings from a file.
        /// </summary>
        public static ParallelSettings Load()
        {
            Log.Debug($"Loading config file: {ConfigFile}");
            if (File.Exists(ConfigFile))
            {
                string json = File.ReadAllText(ConfigFile);
                return JsonConvert.DeserializeObject<ParallelSettings>(json);
            }
            else
            {
                return new ParallelSettings();
            }
        }

        /// <summary>
        /// Saves settings to a file.
        /// </summary>
        public void Save()
        {
            Log.Debug($"Saving config file: {ConfigFile}");
            if (!Directory.Exists(PathBuilder.ProgramData)) Directory.CreateDirectory(PathBuilder.ProgramData);
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="action"></param>
        public void ForEachProfile(Action<ProfileConfig> action)
        {
            foreach (ProfileConfig? profile in Profiles.Select(ProfileConfig.Load))
            {
                if (profile != null) action(profile);
            }
        }
    }
}