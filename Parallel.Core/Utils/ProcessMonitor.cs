// Copyright 2025 Kyle Ebbinga

using System.Diagnostics;
using System.Timers;

namespace Parallel.Core.Utils
{
    /// <summary>
    /// Allows for easy use of monitoring system processes.
    /// </summary>
    public class ProcessMonitor : IDisposable
    {
        private readonly Process _process;
        private System.Timers.Timer? _timer;
        private DateTime _oldTime = DateTime.UtcNow;
        private TimeSpan _oldUsage;

        /// <summary>
        /// The timestamp of when the associated process started running.
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// The duration of time that the associated process has been running.
        /// </summary>
        public TimeSpan Uptime { get; private set; }

        /// <summary>
        /// The percent of cpu currently being utilized by the associated process.
        /// </summary>
        public double CpuUsage { get; private set; }

        /// <summary>
        /// The highest recorded percent of cpu utilized by the associated process.
        /// </summary>
        public double PeakCpuUsage { get; private set; }

        /// <summary>
        /// The amount of memory, in bytes, being used by the associated process.
        /// </summary>
        public double RamUsage { get; private set; }

        /// <summary>
        /// The highest recorded amount of memory, in bytes, being used by the associated process.
        /// </summary>
        public double PeakRamUsage { get; private set; }

        /// <summary>
        /// Initializes new instance of the <see cref="ProcessMonitor"/> class with the current process.
        /// </summary>
        public ProcessMonitor()
        {
            _process = Process.GetCurrentProcess();
            StartTime = _process.StartTime;
        }

        /// <summary>
        /// Initializes new instance of the <see cref="ProcessMonitor"/> class with a provided process.
        /// </summary>
        /// <param name="process">The process to monitor.</param>
        public ProcessMonitor(Process process)
        {
            _process = process;
            StartTime = process.StartTime;
        }


        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Refresh();
        }

        /// <summary>
        /// Starts refreshing the system process information of the associated <see cref="Process"/> with an interval between updates.
        /// </summary>
        /// <param name="interval">The time, in milliseconds, in which to request a system update.</param>
        public void Start(double interval = 5000)
        {
            _oldTime = DateTime.UtcNow;
            _oldUsage = _process.TotalProcessorTime;
            _timer = new System.Timers.Timer();
            _timer.Elapsed += Timer_Elapsed;
            _timer.Interval = interval;
            _timer.Start();
        }

        /// <summary>
        /// Stops refreshing the system process information.
        /// </summary>
        public void Stop()
        {
            _timer?.Stop();
        }

        /// <summary>
        /// Refreshes the information cached in the associated process.
        /// <para>Its important to note that CPU utilization works by calculating the amount of time spent processing in a certain timespan.
        /// Calling this function more frequently will provide a more accurate CPU utilization result.</para>
        /// </summary>
        public void Refresh()
        {
            _process.Refresh();
            Uptime = DateTime.Now - _process.StartTime;

            DateTime endTime = DateTime.UtcNow;
            TimeSpan endUsage = _process.TotalProcessorTime;

            double timeMs = (endTime - _oldTime).TotalMilliseconds;
            double usageMs = (endUsage - _oldUsage).TotalMilliseconds;

            _oldTime = endTime;
            _oldUsage = endUsage;

            CpuUsage = (usageMs / (Environment.ProcessorCount * timeMs)) * 100;
            RamUsage = _process.PrivateMemorySize64;

            if (CpuUsage > PeakCpuUsage) PeakCpuUsage = CpuUsage;
            if (RamUsage > PeakRamUsage) PeakRamUsage = RamUsage;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _process?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}