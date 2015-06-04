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

        private static string[] _allowed = new[] { ".JS", ".CSS", ".HTML", ".HTM" };

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var files = ProjectHelpers.GetSelectedItemPaths();

            if (files.Count() != 1)
            {
                button.Visible = false;
                return;
            }

            string ext = Path.GetExtension(files.ElementAt(0)).ToUpperInvariant();

            button.Visible = _allowed.Contains(ext);
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
            string file = ProjectHelpers.GetSelectedItemPaths().ElementAt(0);

            if (!string.IsNullOrEmpty(file))
                Minify(file);
        }

        public void Minify(string file)
        {
            ProjectItem item = BundlerMinifierPackage._dte.Solution.FindProjectItem(file);
            
            string ext = Path.GetExtension(file);

            string result = Minifier.MinifyFile(file);

            string minFile;
            bool minFileExist = FileHelpers.HasMinFile(file, out minFile);
            
            if (minFileExist)
                ProjectHelpers.CheckFileOutOfSourceControl(minFile);

            File.WriteAllText(minFile, result, new UTF8Encoding(true));

            if (!minFileExist && item != null && item.ContainingProject != null)
                ProjectHelpers.AddNestedFile(file, minFile);
        }
    }
}
