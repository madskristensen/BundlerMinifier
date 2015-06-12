using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BundlerMinifier;
using EnvDTE;
using EnvDTE80;

namespace BundlerMinifierVsix.Commands
{
    static class ItemRemoved
    {
        private static ProjectItemsEvents _events;

        public static void Initialize(DTE2 dte)
        {
            Events2 events = dte.Events as Events2;
            _events = events.ProjectItemsEvents;
            _events.ItemRemoved += FileDeleted;
        }

        private static void FileDeleted(ProjectItem item)
        {
            Window2 window = BundlerMinifierPackage._dte.ActiveWindow as Window2;

            if (window == null || window.Type != vsWindowType.vsWindowTypeSolutionExplorer)
            {
                return;
            }

            var items = ProjectHelpers.GetSelectedItems();
            if (items == null || items.Count() != 1)
                return;

            if (item.ContainingProject == null)
                return;

            try
            {
                string fileName = item.Properties.Item("FullPath").Value.ToString();
                string extension = Path.GetExtension(fileName).ToUpperInvariant();

                if (!FileHelpers.SupportedFiles.Contains(extension))
                    return;

                string configFile = FileHelpers.GetConfigFile(item.ContainingProject);

                BundlerMinifierPackage._dispatcher.BeginInvoke(new Action(() =>
                {
                    RemoveBundle(fileName, configFile);
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);
            }
            catch { }
        }

        private static void RemoveBundle(string fileName, string configFile)
        {
            var bundles = Bundler.GetBundles(configFile);
            string friendlyName = Path.GetFileName(fileName);

            foreach (Bundle bundle in bundles)
            {
                if (bundle.GetAbsoluteOutputFile() == fileName)
                {
                    var question = MessageBox.Show($"Do you want to remove {friendlyName} from {FileHelpers.FILENAME}?", "Bundler & Minifier", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                    if (question == DialogResult.OK)
                        Bundler.RemoveBundle(configFile, bundle);

                    break;
                }
            }
        }
    }
}
