using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix.Commands
{
    internal sealed class UpdateAllFiles
    {
        private readonly Package _package;

        private UpdateAllFiles(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
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
                string config = project.GetConfigFile();

                if (!string.IsNullOrEmpty(config))
                    BundleService.Process(config);
            }
        }
    }
}
