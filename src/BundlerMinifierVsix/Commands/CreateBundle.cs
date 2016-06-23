using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BundlerMinifier;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix.Commands
{
    internal sealed class CreateBundle
    {
        private readonly Package _package;

        private CreateBundle(Package package)
        {
            _package = package;

            var commandService = (OleMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));
            if (commandService != null)
            {
                var menuCommandID = new CommandID(PackageGuids.guidBundlerCmdSet, PackageIds.CreateBundleId);
                var menuItem = new OleMenuCommand(AddBundle, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        public static CreateBundle Instance
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
            Instance = new CreateBundle(package);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Enabled = button.Visible = false;

            var files = ProjectHelpers.GetSelectedItemPaths();
            var supported = BundleFileProcessor.IsSupported(files.ToArray());

            if (supported)
            {
                if (files.Count() == 1)
                {
                    var sourceFile = files.First();
                    var project = BundlerMinifierPackage._dte.Solution.FindProjectItem(sourceFile)?.ContainingProject;
                    var configFile = project.GetConfigFile();

                    var bundles = BundleService.IsOutputConfigered(configFile, sourceFile);
                    bool isMinFile = Path.GetFileName(sourceFile).Contains(".min.");

                    if (!bundles.Any() && !isMinFile)
                    {
                        var minFileName = FileHelpers.GetMinFileName(sourceFile);
                        bundles = BundleService.IsOutputConfigered(configFile, minFileName);
                    }

                    if (bundles.Any())
                    {
                        if (isMinFile)
                        {
                            button.Text = Resources.Text.ButtonReMinify;
                        }
                        else
                        {
                            button.Text = Resources.Text.ButtonReBundle;
                        }
                    }
                    else if (BundleFileProcessor.IsFileConfigured(configFile, sourceFile).Any())
                    {
                        supported = false;
                    }
                    else if (!isMinFile)
                    {
                        button.Text = Resources.Text.ButtonMinify;
                    }
                }
                else
                {
                    button.Text = Resources.Text.ButtonBundle;
                }

                button.Visible = button.Enabled = supported;
            }
        }

        private void AddBundle(object sender, EventArgs e)
        {
            var item = ProjectHelpers.GetSelectedItems().FirstOrDefault();

            if (item == null || item.ContainingProject == null)
                return;

            string folder = item.ContainingProject.GetRootFolder();
            string configFile = Path.Combine(folder, Constants.CONFIG_FILENAME);
            IEnumerable<string> files = ProjectHelpers.GetSelectedItemPaths().Select(f => BundlerMinifier.FileHelpers.MakeRelative(configFile, f));
            string inputFile = item.Properties.Item("FullPath").Value.ToString();
            string outputFile = FileHelpers.GetMinFileName(inputFile);

            if (files.Count() > 1)
            {
                outputFile = GetOutputFileName(inputFile, Path.GetExtension(files.First()));
            }
            else
            {
                // Reminify file
                var bundles = BundleFileProcessor.IsFileConfigured(configFile, inputFile);

                if (bundles.Any())
                {
                    BundleService.SourceFileChanged(configFile, inputFile);
                    return;
                }

                var bundles2 = BundleService.IsOutputConfigered(configFile, inputFile);

                if (bundles2.Any())
                {
                    BundleService.Process(configFile, bundles2);
                    return;
                }
            }

            if (string.IsNullOrEmpty(outputFile))
                return;

            BundlerMinifierPackage._dte.StatusBar.Progress(true, Resources.Text.StatusCreatingBundle, 0, 2);

            string relativeOutputFile = BundlerMinifier.FileHelpers.MakeRelative(configFile, outputFile);
            Bundle bundle = CreateBundleFile(files, relativeOutputFile);

            BundleHandler.AddBundle(configFile, bundle);

            BundlerMinifierPackage._dte.StatusBar.Progress(true, Resources.Text.StatusCreatingBundle, 1, 2);

            item.ContainingProject.AddFileToProject(configFile, "None");
            BundlerMinifierPackage._dte.StatusBar.Progress(true, Resources.Text.StatusCreatingBundle, 2, 2);

            BundleService.Process(configFile);
            BundlerMinifierPackage._dte.StatusBar.Progress(false, Resources.Text.StatusCreatingBundle);
            BundlerMinifierPackage._dte.StatusBar.Text = Resources.Text.StatusBundleCreated;

            ProjectEventCommand.Instance?.EnsureProjectIsActive(item.ContainingProject);
        }

        private static Bundle CreateBundleFile(IEnumerable<string> files, string outputFile)
        {
            var bundle = new Bundle
            {
                IncludeInProject = true,
                OutputFileName = outputFile
            };

            bundle.InputFiles.AddRange(files);
            return bundle;
        }
        
        private static string GetOutputFileName(string inputFile, string extension)
        {
            string ext = extension.TrimStart('.');

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.InitialDirectory = Path.GetDirectoryName(inputFile);
                dialog.DefaultExt = ext;
                dialog.FileName = "bundle";
                dialog.Filter = ext.ToUpperInvariant() + " File|*." + ext;

                DialogResult result = dialog.ShowDialog();

                if (result == DialogResult.OK)
                    return dialog.FileName;
            }

            return null;
        }
    }
}
