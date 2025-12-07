// Copyright 2025 Kyle Ebbinga

using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Parallel.Shell.Extensions
{
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.AllFiles)]
    internal class FileMenuExtension : SharpContextMenu
    {
        protected override bool CanShowMenu()
        {
            return true;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem parallelToolStripMenuItem = new ToolStripMenuItem();
            parallelToolStripMenuItem.Name = "parallelToolStripMenuItem";
            parallelToolStripMenuItem.Text = "Parallel";
            contextMenu.Items.Add(parallelToolStripMenuItem);
            return contextMenu;
        }
    }
}