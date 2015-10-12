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

            if (!files.Any()) return false;

            string ext = Path.GetExtension(files.First()).ToUpperInvariant();

            foreach (string file in files)
            {
                string fileExt = Path.GetExtension(file).ToUpperInvariant();

                if (!_supported.Contains(fileExt) || !fileExt.Equals(ext, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        public void Process(string fileName, IEnumerable<Bundle> bundles = null)
        {
            FileInfo info = new FileInfo(fileName);
            bundles = bundles ?? BundleHandler.GetBundles(fileName);

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
                foreach (string input in bundle.GetAbsoluteInputFiles())
                {
                    if (input.Equals(sourceFile, StringComparison.OrdinalIgnoreCase) || input.Equals(sourceFileFolder, StringComparison.OrdinalIgnoreCase))
                        ProcessBundle(bundleFileFolder, bundle);
                }
            }
        }

        private void ProcessBundle(string baseFolder, Bundle bundle)
        {
            OnProcessing(bundle, baseFolder);

            var inputLastModified = bundle.GetAbsoluteInputFiles().Concat(new[] { bundle.FileName }).Max(inputFile => File.GetLastWriteTimeUtc(inputFile));

            if ((bundle.GetAbsoluteInputFiles().Count > 1 || bundle.InputFiles.FirstOrDefault() != bundle.OutputFileName)
                && inputLastModified > File.GetLastWriteTimeUtc(bundle.OutputFileName))
            {
                BundleHandler.ProcessBundle(baseFolder, bundle);

                string outputFile = Path.Combine(baseFolder, bundle.OutputFileName);
                bool containsChanges = FileHelpers.HasFileContentChanged(outputFile, bundle.Output);

                if (containsChanges)
                {
                    DirectoryInfo outputFileDirectory = Directory.GetParent(outputFile);
                    outputFileDirectory.Create();

                    File.WriteAllText(outputFile, bundle.Output, new UTF8Encoding(false));
                }

                OnAfterBundling(bundle, baseFolder, containsChanges);
            }

            string minFile = GetMinFileName(bundle.GetAbsoluteOutputFile());

            if (bundle.Minify.ContainsKey("enabled") && bundle.Minify["enabled"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase)
                && inputLastModified > File.GetLastWriteTimeUtc(minFile))
            {
                var result = BundleMinifier.MinifyBundle(bundle);

                if (result != null && bundle.SourceMap && !string.IsNullOrEmpty(result.SourceMap))
                {
                    string mapFile = minFile + ".map";
                    bool smChanges = FileHelpers.HasFileContentChanged(mapFile, result.SourceMap);

                    OnBeforeWritingSourceMap(minFile, mapFile, smChanges);

                    if (smChanges)
                    {
                        File.WriteAllText(mapFile, result.SourceMap, new UTF8Encoding(false));
                    }

                    OnAfterWritingSourceMap(minFile, mapFile, smChanges);
                }
            }
        }

        public static string GetMinFileName(string file)
        {
            string ext = Path.GetExtension(file);
            return file.Substring(0, file.LastIndexOf(ext, StringComparison.OrdinalIgnoreCase)) + ".min" + ext;
        }

        protected void OnProcessing(Bundle bundle, string baseFolder)
        {
            if (Processing != null)
            {
                Processing(this, new BundleFileEventArgs(bundle.GetAbsoluteOutputFile(), bundle, baseFolder, false));
            }
        }

        protected void OnAfterBundling(Bundle bundle, string baseFolder, bool containsChanges)
        {
            if (AfterBundling != null)
            {
                AfterBundling(this, new BundleFileEventArgs(bundle.GetAbsoluteOutputFile(), bundle, baseFolder, containsChanges));
            }
        }

        protected void OnBeforeWritingSourceMap(string file, string mapFile, bool containsChanges)
        {
            if (BeforeWritingSourceMap != null)
            {
                BeforeWritingSourceMap(this, new MinifyFileEventArgs(file, mapFile, containsChanges));
            }
        }

        protected void OnAfterWritingSourceMap(string file, string mapFile, bool containsChanges)
        {
            if (AfterWritingSourceMap != null)
            {
                AfterWritingSourceMap(this, new MinifyFileEventArgs(file, mapFile, containsChanges));
            }
        }

        public event EventHandler<BundleFileEventArgs> Processing;
        public event EventHandler<BundleFileEventArgs> AfterBundling;

        public event EventHandler<MinifyFileEventArgs> BeforeWritingSourceMap;
        public event EventHandler<MinifyFileEventArgs> AfterWritingSourceMap;
    }
}
