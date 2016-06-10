using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using BundlerMinifier;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix.Commands
{
    internal sealed class ClearOutputFiles
    {
        private readonly Package _package;

        private ClearOutputFiles(Package package)
        {
            _package = package;

            var commandService = (OleMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));
            if (commandService != null)
            {
                var menuCommandID = new CommandID(PackageGuids.guidBundlerCmdSet, PackageIds.ClearOutputFiles);
                var menuItem = new OleMenuCommand(UpdateSelectedBundle, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        public static ClearOutputFiles Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider
        {
            get
            {
                return _package;
            }
        }

        public static void Initialize(Package package)
        {
            Instance = new ClearOutputFiles(package);
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
                    button.Visible = button.Enabled = true;
            }
            else
            {
                button.Visible = button.Enabled = files.Count() == 1 && Path.GetFileName(files.First()) == Constants.CONFIG_FILENAME;
            }
        }

        private void UpdateSelectedBundle(object sender, EventArgs e)
        {
            var configFile = ProjectHelpers.GetSelectedItemPaths().FirstOrDefault();

            if (!File.Exists(configFile))
            {
                return;
            }

            var bundles = BundleHandler.GetBundles(configFile);

            BundleFileProcessor processor = new BundleFileProcessor();
            processor.Clean(configFile, bundles);
        }

        private void Deletefile(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    if (!ProjectHelpers.DeleteFileFromProject(file))
                    {
                        BundlerMinifier.FileHelpers.RemoveReadonlyFlagFromFile(file);
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}
