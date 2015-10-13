using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BundlerMinifier
{
    /// <summary>
    /// An MSBuild task for running web compilers on a given config file.
    /// </summary>
    public class BundlerCleanTask : Task
    {
        /// <summary>
        /// The file path of the compilerconfig.json file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Execute the Task
        /// </summary>
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

            BundleFileProcessor processor = new BundleFileProcessor();
            processor.DeleteOutputFiles(configFile.FullName);

            Log.LogMessage(MessageImportance.High, "Bundler: Done cleaning output file from " + configFile.Name);

            return true;
        }

    }
}
