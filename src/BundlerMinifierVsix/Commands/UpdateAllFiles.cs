using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix.Commands
{
    internal sealed class UpdateAllFiles
    {
        private readonly Package _package;

        private UpdateAllFiles(Package package)
        {
            _package = package;

            var commandService = (OleMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));
            if (commandService != null)
            {
                var menuCommandID = new CommandID(PackageGuids.guidBundlerCmdSet, PackageIds.UpdateSolution);
                var menuItem = new OleMenuCommand(UpdateSelectedBundle, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;

            button.Visible = ProjectHelpers.IsSolutionLoaded();
        }

        public static UpdateAllFiles Instance
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
            Instance = new UpdateAllFiles(package);
        }

        private void UpdateSelectedBundle(object sender, EventArgs e)
        {
            var projects = ProjectHelpers.GetAllProjects();

            foreach (Project project in projects)
            {
                string folder = Path.GetDirectoryName(project.GetRootFolder());
                var configs = GetFiles(folder, Constants.CONFIG_FILENAME);

                foreach (string config in configs)
                {
                    if (!string.IsNullOrEmpty(config))
                        BundleService.Process(config);
                }
            }
        }

        private static List<string> GetFiles(string path, string pattern)
        {
            var files = new List<string>();

            if (path.Contains("node_modules"))
                return files;

            try
            {
                files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                foreach (var directory in Directory.GetDirectories(path))
                    files.AddRange(GetFiles(directory, pattern));
            }
            catch { }

            return files;
        }
    }
}
