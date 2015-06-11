using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
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
                //if (result == null)
                //{
                //    MessageBox.Show($"There is an error in the {FileHelpers.FILENAME} file. This could be due to a change in the format after this extension was updated.", "Web Compiler", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //    if (File.Exists(configFile))
                //        BundlerMinifierPackage._dte.ItemOperations.OpenFile(configFile);

                //    return;
                //}

                if (result.HasErrors)
                {
                    ErrorList.AddErrors(result.FileName, result.Errors);
                }
                else
                {
                    ErrorList.CleanErrors(result.FileName);
                    BundlerMinifierPackage._dte.StatusBar.Text = $"{Path.GetFileName(result.FileName)} compiled";
                }
            }), DispatcherPriority.ApplicationIdle, null);
        }
    }
}
