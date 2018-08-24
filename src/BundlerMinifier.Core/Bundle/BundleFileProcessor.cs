﻿using System;
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

            string minFile = BundleMinifier.GetMinFileName(bundle.GetAbsoluteOutputFile(), bundle.IsDebugMinificationEnabled);

            if (bundle.IsMinificationEnabled || bundle.IsGzipEnabled)
            {
                var result = BundleMinifier.MinifyBundle(bundle);

                changed |= result.Changed;

                if (bundle.IsMinificationEnabled && bundle.SourceMap && !string.IsNullOrEmpty(result.SourceMap))
                {
                    string mapFile = minFile + ".map";
                    bool smChanges = FileHelpers.HasFileContentChanged(mapFile, result.SourceMap);

                    if (smChanges)
                    {
                        OnBeforeWritingSourceMap(minFile, mapFile, smChanges);
                        File.WriteAllText(mapFile, result.SourceMap, new UTF8Encoding(false));
                        OnAfterWritingSourceMap(minFile, mapFile, smChanges);
                        changed = true;
                    }
                }
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

            string minFile = BundleMinifier.GetMinFileName(bundle.GetAbsoluteOutputFile(), bundle.IsDebugMinificationEnabled);
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

        protected void OnProcessing(Bundle bundle, string baseFolder)
        {
            Processing?.Invoke(this, new BundleFileEventArgs(bundle.GetAbsoluteOutputFile(), bundle, baseFolder, false));
        }

        protected void OnBeforeBundling(Bundle bundle, string baseFolder, bool containsChanges)
        {
            BeforeBundling?.Invoke(this, new BundleFileEventArgs(bundle.GetAbsoluteOutputFile(), bundle, baseFolder, containsChanges));
        }


        protected void OnAfterBundling(Bundle bundle, string baseFolder, bool containsChanges)
        {
            AfterBundling?.Invoke(this, new BundleFileEventArgs(bundle.GetAbsoluteOutputFile(), bundle, baseFolder, containsChanges));
        }

        protected void OnBeforeWritingSourceMap(string file, string mapFile, bool containsChanges)
        {
            BeforeWritingSourceMap?.Invoke(this, new MinifyFileEventArgs(file, mapFile, containsChanges));
        }

        protected void OnAfterWritingSourceMap(string file, string mapFile, bool containsChanges)
        {
            AfterWritingSourceMap?.Invoke(this, new MinifyFileEventArgs(file, mapFile, containsChanges));
        }

        public event EventHandler<BundleFileEventArgs> Processing;
        public event EventHandler<BundleFileEventArgs> BeforeBundling;
        public event EventHandler<BundleFileEventArgs> AfterBundling;

        public event EventHandler<MinifyFileEventArgs> BeforeWritingSourceMap;
        public event EventHandler<MinifyFileEventArgs> AfterWritingSourceMap;
    }
}
