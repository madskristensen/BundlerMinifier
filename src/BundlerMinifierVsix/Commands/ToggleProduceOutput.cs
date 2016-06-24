using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix.Commands
{
    internal sealed class ToggleProduceOutput
    {
        private readonly Package _package;

        private ToggleProduceOutput(Package package)
        {
            _package = package;

            var commandService = (OleMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));
            if (commandService != null)
            {
                var menuCommandID = new CommandID(PackageGuids.guidBundlerCmdSet, PackageIds.SuppressOutput);
                var menuItem = new OleMenuCommand(Execute, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        public static ToggleProduceOutput Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new ToggleProduceOutput(package);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var files = ProjectHelpers.GetSelectedItemPaths();
            button.Visible = button.Enabled = false;

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
                    button.Checked = button.Visible && BundleService.IsOutputProduced(config);
                }
            }
            else
            {
                button.Visible = button.Enabled = files.Count() == 1 && Path.GetFileName(files.First()) == Constants.CONFIG_FILENAME;
                button.Checked = button.Visible && BundleService.IsOutputProduced(files.First());
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var project = ProjectHelpers.GetActiveProject();
            var configFile = project?.GetConfigFile();

            if (!string.IsNullOrEmpty(configFile))
            {
                BundleService.ToggleOutputProduction(configFile, !button.Checked);
                ProjectEventCommand.Instance.EnsureProjectIsActive(project);
            }
        }
    }
}
