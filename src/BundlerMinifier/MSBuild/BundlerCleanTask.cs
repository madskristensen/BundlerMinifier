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

            Log.LogMessage(MessageImportance.High, Environment.NewLine + "Bundler: Cleaning output from " + configFile.Name);

            if (!configFile.Exists)
            {
                Log.LogWarning(configFile.FullName + " does not exist");
                return true;
            }

            Telemetry.SetDeviceName("MSBuild");

            var bundles = BundleHandler.GetBundles(configFile.FullName);

            foreach (Bundle bundle in bundles)
            {
                var outputFile = bundle.GetAbsoluteOutputFile();
                var inputFiles = bundle.GetAbsoluteInputFiles();

                var minFile = BundleFileProcessor.GetMinFileName(outputFile);
                var mapFile = minFile + ".map";
                var gzipFile = minFile + ".gz";

                if (!inputFiles.Contains(outputFile))
                    Deletefile(outputFile);

                Deletefile(minFile);
                Deletefile(mapFile);
                Deletefile(gzipFile);
            }

            Telemetry.TrackEvent("Delete output files");
            Log.LogMessage(MessageImportance.High, "Bundler: Done cleaning output file from " + configFile.Name);

            return true;
        }

        private void Deletefile(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    FileHelpers.RemoveReadonlyFlagFromFile(file);
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
            }
        }
    }
}
