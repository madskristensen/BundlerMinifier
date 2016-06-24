using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix.Commands
{
    internal sealed class OpenSettings
    {
        private readonly Package _package;

        private OpenSettings(Package package)
        {
            _package = package;

            var commandService = (OleMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));
            if (commandService != null)
            {
                var menuCommandID = new CommandID(PackageGuids.guidBundlerCmdSet, PackageIds.OpenSettings);
                var menuItem = new OleMenuCommand(Execute, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            var files = ProjectHelpers.GetSelectedItemPaths();
            int count = files.Count();

            if (count == 0) // Project
            {
                var project = ProjectHelpers.GetActiveProject();

                if (project == null)
                    return;

                string config = project.GetConfigFile();

                if (!string.IsNullOrEmpty(config) && File.Exists(config))
                {
                    button.Visible = button.Enabled = true;
                }
            }
            else
            {
                button.Visible = button.Enabled = count == 1 && Path.GetFileName(files.First()) == Constants.CONFIG_FILENAME;
            }
        }

        public static OpenSettings Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new OpenSettings(package);
        }

        private void Execute(object sender, EventArgs e)
        {
            BundlerMinifierPackage._instance.ShowOptionPage(typeof(Options));
        }
    }
}
