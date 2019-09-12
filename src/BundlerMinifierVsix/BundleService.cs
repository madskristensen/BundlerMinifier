using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BundlerMinifier;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BundlerMinifierVsix
{
    static class BundleService
    {
        private static BundleFileProcessor _processor;
        private static DTE2 _dte;

        static BundleService()
        {
            _dte = BundlerMinifierPackage._dte;

            BundleMinifier.BeforeWritingMinFile += CheckFileOutOfSourceControl;
            BundleMinifier.AfterWritingMinFile += AfterWritingFile;
            BundleMinifier.BeforeWritingGzipFile += CheckFileOutOfSourceControl;
            BundleMinifier.AfterWritingGzipFile += AfterWritingFile;
            BundleMinifier.ErrorMinifyingFile += ErrorMinifyingFile;
        }

        private static void AfterWritingFile(object sender, MinifyFileEventArgs e)
        {
            if (e.Bundle != null)
            {
                var sourceFile = e.OriginalFile;

                if (e.Bundle.OutputIsMinFile)
                {
                    string ext = Path.GetExtension(sourceFile);
                    var unMinFile = sourceFile.Replace(".min" + ext, ext);
                    if (File.Exists(unMinFile))
                        sourceFile = unMinFile;
                }

                // Bundle file minification
                if (e.Bundle.IncludeInProject)
                    ProjectHelpers.AddNestedFile(sourceFile, e.ResultFile, true);
            }
            else
            {
                // Single file minification
                ProjectHelpers.AddNestedFile(e.OriginalFile, e.ResultFile);
            }
        }

        private static BundleFileProcessor Processor
        {
            get
            {
                if (_processor == null)
                {
                    _processor = new BundleFileProcessor();
                    _processor.BeforeBundling += (s, e) => { if (e.ContainsChanges) { ProjectHelpers.CheckFileOutOfSourceControl(e.OutputFileName); } };
                    _processor.AfterBundling += AfterProcess;
                    _processor.AfterWritingSourceMap += AfterWritingSourceMap;
                    _processor.Processing += (s, e) => { ErrorList.CleanErrors(e.OutputFileName); };
                    _processor.BeforeWritingSourceMap += CheckFileOutOfSourceControl;
                }

                return _processor;
            }
        }

        internal static IEnumerable<Bundle> IsOutputConfigered(string configFile, string sourceFile)
        {
            List<Bundle> list = new List<Bundle>();

            if (string.IsNullOrEmpty(configFile))
                return list;

            try
            {
                var bundles = BundleHandler.GetBundles(configFile);

                foreach (Bundle bundle in bundles)
                {
                    if (bundle.GetAbsoluteOutputFile().Equals(sourceFile, StringComparison.OrdinalIgnoreCase))
                        list.Add(bundle);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            return list;
        }

        public static void Process(string configFile)
        {
            Process(configFile, null);
        }

        public static void Process(string configFile, IEnumerable<Bundle> bundles)
        {
            if (!IsOutputProduced(configFile))
                return;

            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    Processor.Process(configFile, bundles);
                }
                catch (Exception ex)
                {
                    HandleProcessingException(configFile, ex);
                }
            });
        }

        public static bool IsOutputProduced(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
                return false;

            string bindings = configFile + ".bindings";

            if (File.Exists(bindings))
            {
                var lines = File.ReadAllLines(bindings);
                return !lines.Any(l => l.TrimStart().StartsWith("produceoutput=false", StringComparison.OrdinalIgnoreCase));
            }

            if (BundlerMinifierPackage.Options != null)
                return BundlerMinifierPackage.Options.ProduceOutput;
            else
                return false;
        }

        public static void ToggleOutputProduction(string configFile, bool produceOutput)
        {
            string bindings = configFile + ".bindings";
            var sb = new StringBuilder();

            if (File.Exists(bindings))
            {
                var lines = File.ReadAllLines(bindings);

                foreach (var line in lines)
                {
                    if (!line.TrimStart().StartsWith("produceoutput", StringComparison.OrdinalIgnoreCase))
                        sb.AppendLine(line);
                }
            }

            sb.AppendLine($"produceoutput={produceOutput.ToString().ToLowerInvariant()}");

            ProjectHelpers.CheckFileOutOfSourceControl(bindings);
            File.WriteAllText(bindings, sb.ToString().Trim());
            ProjectHelpers.AddNestedFile(configFile, bindings);
        }

        public static void SourceFileChanged(string configFile, string sourceFile)
        {
            if (!IsOutputProduced(configFile))
                return;

            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    Processor.SourceFileChanged(configFile, sourceFile);
                }
                catch (Exception ex)
                {
                    HandleProcessingException(configFile, ex);
                }
            });
        }

        private static async void HandleProcessingException(string configFile, Exception ex)
        {
            await BundlerMinifierPackage.IsPackageInitialized;
            BundlerMinifierPackage._dispatcher.Invoke(new Action(() =>
            {
                var errorMessage = Resources.Text.ErrorExceptionThrown
                    .AddParams(configFile, ex);
                Logger.Log(errorMessage);

                var window = _dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                window.Activate();
            }));
        }

        private static void CheckFileOutOfSourceControl(object sender, MinifyFileEventArgs e)
        {
            if (e.ContainsChanges)
                ProjectHelpers.CheckFileOutOfSourceControl(e.ResultFile);
        }

        private static void AfterProcess(object sender, BundleFileEventArgs e)
        {
            if (!e.Bundle.IncludeInProject)
                return;

            var item = _dte.Solution.FindProjectItem(e.Bundle.FileName);

            if (item == null || item.ContainingProject == null)
                return;

            item.ContainingProject.AddFileToProject(e.OutputFileName);
            _dte.StatusBar.Text = Resources.Text.statusBundleUpdated;
        }

        private static void AfterWritingSourceMap(object sender, MinifyFileEventArgs e)
        {
            var item = _dte.Solution.FindProjectItem(e.OriginalFile);

            if (item == null || item.ContainingProject == null)
                return;

            ProjectHelpers.AddNestedFile(e.OriginalFile, e.ResultFile);
        }

        private static void ErrorMinifyingFile(object sender, MinifyFileEventArgs e)
        {
            ErrorListService.ProcessCompilerResults(e.Result);
            BundlerMinifierPackage._dte.StatusBar.Text = Resources.Text.ErrorMinifying.AddParams(Path.GetFileName(e.OriginalFile));
        }
    }
}
