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

        public static bool IsSupported(params string[] files)
        {
            files = files.Where(f => !string.IsNullOrEmpty(f)).ToArray();

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

        public void Clean(string fileName, IEnumerable<Bundle> bundles = null)
        {
            FileInfo info = new FileInfo(fileName);
            bundles = bundles ?? BundleHandler.GetBundles(fileName);

            foreach (Bundle bundle in bundles)
            {
                CleanBundle(info.Directory.FullName, bundle);
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

        public static IEnumerable<Bundle> IsFileConfigured(string configFile, string sourceFile)
        {
            List<Bundle> list = new List<Bundle>();

            try
            {
                var configs = BundleHandler.GetBundles(configFile);
                string folder = Path.GetDirectoryName(configFile);

                foreach (Bundle bundle in configs)
                {
                    foreach (string input in bundle.GetAbsoluteInputFiles())
                    {
                        if (input.Equals(sourceFile, StringComparison.OrdinalIgnoreCase) && !list.Contains(bundle))
                            list.Add(bundle);
                    }
                }

                return list;
            }
            catch (Exception)
            {
                return list;
            }
        }

        private void ProcessBundle(string baseFolder, Bundle bundle)
        {
            OnProcessing(bundle, baseFolder);
            var inputs = bundle.GetAbsoluteInputFiles();
            var inputLastModified = inputs.Count > 0 ? inputs.Max(inputFile => File.GetLastWriteTimeUtc(inputFile)) : DateTime.MaxValue;

            if ((bundle.GetAbsoluteInputFiles().Count > 1 || bundle.InputFiles.FirstOrDefault() != bundle.OutputFileName)
                && inputLastModified > File.GetLastWriteTimeUtc(bundle.GetAbsoluteOutputFile()))
            {
                BundleHandler.ProcessBundle(baseFolder, bundle);

                string outputFile = bundle.GetAbsoluteOutputFile();
                bool containsChanges = FileHelpers.HasFileContentChanged(outputFile, bundle.Output);

                if (containsChanges)
                {
                    DirectoryInfo outputFileDirectory = Directory.GetParent(outputFile);
                    outputFileDirectory.Create();

                    File.WriteAllText(outputFile, bundle.Output, new UTF8Encoding(false));
                    OnAfterBundling(bundle, baseFolder, containsChanges);
                }
            }

            string minFile = BundleMinifier.GetMinFileName(bundle.GetAbsoluteOutputFile());

            if (bundle.Minify.ContainsKey("enabled") && bundle.Minify["enabled"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                var result = BundleMinifier.MinifyBundle(bundle);

                if (result != null && bundle.SourceMap && !string.IsNullOrEmpty(result.SourceMap))
                {
                    string mapFile = minFile + ".map";
                    bool smChanges = FileHelpers.HasFileContentChanged(mapFile, result.SourceMap);

                    if (smChanges)
                    {
                        OnBeforeWritingSourceMap(minFile, mapFile, smChanges);
                        File.WriteAllText(mapFile, result.SourceMap, new UTF8Encoding(false));
                        OnAfterWritingSourceMap(minFile, mapFile, smChanges);
                    }
                }
            }
        }

        private void CleanBundle(string baseFolder, Bundle bundle)
        {
            string outputFile = bundle.GetAbsoluteOutputFile();
            baseFolder = baseFolder.DemandTrailingPathSeparatorChar();
            if (!bundle.GetAbsoluteInputFiles().Contains(outputFile, StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(outputFile))
                {
                    new FileInfo(outputFile).IsReadOnly = false;
                    File.Delete(outputFile);
                    Console.WriteLine($"Deleted {FileHelpers.MakeRelative(baseFolder, outputFile).Cyan().Bright()}");
                }
            }

            string minFile = BundleMinifier.GetMinFileName(bundle.GetAbsoluteOutputFile());
            string mapFile = minFile + ".map";
            string gzFile = minFile + ".gz";

            if (File.Exists(minFile))
            {
                new FileInfo(minFile).IsReadOnly = false;
                File.Delete(minFile);
                Console.WriteLine($"Deleted {FileHelpers.MakeRelative(baseFolder, minFile).Cyan().Bright()}");
            }

            if (File.Exists(mapFile))
            {
                new FileInfo(mapFile).IsReadOnly = false;
                File.Delete(mapFile);
                Console.WriteLine($"Deleted {mapFile.Cyan().Bright()}");
            }

            if (File.Exists(gzFile))
            {
                new FileInfo(gzFile).IsReadOnly = false;
                File.Delete(gzFile);
                Console.WriteLine($"Deleted {gzFile.Cyan().Bright()}");
            }
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
