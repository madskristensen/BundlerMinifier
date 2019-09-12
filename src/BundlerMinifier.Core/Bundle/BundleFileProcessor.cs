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

        public bool Process(string fileName, IEnumerable<Bundle> bundles = null)
        {
            FileInfo info = new FileInfo(fileName);
            bundles = bundles ?? BundleHandler.GetBundles(fileName);
            bool result = false;

            foreach (Bundle bundle in bundles)
            {
                result |= ProcessBundle(info.Directory.FullName, bundle);
            }

            return result;
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

        private bool ProcessBundle(string baseFolder, Bundle bundle)
        {
            OnProcessing(bundle, baseFolder);
            var inputs = bundle.GetAbsoluteInputFiles();
            bool changed = false;

            if (bundle.GetAbsoluteInputFiles(true).Count > 1 || bundle.InputFiles.FirstOrDefault() != bundle.OutputFileName)
            {
                BundleHandler.ProcessBundle(baseFolder, bundle);

                if (!bundle.IsMinificationEnabled || !bundle.OutputIsMinFile)
                {
                    string outputFile = bundle.GetAbsoluteOutputFile();
                    bool containsChanges = FileHelpers.HasFileContentChanged(outputFile, bundle.Output);

                    if (containsChanges)
                    {
                        OnBeforeBundling(bundle, baseFolder, containsChanges);
                        DirectoryInfo outputFileDirectory = Directory.GetParent(outputFile);
                        outputFileDirectory.Create();

                        File.WriteAllText(outputFile, bundle.Output, new UTF8Encoding(false));
                        OnAfterBundling(bundle, baseFolder, containsChanges);
                        changed = true;
                    }
                }
            }

            MinificationResult minResult = null;
            var minFile = BundleMinifier.GetMinFileName(bundle.GetAbsoluteOutputFile());
            if (bundle.IsMinificationEnabled)
            {
                var outputWriteTime = File.GetLastWriteTimeUtc(minFile);
                var minifyChanged = bundle.MostRecentWrite >= outputWriteTime;

                if (minifyChanged)
                {
                    minResult = BundleMinifier.MinifyBundle(bundle);

                    // If no change is detected, then the minFile is not modified, so we need to update the write time manually
                    if (!minResult.Changed && File.Exists(minFile))
                        File.SetLastWriteTimeUtc(minFile, DateTime.UtcNow);
                    changed |= minResult.Changed;

                    if (bundle.SourceMap && !string.IsNullOrEmpty(minResult.SourceMap))
                    {
                        string mapFile = minFile + ".map";
                        bool smChanges = FileHelpers.HasFileContentChanged(mapFile, minResult.SourceMap);

                        if (smChanges)
                        {
                            OnBeforeWritingSourceMap(minFile, mapFile, smChanges);
                            File.WriteAllText(mapFile, minResult.SourceMap, new UTF8Encoding(false));
                            OnAfterWritingSourceMap(minFile, mapFile, smChanges);
                            changed = true;
                        }
                    }
                }
                else
                {
                    OnMinificationSkipped(bundle, baseFolder, false);
                }
            }

            if (bundle.IsGzipEnabled)
            {
                var fileToGzip = bundle.IsMinificationEnabled ?
                    minFile : bundle.GetAbsoluteOutputFile();

                if (minResult == null)
                    BundleMinifier.GzipFile(fileToGzip, bundle, false, File.ReadAllText(fileToGzip));
                else
                    BundleMinifier.GzipFile(fileToGzip, bundle, minResult.Changed, minResult.MinifiedContent);
            }

            return changed;
        }

        private void CleanBundle(string baseFolder, Bundle bundle)
        {
            string outputFile = bundle.GetAbsoluteOutputFile();
            baseFolder = baseFolder.DemandTrailingPathSeparatorChar();
            if (!bundle.GetAbsoluteInputFiles().Contains(outputFile, StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(outputFile))
                {
                    FileHelpers.RemoveReadonlyFlagFromFile(outputFile);
                    File.Delete(outputFile);
                    Console.WriteLine($"Deleted {FileHelpers.MakeRelative(baseFolder, outputFile).Cyan().Bright()}");
                }
            }

            string minFile = BundleMinifier.GetMinFileName(bundle.GetAbsoluteOutputFile());
            string mapFile = minFile + ".map";
            string gzFile = minFile + ".gz";

            if (File.Exists(minFile))
            {
                FileHelpers.RemoveReadonlyFlagFromFile(minFile);
                File.Delete(minFile);
                Console.WriteLine($"Deleted {FileHelpers.MakeRelative(baseFolder, minFile).Cyan().Bright()}");
            }

            if (File.Exists(mapFile))
            {
                FileHelpers.RemoveReadonlyFlagFromFile(mapFile);
                File.Delete(mapFile);
                Console.WriteLine($"Deleted {mapFile.Cyan().Bright()}");
            }

            if (File.Exists(gzFile))
            {
                FileHelpers.RemoveReadonlyFlagFromFile(gzFile);
                File.Delete(gzFile);
                Console.WriteLine($"Deleted {gzFile.Cyan().Bright()}");
            }
        }

        public event EventHandler<BundleFileEventArgs> Processing;
        protected void OnProcessing(Bundle bundle, string baseFolder)
        {
            Processing?.Invoke(this, new BundleFileEventArgs(bundle.GetAbsoluteOutputFile(), bundle, baseFolder, false));
        }

        public event EventHandler<BundleFileEventArgs> BeforeBundling;
        protected void OnBeforeBundling(Bundle bundle, string baseFolder, bool containsChanges)
        {
            BeforeBundling?.Invoke(this, new BundleFileEventArgs(bundle.GetAbsoluteOutputFile(), bundle, baseFolder, containsChanges));
        }


        public event EventHandler<BundleFileEventArgs> AfterBundling;
        protected void OnAfterBundling(Bundle bundle, string baseFolder, bool containsChanges)
        {
            AfterBundling?.Invoke(this, new BundleFileEventArgs(bundle.GetAbsoluteOutputFile(), bundle, baseFolder, containsChanges));
        }

        public event EventHandler<MinifyFileEventArgs> BeforeWritingSourceMap;
        protected void OnBeforeWritingSourceMap(string file, string mapFile, bool containsChanges)
        {
            BeforeWritingSourceMap?.Invoke(this, new MinifyFileEventArgs(file, mapFile, containsChanges));
        }

        public event EventHandler<MinifyFileEventArgs> AfterWritingSourceMap;
        protected void OnAfterWritingSourceMap(string file, string mapFile, bool containsChanges)
        {
            AfterWritingSourceMap?.Invoke(this, new MinifyFileEventArgs(file, mapFile, containsChanges));
        }

        public event EventHandler<BundleFileEventArgs> MinificationSkipped;
        protected void OnMinificationSkipped(Bundle bundle, string baseFolder, bool containsChanges)
        {
            MinificationSkipped?.Invoke(this, new BundleFileEventArgs(bundle.GetAbsoluteOutputFile(), bundle, baseFolder, containsChanges));
        }


    }
}
