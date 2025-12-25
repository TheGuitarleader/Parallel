using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using Parallel.Core.Settings;
using Serilog;

namespace Parallel.Gui.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();
            Log.Information($"{assembly.Name} [Version {assembly.Version}]");

            if (!ParallelConfig.CanStartGuiInstance())
            {
                Log.Warning("Can't start new instance");
                IMsBox<ButtonResult> msgBox = MessageBoxManager.GetMessageBoxStandard("Instance Already Running!", "An instance of Parallel is already running!", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
                await msgBox.ShowWindowDialogAsync(this);
                Environment.Exit(1);
            }
        }
    }
}