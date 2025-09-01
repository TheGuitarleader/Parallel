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
        public static string VaultsDir { get; } = Path.Combine(PathBuilder.ProgramData, "Vaults");

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
        public HashSet<string> Vaults { get; } = new HashSet<string>();


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
        public void ForEachVault(Action<VaultConfig> action)
        {
            foreach (string path in Directory.GetFiles(VaultsDir, "*.json", SearchOption.TopDirectoryOnly))
            {
                VaultConfig? vault = VaultConfig.Load(path);
                if (vault != null) action(vault);
            }
        }

        /// <summary>
        /// Asynchronously runs an <see cref="Action{T}"/> for each <see cref="VaultConfig"/> with a default of 3 at a time.
        /// </summary>
        /// <param name="actionAsync"></param>
        /// <param name="maxDegreeOfParallelism"></param>
        public static async Task ForEachVaultAsync(Func<VaultConfig, Task> actionAsync, int maxDegreeOfParallelism = 3)
        {
            string[] vaultPaths = Directory.GetFiles(VaultsDir, "*.json", SearchOption.TopDirectoryOnly);
            SemaphoreSlim semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            IEnumerable<Task> tasks = vaultPaths.Select(path => Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    VaultConfig? vault = VaultConfig.Load(path);
                    if (vault != null)
                    {
                        await actionAsync(vault);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));

            await Task.WhenAll(tasks);
        }
    }
}