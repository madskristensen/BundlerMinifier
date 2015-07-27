using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace BundlerMinifier
{
    public class BundleFileProcessor
    {
        private static string[] _supported = new[] { ".JS", ".CSS", ".HTML", ".HTM" };
        public static bool IsSupported(IEnumerable<string> files)
        {
            files = files.Where(f => !string.IsNullOrEmpty(f));

            if (files.Count() <= 1) return false;

            string ext = Path.GetExtension(files.First()).ToUpperInvariant();

            foreach (string file in files)
            {
                string fileExt = Path.GetExtension(file).ToUpperInvariant();

                if (!_supported.Contains(fileExt) || !fileExt.Equals(ext, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        public void Process(string fileName)
        {
            FileInfo info = new FileInfo(fileName);
            var bundles = BundleHandler.GetBundles(fileName);

            foreach (Bundle bundle in bundles)
            {
                ProcessBundle(info.Directory.FullName, bundle);
            }
        }

        public void SourceFileChanged(string bundleFile, string sourceFile)
        {
            var bundles = BundleHandler.GetBundles(bundleFile);
            string bundleFileFolder = Path.GetDirectoryName(bundleFile),
                   sourceFileFolder = Path.GetDirectoryName(sourceFile);

            foreach (Bundle bundle in bundles)
            {
                foreach (string inputFile in bundle.InputFiles)
                {
                    string input = Path.Combine(bundleFileFolder, inputFile.Replace("/", "\\"));

                    if (input.Equals(sourceFile, System.StringComparison.OrdinalIgnoreCase) ||
                        input.Equals(sourceFileFolder, System.StringComparison.OrdinalIgnoreCase))
                        ProcessBundle(bundleFileFolder, bundle);
                }
            }
        }

        private void ProcessBundle(string baseFolder, Bundle bundle)
        {
            BundleHandler.ProcessBundle(baseFolder, bundle);

            string outputFile = Path.Combine(baseFolder, bundle.OutputFileName);

            OnBeforeProcess(bundle, baseFolder);

            DirectoryInfo outputFileDirectory = Directory.GetParent(outputFile);
            outputFileDirectory.Create();

            File.WriteAllText(outputFile, bundle.Output, new UTF8Encoding(true));

            OnAfterProcess(bundle, baseFolder);

            if (bundle.Minify.ContainsKey("enabled") && bundle.Minify["enabled"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                var result = BundleMinifier.MinifyBundle(bundle);

                if (result != null && bundle.SourceMaps && !string.IsNullOrEmpty(result.SourceMap))
                {
                    string minFile = FileMinifier.GetMinFileName(bundle.GetAbsoluteOutputFile());
                    string mapFile = minFile + ".map";

                    OnBeforeWritingSourceMap(minFile, mapFile);
                    File.WriteAllText(mapFile, result.SourceMap, new UTF8Encoding(true));
                    OnAfterWritingSourceMap(minFile, mapFile);
                }
            }
        }

        protected void OnBeforeProcess(Bundle bundle, string baseFolder)
        {
            if (BeforeProcess != null)
            {
                BeforeProcess(this, new BundleFileEventArgs(bundle.GetAbsoluteOutputFile(), bundle, baseFolder));
            }
        }

        protected void OnAfterProcess(Bundle bundle, string baseFolder)
        {
            if (AfterProcess != null)
            {
                AfterProcess(this, new BundleFileEventArgs(bundle.GetAbsoluteOutputFile(), bundle, baseFolder));
            }
        }

        protected void OnBeforeWritingSourceMap(string file, string mapFile)
        {
            if (BeforeWritingSourceMap != null)
            {
                BeforeWritingSourceMap(this, new MinifyFileEventArgs(file, mapFile));
            }
        }

        protected void OnAfterWritingSourceMap(string file, string mapFile)
        {
            if (AfterWritingSourceMap != null)
            {
                AfterWritingSourceMap(this, new MinifyFileEventArgs(file, mapFile));
            }
        }

        public event EventHandler<BundleFileEventArgs> BeforeProcess;
        public event EventHandler<BundleFileEventArgs> AfterProcess;

        public event EventHandler<MinifyFileEventArgs> BeforeWritingSourceMap;
        public event EventHandler<MinifyFileEventArgs> AfterWritingSourceMap;
    }
}
