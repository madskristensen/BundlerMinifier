using System;
using System.IO;
using System.Linq;

namespace BundlerMinifier
{
    internal class ChangeHandler
    {
        private static string[] _ignorePatterns = { "\\node_modules\\", "\\bower_components\\", "\\jspm_packages\\" };
        private readonly Bundle _bundle;
        private readonly string _configFile;
        private readonly bool _isClean;
        private readonly BundleFileProcessor _processor;

        public ChangeHandler(BundleFileProcessor processor, string configFile, Bundle bundle, bool isClean)
        {
            _processor = processor;
            _configFile = configFile;
            _bundle = bundle;
            _isClean = isClean;
        }

        public bool FilesChanged(FileSystemEventArgs e)
        {
            if (!IsFileValid(e.FullPath))
            {
                return false;
            }

            if(!BundleFileProcessor.IsFileConfigured(_configFile, e.FullPath).Any())
            {
                return false;
            }

            var inputs = _bundle.GetAbsoluteInputFiles();
            var inputLastModified = inputs.Count > 0 ? inputs.Max(inputFile => File.GetLastWriteTimeUtc(inputFile)) : DateTime.MaxValue;

            if ((_bundle.GetAbsoluteInputFiles().Count > 1 || _bundle.InputFiles.FirstOrDefault() != _bundle.OutputFileName)
                && inputLastModified > File.GetLastWriteTimeUtc(_bundle.GetAbsoluteOutputFile()))
            {
                if (_isClean)
                {
                    _processor.Clean(_configFile, new Bundle[] { _bundle });
                }
                else
                {
                    _processor.Process(_configFile, new Bundle[] { _bundle });
                }
            }

            return true;
        }

        private bool IsFileValid(string file)
        {
            string fileName = Path.GetFileName(file);

            // VS adds ~ to temp file names so let's ignore those
            if (fileName.Contains('~') || fileName.Contains(".min."))
                return false;

            if (_ignorePatterns.Any(p => file.IndexOf(p) > -1))
            {
                //var fsw = (FileSystemWatcher)sender;
                //fsw.EnableRaisingEvents = false;
                return false;
            }

            if (!BundleFileProcessor.IsSupported(file))
                return false;

            return true;
        }
    }
}