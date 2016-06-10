using System;
using System.IO;
using System.Windows.Threading;
using BundlerMinifier;

namespace BundlerMinifierVsix
{
    class ErrorListService
    {
        public static void ProcessCompilerResults(MinificationResult result)
        {
            BundlerMinifierPackage._dispatcher.BeginInvoke(new Action(() =>
            {
                if (result.HasErrors)
                {
                    ErrorList.AddErrors(result.FileName, result.Errors);
                }
                else
                {
                    ErrorList.CleanErrors(result.FileName);
                    BundlerMinifierPackage._dte.StatusBar.Text = string.Format(Resources.Text.StatusMinified, Path.GetFileName(result.FileName));
                }
            }), DispatcherPriority.ApplicationIdle, null);
        }
    }
}
