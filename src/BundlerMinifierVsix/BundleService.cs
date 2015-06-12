using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using BundlerMinifier;
using EnvDTE80;

namespace BundlerMinifierVsix
{
    static class BundleService
    {
        private static BundleFileProcessor _processor;
        private static DTE2 _dte;

        static BundleService()
        {
            _dte = BundlerMinifierPackage._dte;

            BundleMinifier.BeforeWritingMinFile += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.ResultFile); };
            BundleMinifier.AfterWritingMinFile += (s, e) => { ProjectHelpers.AddNestedFile(e.OriginalFile, e.ResultFile);  };
            BundleMinifier.ErrorMinifyingFile += ErrorMinifyingFile;

            FileMinifier.BeforeWritingMinFile += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.ResultFile); };
            FileMinifier.AfterWritingMinFile += (s, e) => { ProjectHelpers.AddNestedFile(e.OriginalFile, e.ResultFile); };
        }

        private static BundleFileProcessor Processor
        {
            get
            {
                if (_processor == null)
                {
                    _processor = new BundleFileProcessor();
                    _processor.AfterProcess += AfterProcess;
                    _processor.AfterWritingSourceMap += AfterWritingSourceMap;
                    _processor.BeforeProcess += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.OutputFileName); ErrorList.CleanErrors(e.OutputFileName); };
                    _processor.BeforeWritingSourceMap += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.ResultFile); };
                }

                return _processor;
            }
        }

        public static void Process(string conigFile)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    Processor.Process(conigFile);
                }
                catch
                {
                    MessageBox.Show($"There is an error in the {FileHelpers.FILENAME} file. This could be due to a change in the format after this extension was updated.", "Web Compiler", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                catch
                {
                    MessageBox.Show($"There is an error in the {FileHelpers.FILENAME} file. This could be due to a change in the format after this extension was updated.", "Web Compiler", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }

        public static void MinifyFile(string fileName)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                string ext = Path.GetExtension(fileName);
                string minFile;
                bool minFileExist = FileHelpers.HasMinFile(fileName, out minFile);

                string mapFile;
                bool mapFileExist = FileHelpers.HasSourceMap(minFile, out mapFile);

                bool produceSourceMap = (minFileExist && mapFileExist) || (!minFileExist && !mapFileExist);

                MinificationResult result = FileMinifier.MinifyFile(fileName, produceSourceMap);
                if (result == null)
                    return;

                ErrorListService.ProcessCompilerResults(result);

                if (!result.HasErrors && produceSourceMap && !string.IsNullOrEmpty(result.SourceMap))
                {
                    mapFile = minFile + ".map";
                    ProjectHelpers.CheckFileOutOfSourceControl(mapFile);
                    File.WriteAllText(mapFile, result.SourceMap, new UTF8Encoding(true));

                    if (!mapFileExist)
                        ProjectHelpers.AddNestedFile(minFile, mapFile);
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
