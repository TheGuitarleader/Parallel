using Avalonia;
using System;
using System.Reflection;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using Serilog;

namespace Parallel.Gui
{
    internal sealed class Program
    {
        private static readonly LogEventTracker EventTracker = new LogEventTracker();
        internal static ParallelConfig Settings = new ParallelConfig();

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
        }
    }
}