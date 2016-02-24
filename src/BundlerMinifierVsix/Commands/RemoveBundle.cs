using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using BundlerMinifier;

namespace BundlerMinifierVsix.Commands
{
    internal sealed class RemoveBundle
    {
        private readonly Package _package;

        private RemoveBundle(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(PackageGuids.guidBundlerCmdSet, PackageIds.RemoveBundle);
                var menuItem = new OleMenuCommand(RemoveConfig, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        private IEnumerable<Bundle> _bundles;

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var items = ProjectHelpers.GetSelectedItems();

            button.Visible = false;

            if (items.Count() != 1)
                return;

            var item = items.First();

            if (item == null || item.ContainingProject == null || item.Properties == null)
                return;

            var sourceFile = item.Properties.Item("FullPath").Value.ToString();

            if (!BundleService.IsSupported(sourceFile))
                return;

            string configFile = item.ContainingProject.GetConfigFile();

            _bundles = BundleService.IsOutputConfigered(configFile, sourceFile);

            button.Visible = _bundles.Any();
        }

        public static RemoveBundle Instance
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
            Instance = new RemoveBundle(package);
        }

        private void RemoveConfig(object sender, EventArgs e)
        {
            var question = MessageBox.Show($"This will remove the file from {Constants.CONFIG_FILENAME}.\r\rDo you want to continue?", Vsix.Name, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

            if (question == DialogResult.Cancel)
                return;

            try
            {
                foreach (Bundle bundle in _bundles)
                {
                    BundleHandler.RemoveBundle(bundle.FileName, bundle);
                }

                Telemetry.TrackEvent("VS remove bundle");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                BundlerMinifierPackage._dte.StatusBar.Text = $"Could not update {Constants.CONFIG_FILENAME}. Make sure it's not write-protected or has syntax errors.";
            }
        }
    }
}
