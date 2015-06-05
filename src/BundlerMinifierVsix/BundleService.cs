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
        }

        public static BundleFileProcessor Processor
        {
            get
            {
                if (_processor == null)
                {
                    _processor = new BundleFileProcessor();
                    _processor.AfterProcess += AfterProcess;
                    _processor.AfterWritingSourceMap += AfterWritingSourceMap;
                    _processor.BeforeProcess += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.OutputFileName); };
                    _processor.BeforeWritingSourceMap += (s, e) => { ProjectHelpers.CheckFileOutOfSourceControl(e.ResultFile); };
                }

                return _processor;
            }
        }

        private static void AfterProcess(object sender, BundleFileEventArgs e)
        {
            if (!e.Bundle.IncludeInProject)
                return;

            var item = _dte.Solution.FindProjectItem(e.Bundle.FileName);

            if (item == null || item.ContainingProject == null)
                return;

            ProjectHelpers.AddFileToProject(item.ContainingProject, e.OutputFileName);
            _dte.StatusBar.Text = "Bundle updated";
        }

        private static void AfterWritingSourceMap(object sender, MinifyFileEventArgs e)
        {
            var item = _dte.Solution.FindProjectItem(e.OriginalFile);

            if (item == null || item.ContainingProject == null)
                return;

            ProjectHelpers.AddNestedFile(e.OriginalFile, e.ResultFile);
        }
    }
}
