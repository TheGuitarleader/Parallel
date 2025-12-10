// Copyright 2025 Kyle Ebbinga

using Newtonsoft.Json;
using Org.BouncyCastle.Math.EC;
using Parallel.Core.IO;

namespace Parallel.Core.Settings
{
    /// <summary>
    ///
    /// </summary>
    public class ParallelConfig
    {
        /// <summary>
        /// The location to the application configuration file.
        /// </summary>
        private static string ConfigFile { get; } = Path.Combine(PathBuilder.ProgramData, "Configuration.json");

        /// <summary>
        /// The location of files for different file system credentials./>.
        /// </summary>
        public static string VaultsDir { get; } = Path.Combine(PathBuilder.ProgramData, "Vaults");

        public static ParallelOptions Options { get; } = new ParallelOptions
        {
            MaxDegreeOfParallelism = Load().MaxConcurrentProcesses
        };

        public static int MaxUploads { get; } = Load().MaxConcurrentUploads;

        // /// <summary>
        // /// The address that will accept incoming commands.
        // /// <para>Default: 127.0.0.1</para>
        // /// </summary>
        // public string Address { get; set; } = "127.0.0.1";
        //
        // /// <summary>
        // /// The port number to listen for commands on.
        // /// <para>Default: 8192</para>
        // /// </summary>
        // public int ListenerPort { get; set; } = 8192;

        /// <summary>
        /// Gets or sets the maximum number of concurrent vaults that can run.
        /// <para>Default: 2</para>
        /// </summary>
        public int MaxConcurrentVaults { get; set; } = 2;

        /// <summary>
        /// Gets or sets the maximum number of concurrent processes that can run.
        /// <para>Default: The processor count.</para>
        /// </summary>
        public int MaxConcurrentProcesses { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Gets or sets the maximum number of concurrent processes that can run.
        /// <para>Default: 4</para>
        /// </summary>
        public int MaxConcurrentUploads { get; set; } = 4;

        /// <summary>
        /// The amount of time, in days, to hold a file before it can be cleaned.
        /// <para>Default: 90 days</para>
        /// </summary>
        public int RetentionPeriod { get; set; } = 90;

        /// <summary>
        /// A collection of directories to be cleaned on the machine.
        /// <para>It's important to note that when using the service host this will delete any available file.</para>
        /// <para>Default: Empty</para>
        /// </summary>
        public HashSet<string> CleanDirectories { get; } = CreateCleanDirectories();

        /// <summary>
        /// The profiles to use.
        /// <para>When pulling, the CLI defaults to the first in the list.</para>
        /// </summary>
        public HashSet<LocalVaultConfig> Vaults { get; } = [];


        /// <summary>
        /// Loads settings from a file.
        /// </summary>
        public static ParallelConfig Load()
        {
            Log.Debug($"Loading config file: {ConfigFile}");
            if (!File.Exists(ConfigFile)) return new ParallelConfig();

            string json = File.ReadAllText(ConfigFile);
            ParallelConfig? config = JsonConvert.DeserializeObject<ParallelConfig>(json);
            return config ?? new ParallelConfig();
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
        /// Creates a default array of cleanable directories.
        /// </summary>
        /// <returns></returns>
        private static HashSet<string> CreateCleanDirectories()
        {
            return
            [
                Path.GetTempPath(),
            ];
        }

        /// <summary>
        /// Asynchronously runs an <see cref="Action{T}"/> for each <see cref="LocalVaultConfig"/> using the <see cref="MaxConcurrentVaults"/> limiter.
        /// </summary>
        /// <param name="actionAsync"></param>
        /// <param name="cancellationToken"></param>
        public async Task ForEachVaultAsync(Func<LocalVaultConfig, Task> actionAsync, CancellationToken cancellationToken = default)
        {
            ParallelOptions options = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxConcurrentVaults,
                CancellationToken = cancellationToken
            };

            await System.Threading.Tasks.Parallel.ForEachAsync(Vaults.Where(v => v.Enabled), options, async (vault, ct) =>
            {
                await actionAsync(vault);
            });
        }

        /// <summary>
        /// Gets a <see cref="LocalVaultConfig"/> by either its id or name.
        /// </summary>
        /// <param name="vault"></param>
        /// <returns></returns>
        public static LocalVaultConfig? GetVault(string vault)
        {
            return Load().Vaults.FirstOrDefault(v => v.Id.Equals(vault, StringComparison.OrdinalIgnoreCase) || v.Name.Equals(vault, StringComparison.OrdinalIgnoreCase));
        }
    }
}