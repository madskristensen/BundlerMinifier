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
            if (item.ContainingProject == null)
                return;

            try
            {
                string fileName = item.Properties.Item("FullPath").Value.ToString();
                string extension = Path.GetExtension(fileName).ToUpperInvariant();

                if (!FileHelpers.SupportedFiles.Contains(extension))
                    return;

                string configFile = FileHelpers.GetConfigFile(item.ContainingProject);

                RemoveBundle(fileName, configFile);
            }
            catch { }
        }

        private static void RemoveBundle(string fileName, string configFile)
        {
            var bundles = Bundler.GetBundles(configFile);

            foreach (Bundle bundle in bundles)
            {
                if (bundle.GetAbsoluteOutputFile() == fileName)
                {
                    var question = MessageBox.Show($"Do you want to remove the file from {FileHelpers.FILENAME}?", "Bundler & Minifier", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                    if (question == DialogResult.OK)
                        Bundler.RemoveBundle(configFile, bundle);

                    break;
                }
            }
        }
    }
}
