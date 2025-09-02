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
        /// <para>Default: Half the processor count.</para>
        /// </summary>
        public int MaxConcurrentProcesses { get; set; } = Math.Clamp(Environment.ProcessorCount / 2, 1, Environment.ProcessorCount);

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
            if (File.Exists(ConfigFile))
            {
                string json = File.ReadAllText(ConfigFile);
                return JsonConvert.DeserializeObject<ParallelConfig>(json);
            }
            else
            {
                return new ParallelConfig();
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

            await System.Threading.Tasks.Parallel.ForEachAsync(Vaults, options, async (vault, ct) =>
            {
                await actionAsync(vault);
            });
        }
    }
}