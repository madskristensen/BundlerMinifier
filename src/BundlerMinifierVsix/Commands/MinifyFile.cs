using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using BundlerMinifier;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix.Commands
{
    internal sealed class MinifyFile
    {
        private readonly Package _package;

        private MinifyFile(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(GuidList.guidBundlerCmdSet, PackageCommands.MinifyFile);
                var menuItem = new OleMenuCommand(UpdateSelectedBundle, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var files = ProjectHelpers.GetSelectedItemPaths();

            if (files.Count() != 1)
            {
                button.Visible = false;
                return;
            }

            string file = files.First();
            string fileName = Path.GetFileName(file);
            string ext = Path.GetExtension(fileName).ToUpperInvariant();
            string minFile;
            bool hasMinFile = FileHelpers.HasMinFile(file, out minFile);

            button.Visible = !fileName.Contains(".min.") && !hasMinFile && FileHelpers.SupportedFiles.Contains(ext);
        }

        public static MinifyFile Instance
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
            Instance = new MinifyFile(package);
        }

        private void UpdateSelectedBundle(object sender, EventArgs e)
        {
            string file = ProjectHelpers.GetSelectedItemPaths().First();

            if (!string.IsNullOrEmpty(file))
                Minify(file);
        }

        public void Minify(string file)
        {
            BundleService.MinifyFile(file);
        }
    }
}
