// Copyright 2025 Kyle Ebbinga

using SharpShell;
using SharpShell.ServerRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Parallel.Shell.Extensions;

namespace Parallel.Shell
{
    public class ServerManager
    {
        private readonly HashSet<ISharpShellServer> _servers = new HashSet<ISharpShellServer>()
        {
            new FileMenuExtension(),
            new CountLinesExtension()
        };

        // static ServerManager()
        // {
        //     Type[] types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(ISharpShellServer)) && t.IsAbstract).ToArray();
        //     foreach (Type type in types) _servers.Add((ISharpShellServer)Activator.CreateInstance(type));
        // }

        public void RegisterServers()
        {
            Console.WriteLine($"Registering {_servers.Count} servers...");
            foreach (ISharpShellServer server in _servers)
            {
                ServerRegistrationManager.InstallServer(server, RegistrationType.OS64Bit, true);
                ServerRegistrationManager.RegisterServer(server, RegistrationType.OS64Bit);
            }
        }

        public void UnregisterServers()
        {
            Console.WriteLine($"Unregistering {_servers.Count} servers...");
            foreach (ISharpShellServer server in _servers)
            {
                ServerRegistrationManager.UnregisterServer(server, RegistrationType.OS64Bit);
                ServerRegistrationManager.UninstallServer(server, RegistrationType.OS64Bit);
            }
        }
    }
}