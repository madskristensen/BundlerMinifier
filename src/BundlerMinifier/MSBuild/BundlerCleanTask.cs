using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BundlerMinifier
{
    public class BundlerCleanTask : Task
    {
        public string FileName { get; set; }

        public override bool Execute()
        {
            FileInfo configFile = new FileInfo(FileName);

            Log.LogMessage(MessageImportance.Normal, "Bundler: Started cleaning output for " + configFile.FullName);

            if (!configFile.Exists)
            {
                Log.LogWarning("Bundler: " + configFile.FullName + " does not exist");
                return true;
            }

            var bundles = BundleHandler.GetBundles(configFile.FullName);

            if (bundles != null)
            {
                foreach (Bundle bundle in bundles)
                {
                    var outputFile = bundle.GetAbsoluteOutputFile();
                    var inputFiles = bundle.GetAbsoluteInputFiles();

                    var minFile = BundleMinifier.GetMinFileName(outputFile);
                    var mapFile = minFile + ".map";
                    var gzipFile = minFile + ".gz";

                    if (!inputFiles.Contains(outputFile))
                        Deletefile(outputFile);

                    Deletefile(minFile);
                    Deletefile(mapFile);
                    Deletefile(gzipFile);
                }

                Log.LogMessage(MessageImportance.Normal, "Bundler: Finished cleaning output for " + configFile.FullName);

                return true;
            }

            Log.LogWarning($"Bundler: There was an error reading {configFile.FullName}");
            return false;
        }

        private void Deletefile(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    FileHelpers.RemoveReadonlyFlagFromFile(file);
                    File.Delete(file);
                    Log.LogMessage(MessageImportance.High, "Bundler: Deleted file " + file);
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
            }
        }
    }
}
