using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BundlerMinifier
{
    public class BundleFileEventArgs : EventArgs
    {
        public BundleFileEventArgs(string outputFileName, Bundle bundle, string baseFolder, bool containsChanges)
        {
            ContainsChanges = containsChanges;
            OutputFileName = outputFileName;
            Bundle = bundle;
            BaseFolder = baseFolder;
        }

        public bool ContainsChanges { get; set; }

        public Bundle Bundle { get; set; }

        public string OutputFileName { get; set; }

        public string BaseFolder { get; set; }

    }
}
