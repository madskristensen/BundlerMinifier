using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BundlerMinifier
{
    /// <summary>
    /// An MSBuild task for running web compilers on a given config file.
    /// </summary>
    public class BundlerBuildTask : Task
    {
        private bool _isSuccessful = true;

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

            if (!configFile.Exists)
            {
                Log.LogWarning("Bundler: " + configFile.FullName + " does not exist");
                return true;
            }

            Log.LogMessage(MessageImportance.Normal, "Bundler: Started processing " + configFile.FullName);

            BundleFileProcessor processor = new BundleFileProcessor();
            processor.Processing += (s, e) => { RemoveReadonlyFlagFromFile(e.Bundle.GetAbsoluteOutputFile()); };
            processor.AfterBundling += Processor_AfterProcess;
            BundleMinifier.BeforeWritingMinFile += (s, e) => { RemoveReadonlyFlagFromFile(e.ResultFile); };
            processor.BeforeWritingSourceMap += (s, e) => { RemoveReadonlyFlagFromFile(e.ResultFile); };
            processor.AfterWritingSourceMap += Processor_AfterWritingSourceMap;
            BundleMinifier.ErrorMinifyingFile += BundleMinifier_ErrorMinifyingFile;
            BundleMinifier.AfterWritingMinFile += FileMinifier_AfterWritingMinFile;

            processor.Process(configFile.FullName);

            Log.LogMessage(MessageImportance.Normal, "Bundler: Finished processing " + configFile.FullName);

            return _isSuccessful;
        }

        private static void RemoveReadonlyFlagFromFile(string fileName)
        {
            FileInfo file = new FileInfo(fileName);

            if (file.Exists && file.IsReadOnly)
                file.IsReadOnly = false;
        }

        private void BundleMinifier_ErrorMinifyingFile(object sender, MinifyFileEventArgs e)
        {
            if (e.Result == null || !e.Result.HasErrors)
                return;

            _isSuccessful = false;

            foreach (var error in e.Result.Errors)
            {
                Log.LogError("Bundler", "0", "", error.FileName, error.LineNumber, error.ColumnNumber, error.LineNumber, error.ColumnNumber, error.Message, null); ;
            }
        }

        private void Processor_AfterProcess(object sender, BundleFileEventArgs e)
        {
            Log.LogMessage(MessageImportance.High, "Bundler: Created bundled file " + Path.Combine(e.BaseFolder, e.Bundle.OutputFileName));
        }

        private void Processor_AfterWritingSourceMap(object sender, MinifyFileEventArgs e)
        {
            Log.LogMessage(MessageImportance.High, "Bundler: Created source map file " + FileName, e.ResultFile);
        }

        private void FileMinifier_AfterWritingMinFile(object sender, MinifyFileEventArgs e)
        {
            Log.LogMessage(MessageImportance.High, "Bundler: Created minified file " + FileName, e.ResultFile);
        }
    }
}
