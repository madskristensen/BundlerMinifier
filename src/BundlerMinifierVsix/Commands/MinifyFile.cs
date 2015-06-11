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

            FileMinifier.BeforeWritingMinFile += BeforeWritingMinFile;
            FileMinifier.AfterWritingMinFile += AfterWritingMinFile;
            BundleMinifier.BeforeWritingMinFile += BeforeWritingMinFile;
            BundleMinifier.AfterWritingMinFile += AfterWritingMinFile;
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

            string fileName = Path.GetExtension(files.ElementAt(0));
            string ext = fileName.ToUpperInvariant();

            button.Visible = !fileName.Contains(".min.") && _allowed.Contains(ext);
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
            string minFile;
            bool minFileExist = FileHelpers.HasMinFile(file, out minFile);

            string mapFile;
            bool mapFileExist = FileHelpers.HasSourceMap(minFile, out mapFile);

            bool produceSourceMap = (minFileExist && mapFileExist) || (!minFileExist && !mapFileExist);

            MinificationResult result = FileMinifier.MinifyFile(file, produceSourceMap);

            // Source maps
            if (produceSourceMap && !string.IsNullOrEmpty(result.SourceMap))
            {
                mapFile = minFile + ".map";
                ProjectHelpers.CheckFileOutOfSourceControl(mapFile);
                File.WriteAllText(mapFile, result.SourceMap, new UTF8Encoding(true));

                if (!mapFileExist)
                    ProjectHelpers.AddNestedFile(minFile, mapFile);
            }
        }

        private void AfterWritingMinFile(object sender, MinifyFileEventArgs e)
        {
            ProjectHelpers.AddNestedFile(e.OriginalFile, e.ResultFile);
        }

        private void BeforeWritingMinFile(object sender, MinifyFileEventArgs e)
        {
            ProjectHelpers.CheckFileOutOfSourceControl(e.ResultFile);
        }
    }
}
