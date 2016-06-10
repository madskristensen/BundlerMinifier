using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Forms;
using BundlerMinifier;
using Microsoft.VisualStudio.Shell;

namespace BundlerMinifierVsix.Commands
{
    internal sealed class RemoveBundle
    {
        private readonly Package _package;
        private IEnumerable<Bundle> _bundles;

        private RemoveBundle(Package package)
        {
            _package = package;

            var commandService = (OleMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));
            if (commandService != null)
            {
                var menuCommandID = new CommandID(PackageGuids.guidBundlerCmdSet, PackageIds.RemoveBundle);
                var menuItem = new OleMenuCommand(RemoveConfig, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
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

            if (!BundleFileProcessor.IsSupported(sourceFile))
                return;

            string configFile = item.ContainingProject.GetConfigFile();

            _bundles = BundleService.IsOutputConfigered(configFile, sourceFile);

            button.Visible = _bundles.Any();
        }

        private void RemoveConfig(object sender, EventArgs e)
        {
            string prompt = Resources.Text.promptRemoveBundle.AddParams(Constants.CONFIG_FILENAME);
            var question = MessageBox.Show(prompt, Vsix.Name, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

            if (question == DialogResult.Cancel)
                return;

            try
            {
                foreach (Bundle bundle in _bundles)
                {
                    BundleHandler.RemoveBundle(bundle.FileName, bundle);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                BundlerMinifierPackage._dte.StatusBar.Text = Resources.Text.ErrorRemoveBundle.AddParams(Constants.CONFIG_FILENAME);
            }
        }
    }
}
