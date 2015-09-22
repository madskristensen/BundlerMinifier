using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using BundlerMinifier;
using EnvDTE80;
using System.Linq;
using System;
using System.Collections.Generic;

namespace BundlerMinifierVsix
{
    static class BundleService
    {
        private static BundleFileProcessor _processor;
        private static DTE2 _dte;
        private static string[] _supported = new[] { ".JS", ".CSS", ".HTML", ".HTM" };

        static BundleService()
        {
            _dte = BundlerMinifierPackage._dte;

            BundleMinifier.BeforeWritingMinFile += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.ResultFile); };
            BundleMinifier.AfterWritingMinFile += AfterWritingFile;
            BundleMinifier.BeforeWritingGzipFile += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.ResultFile); };
            BundleMinifier.AfterWritingGzipFile += AfterWritingFile;
            BundleMinifier.ErrorMinifyingFile += ErrorMinifyingFile;
        }

        private static void AfterWritingFile(object sender, MinifyFileEventArgs e)
        {
            if (e.Bundle != null)
            {
                // Bundle file minification
                if (e.Bundle.IncludeInProject)
                    ProjectHelpers.AddNestedFile(e.OriginalFile, e.ResultFile);
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
                    _processor.AfterBundling += AfterProcess;
                    _processor.AfterWritingSourceMap += AfterWritingSourceMap;
                    _processor.Processing += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.OutputFileName); ErrorList.CleanErrors(e.OutputFileName); };
                    _processor.BeforeWritingSourceMap += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.ResultFile); };
                }

                return _processor;
            }
        }

        public static bool IsSupported(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToUpperInvariant();

            return _supported.Contains(ext);
        }

        internal static IEnumerable<Bundle> IsOutputConfigered(string configFile, string sourceFile)
        {
            List<Bundle> list = new List<Bundle>();

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

        public static void Process(string conigFile)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    Processor.Process(conigFile);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    MessageBox.Show($"There is an error in the {Constants.CONFIG_FILENAME} file. This could be due to a change in the format after this extension was updated.", "Web Compiler", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }

        public static void SourceFileChanged(string configFile, string sourceFile)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    Processor.SourceFileChanged(configFile, sourceFile);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    MessageBox.Show($"There is an error in the {Constants.CONFIG_FILENAME} file. This could be due to a change in the format after this extension was updated.", "Web Compiler", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }

        private static void AfterProcess(object sender, BundleFileEventArgs e)
        {
            if (!e.Bundle.IncludeInProject)
                return;

            var item = _dte.Solution.FindProjectItem(e.Bundle.FileName);

            if (item == null || item.ContainingProject == null)
                return;

            item.ContainingProject.AddFileToProject(e.OutputFileName);
            _dte.StatusBar.Text = "Bundle updated";
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
            BundlerMinifierPackage._dte.StatusBar.Text = $"There was a error minifying {Path.GetFileName(e.OriginalFile)}";
        }
    }
}
