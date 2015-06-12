using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix.Commands
{
    internal sealed class UpdateBundle
    {
        private readonly Package _package;

        private UpdateBundle(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(GuidList.guidBundlerCmdSet, PackageCommands.UpdateBundle);
                var menuItem = new OleMenuCommand(UpdateSelectedBundle, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var files = ProjectHelpers.GetSelectedItemPaths();

            button.Visible = files.Count() == 1 && Path.GetFileName(files.First()) == FileHelpers.FILENAME;
        }

        public static UpdateBundle Instance
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
            Instance = new UpdateBundle(package);
        }

        private void UpdateSelectedBundle(object sender, EventArgs e)
        {
            var file = ProjectHelpers.GetSelectedItemPaths().ElementAt(0);
            var item = ProjectHelpers.GetSelectedItems().ElementAt(0);

            BundleService.Process(file);
        }
    }
}
