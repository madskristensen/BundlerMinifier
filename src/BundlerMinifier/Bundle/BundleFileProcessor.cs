using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace BundlerMinifier
{
    public class BundleFileProcessor
    {
        public static bool IsSupported(IEnumerable<string> files)
        {
            files = files.Where(f => !string.IsNullOrEmpty(f));

            if (files.Count() <= 1) return false;

            string ext = Path.GetExtension(files.ElementAt(0));

            foreach (string file in files)
            {
                if (!Path.GetExtension(file).Equals(ext, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        public void Process(string fileName)
        {
            FileInfo info = new FileInfo(fileName);
            var bundles = Bundler.GetBundles(fileName);

            foreach (Bundle bundle in bundles)
            {
                ProcessBundle(info.Directory.FullName, bundle);
            }
        }

        public void SourceFileChanged(string bundleFile, string sourceFile)
        {
            var bundles = Bundler.GetBundles(bundleFile);
            string folder = Path.GetDirectoryName(bundleFile);

            foreach (Bundle bundle in bundles)
            {
                foreach (string inputFile in bundle.InputFiles)
                {
                    string input = Path.Combine(folder, inputFile.Replace("/", "\\"));

                    if (input.Equals(sourceFile, System.StringComparison.OrdinalIgnoreCase))
                        ProcessBundle(folder, bundle);
                }
            }
        }

        private void ProcessBundle(string baseFolder, Bundle bundle)
        {
            Bundler.ProcessBundle(baseFolder, bundle);

            string outputFile = Path.Combine(baseFolder, bundle.OutputFileName);

            OnBeforeProcess(bundle, baseFolder);
            File.WriteAllText(outputFile, bundle.Output, new UTF8Encoding(true));
            OnAfterProcess(bundle, baseFolder);

            if (bundle.Minify.ContainsKey("enabled") && bundle.Minify["enabled"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                var result = BundleMinifier.MinifyBundle(bundle);

                if (bundle.SourceMaps && !string.IsNullOrEmpty(result.SourceMap))
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
